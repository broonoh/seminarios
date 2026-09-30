using System.ComponentModel;
using System.Text.Json;
using SeminarioMobile.ViewModels;
#if ANDROID
using Android.Webkit;
#endif

namespace SeminarioMobile.Views;

public partial class ImportarPortalPage : ContentPage
{
    private readonly ImportarPortalViewModel _viewModel;
    private IDispatcherTimer? _timerVerificacaoLogin;

    public ImportarPortalPage(ImportarPortalViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_viewModel.LogadoNoPortal)
            IniciarVerificacaoPeriodica();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        PararVerificacaoPeriodica();
    }

    private void OnMenuClicked(object? sender, EventArgs e) =>
        Shell.Current.FlyoutIsPresented = true;

    // A cada tick, tenta buscar a lista de eventos de dentro do próprio WebView
    // (login pode não ter acontecido ainda — o ViewModel ignora falhas silenciosamente
    // e tenta de novo no próximo tick).
    private void IniciarVerificacaoPeriodica()
    {
        if (_timerVerificacaoLogin is not null)
            return;

        _timerVerificacaoLogin = Dispatcher.CreateTimer();
        _timerVerificacaoLogin.Interval = TimeSpan.FromSeconds(2);
        _timerVerificacaoLogin.Tick += (_, _) => _viewModel.VerificarLoginCommand.Execute(BuscarJsonAsync);
        _timerVerificacaoLogin.Start();
    }

    private void PararVerificacaoPeriodica()
    {
        _timerVerificacaoLogin?.Stop();
        _timerVerificacaoLogin = null;
    }

    // Executa um fetch() dentro do contexto do WebView (mesma sessão/cookies/origem do
    // navegador real), já que o backend do portal (Laravel Sanctum) rejeita chamadas
    // feitas de fora do próprio navegador mesmo enviando o cookie de sessão correto.
    // EvaluateJavaScriptAsync não espera Promises resolverem, então o resultado é
    // guardado numa variável global e lido por polling.
    private async Task<string> BuscarJsonAsync(string url)
    {
        var varName = $"__portalFetch_{Guid.NewGuid():N}";
        var urlEscapada = JsonSerializer.Serialize(url);
        var script = $$"""
            (function(){
                window.{{varName}} = null;
                fetch({{urlEscapada}}, { credentials: 'include', headers: { 'Accept': 'application/json' } })
                    .then(function(r) { return r.text().then(function(t) { return JSON.stringify({ status: r.status, body: t }); }); })
                    .then(function(res) { window.{{varName}} = res; })
                    .catch(function(e) { window.{{varName}} = JSON.stringify({ status: 0, body: String(e) }); });
            })();
            """;
        await WebViewPortal.EvaluateJavaScriptAsync(script);

        for (var i = 0; i < 75; i++)
        {
            var bruto = await WebViewPortal.EvaluateJavaScriptAsync($"window.{varName} || ''");
            if (!string.IsNullOrEmpty(bruto) && bruto is not ("null" or "\"\""))
            {
                var envelopeJson = DesempacotarString(bruto);
                var envelope = JsonSerializer.Deserialize<FetchEnvelope>(envelopeJson)!;
                if (envelope.Status is >= 200 and < 300)
                    return envelope.Body;
                throw new HttpRequestException($"Portal retornou status {envelope.Status}: {envelope.Body}");
            }
            await Task.Delay(200);
        }

        throw new TimeoutException("Tempo esgotado aguardando resposta do portal.");
    }

    // No Android, EvaluateJavaScriptAsync devolve o valor da string JS sem as aspas
    // externas do literal JSON, mas com os escapes internos intactos — reconstrói o
    // literal válido antes de desserializar.
    private static string DesempacotarString(string valor)
    {
        var candidato = valor.StartsWith('"') && valor.EndsWith('"') ? valor : $"\"{valor}\"";
        try
        {
            return JsonSerializer.Deserialize<string>(candidato) ?? valor;
        }
        catch (JsonException)
        {
            return valor;
        }
    }

    private record FetchEnvelope(int Status, string Body);

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ImportarPortalViewModel.LogadoNoPortal))
            return;

        if (_viewModel.LogadoNoPortal)
        {
            PararVerificacaoPeriodica();
            return;
        }

        // "Trocar conta": limpa a sessão do portal, recarrega o WebView na tela de
        // login e volta a verificar periodicamente.
#if ANDROID
        CookieManager.Instance?.RemoveAllCookies(null);
#endif
        WebViewPortal.Reload();
        IniciarVerificacaoPeriodica();
    }
}
