using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeminarioMobile.Models;
using SeminarioMobile.Services;

namespace SeminarioMobile.ViewModels;

public partial class ImportarPortalViewModel(
    PortalImportService portalImportService,
    LocalService localService,
    ClasseService classeService,
    SeminarioService seminarioService) : ObservableObject
{
    private const string ChavePreferenciaCodigoGrupo = "portal_codigo_grupo";

    private Func<string, Task<string>>? _buscarJsonAsync;

    public ObservableCollection<EventoPortal> Eventos { get; } = [];

    [ObservableProperty]
    private string codigoGrupo = Preferences.Get(ChavePreferenciaCodigoGrupo, "51089");

    [ObservableProperty]
    private bool logadoNoPortal;

    [ObservableProperty]
    private bool carregando;

    [ObservableProperty]
    private string? mensagemResultado;

    partial void OnCodigoGrupoChanged(string value) => Preferences.Set(ChavePreferenciaCodigoGrupo, value);

    // Chamado periodicamente pelo code-behind da página enquanto o WebView está na tela
    // de login. buscarJsonAsync executa o fetch de dentro do próprio WebView (via JS),
    // reaproveitando a sessão/cookies reais do navegador embutido.
    [RelayCommand]
    public async Task VerificarLoginAsync(Func<string, Task<string>> buscarJsonAsync)
    {
        if (LogadoNoPortal || Carregando)
            return;

        _buscarJsonAsync = buscarJsonAsync;
        try
        {
            Carregando = true;
            var eventos = await portalImportService.ListarEventosAsync(buscarJsonAsync, CodigoGrupo);
            Eventos.Clear();
            foreach (var e in eventos) Eventos.Add(e);
            LogadoNoPortal = true;
        }
        catch
        {
            // Ainda não logado ou a sessão não é válida para este código de grupo — ignora
            // e deixa o WebView continuar na tela de login; a próxima verificação tenta de novo.
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task RecarregarAsync()
    {
        if (_buscarJsonAsync is null)
            return;

        Carregando = true;
        MensagemResultado = null;
        try
        {
            var eventos = await portalImportService.ListarEventosAsync(_buscarJsonAsync, CodigoGrupo);
            Eventos.Clear();
            foreach (var e in eventos) Eventos.Add(e);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erro ao carregar eventos", ex.Message, "OK");
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task ImportarSelecionadosAsync()
    {
        var selecionados = Eventos.Where(e => e.Selecionado).ToList();
        if (selecionados.Count == 0)
        {
            await Shell.Current.DisplayAlert("Nenhum evento selecionado", "Marque ao menos um evento para importar.", "OK");
            return;
        }

        Carregando = true;
        MensagemResultado = null;
        try
        {
            var resultado = await portalImportService.ImportarAsync(selecionados, localService, classeService, seminarioService);
            MensagemResultado = resultado.Ignorados > 0
                ? $"{resultado.Importados} seminário(s) importado(s), {resultado.Ignorados} já existia(m) e foram ignorados."
                : $"{resultado.Importados} seminário(s) importado(s) com sucesso.";
            foreach (var e in selecionados) e.Selecionado = false;
        }
        catch (RegraNegocioException ex)
        {
            await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Não foi possível importar", ex.Message, "OK");
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private void Desconectar()
    {
        _buscarJsonAsync = null;
        LogadoNoPortal = false;
        Eventos.Clear();
        MensagemResultado = null;
    }
}
