using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeminarioMobile.Models;
using SeminarioMobile.Services;

namespace SeminarioMobile.ViewModels;

public partial class IgrejasViewModel(IgrejaService service) : ObservableObject
{
    private int? _idEmEdicao;

    [ObservableProperty]
    private ObservableCollection<Igreja> itens = [];

    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private string tituloFormulario = "Nova Igreja";

    [ObservableProperty]
    private bool emEdicao;

    [RelayCommand]
    public async Task CarregarAsync()
    {
        Itens = new ObservableCollection<Igreja>(await service.ListarAsync());
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome)) return;
        try
        {
            if (_idEmEdicao is int id)
                await service.AtualizarAsync(id, Nome.Trim());
            else
                await service.CriarAsync(Nome.Trim());

            LimparFormulario();
            await CarregarAsync();
        }
        catch (RegraNegocioException ex)
        {
            await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Não foi possível salvar", $"Tente novamente com um nome mais curto.\n\nDetalhe: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void Editar(Igreja item)
    {
        _idEmEdicao = item.Id;
        Nome = item.Nome;
        TituloFormulario = "Editar Igreja";
        EmEdicao = true;
    }

    [RelayCommand]
    private void CancelarEdicao() => LimparFormulario();

    [RelayCommand]
    private async Task ExcluirAsync(Igreja item)
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
    private async Task AtivarAsync(Igreja item)
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
        Nome = string.Empty;
        TituloFormulario = "Nova Igreja";
        EmEdicao = false;
    }
}
