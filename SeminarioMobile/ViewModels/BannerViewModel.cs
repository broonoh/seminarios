using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using SeminarioMobile.Helpers;
using SeminarioMobile.Models;
using SeminarioMobile.Services;

namespace SeminarioMobile.ViewModels;

public partial class BannerViewModel(BannerService bannerService, SeminarioService seminarioService) : ObservableObject
{
    private List<Seminario> _todosItens = [];

    [ObservableProperty]
    private ObservableCollection<MesOpcao> meses = [];

    [ObservableProperty]
    private MesOpcao? mesSelecionado;

    [ObservableProperty]
    private int quantidadeSeminarios;

    [ObservableProperty]
    private bool gerando;

    [ObservableProperty]
    private string? ultimoCaminhoPng;

    // false = modelo Padrão, true = Colorido por Seminário
    [ObservableProperty]
    private bool templateColorido;

    [RelayCommand]
    public async Task CarregarAsync()
    {
        _todosItens = await seminarioService.ListarAsync();

        var selecionadoAnterior = MesSelecionado;
        Meses = new ObservableCollection<MesOpcao>(DataEventoHelper.ExtrairMeses(_todosItens, incluirTodos: false));

        var hoje = DateTime.Today;
        var proximaSelecao = (selecionadoAnterior is not null && Meses.Contains(selecionadoAnterior) ? selecionadoAnterior : null)
            ?? Meses.FirstOrDefault(m => m.Ano == hoje.Year && m.Mes == hoje.Month)
            ?? Meses.FirstOrDefault(m => new DateTime(m.Ano, m.Mes, 1) >= new DateTime(hoje.Year, hoje.Month, 1))
            ?? Meses.FirstOrDefault();

        if (Equals(MesSelecionado, proximaSelecao))
            await GerarPreviewAsync();
        else
            MesSelecionado = proximaSelecao;

        // Necessário mesmo quando a seleção "não muda" de valor: Meses foi recriado do
        // zero acima, então as instâncias de MesOpcao são novas e precisam ser marcadas.
        SincronizarSelecaoChips();
    }

    partial void OnMesSelecionadoChanged(MesOpcao? value)
    {
        SincronizarSelecaoChips();
        _ = GerarPreviewAsync();
    }

    private void SincronizarSelecaoChips()
    {
        foreach (var m in Meses) m.Selecionado = Equals(m, MesSelecionado);
    }

    partial void OnTemplateColoridoChanged(bool value) => _ = GerarPreviewAsync();

    [RelayCommand]
    private void SelecionarMes(MesOpcao mes) => MesSelecionado = mes;

    private async Task GerarPreviewAsync()
    {
        if (MesSelecionado is null)
        {
            QuantidadeSeminarios = 0;
            UltimoCaminhoPng = null;
            return;
        }

        QuantidadeSeminarios = _todosItens.Count(s =>
        {
            var inicio = DataEventoHelper.ParseInicio(s.DataEvento);
            return inicio.Year == MesSelecionado.Ano && inicio.Month == MesSelecionado.Mes;
        });

        Gerando = true;
        try
        {
            var template = TemplateColorido ? TemplateBanner.ColoridoPorSeminario : TemplateBanner.Padrao;
            UltimoCaminhoPng = await bannerService.GerarImagemAsync(MesSelecionado.Ano, MesSelecionado.Mes, template);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erro ao gerar banner", ex.Message, "OK");
        }
        finally
        {
            Gerando = false;
        }
    }

    [RelayCommand]
    private async Task CompartilharAsync()
    {
        if (QuantidadeSeminarios == 0 || MesSelecionado is null)
        {
            await Shell.Current.DisplayAlert("Nenhum seminário neste mês", "Cadastre pelo menos um seminário na aba \"Seminários\" para este mês antes de compartilhar o banner.", "OK");
            return;
        }

        if (UltimoCaminhoPng is null)
            await GerarPreviewAsync();

        if (UltimoCaminhoPng is null)
            return;

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Banner de Seminários",
            File = new ShareFile(UltimoCaminhoPng),
        });
    }
}
