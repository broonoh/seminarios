using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeminarioMobile.Models;
using SeminarioMobile.Services;

namespace SeminarioMobile.ViewModels;

public partial class ObservacoesViewModel(ObservacaoService service) : ObservableObject
{
    private int? _idEmEdicao;

    [ObservableProperty]
    private ObservableCollection<Observacao> itens = [];

    [ObservableProperty]
    private string texto = string.Empty;

    [ObservableProperty]
    private string tituloFormulario = "Nova observação";

    [ObservableProperty]
    private bool emEdicao;

    [RelayCommand]
    public async Task CarregarAsync()
    {
        Itens = new ObservableCollection<Observacao>(await service.ListarAsync());
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(Texto)) return;
        try
        {
            if (_idEmEdicao is int id)
                await service.AtualizarAsync(id, Texto.Trim());
            else
                await service.CriarAsync(Texto.Trim());

            LimparFormulario();
            await CarregarAsync();
        }
        catch (RegraNegocioException ex)
        {
            await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Não foi possível salvar", $"Tente novamente com um texto mais curto.\n\nDetalhe: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void Editar(Observacao item)
    {
        _idEmEdicao = item.Id;
        Texto = item.Texto;
        TituloFormulario = "Editar observação";
        EmEdicao = true;
    }

    [RelayCommand]
    private void CancelarEdicao() => LimparFormulario();

    [RelayCommand]
    private async Task ExcluirAsync(Observacao item)
    {
        var confirmar = await Shell.Current.DisplayAlert("Excluir", "Excluir este registro?", "Sim", "Não");
        if (!confirmar) return;
        try
        {
            await service.ExcluirAsync(item.Id);
            await CarregarAsync();
        }
        catch (RegraNegocioException ex)
        {
            await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Não foi possível excluir", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task AtivarAsync(Observacao item)
    {
        try
        {
            await service.AtivarAsync(item.Id);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Não foi possível ativar", ex.Message, "OK");
        }
    }

    private void LimparFormulario()
    {
        _idEmEdicao = null;
        Texto = string.Empty;
        TituloFormulario = "Nova observação";
        EmEdicao = false;
    }
}
