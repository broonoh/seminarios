using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeminarioMobile.Models;
using SeminarioMobile.Services;

namespace SeminarioMobile.ViewModels;

public partial class MaanaimViewModel(LocalService service) : ObservableObject
{
    private int? _idEmEdicao;

    [ObservableProperty]
    private ObservableCollection<Local> itens = [];

    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private string tituloFormulario = "Novo Maanaim";

    [ObservableProperty]
    private bool emEdicao;

    [RelayCommand]
    public async Task CarregarAsync()
    {
        Itens = new ObservableCollection<Local>(await service.ListarAsync());
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
    private void Editar(Local item)
    {
        _idEmEdicao = item.Id;
        Nome = item.Nome;
        TituloFormulario = "Editar Maanaim";
        EmEdicao = true;
    }

    [RelayCommand]
    private void CancelarEdicao() => LimparFormulario();

    [RelayCommand]
    private async Task ExcluirAsync(Local item)
    {
        var confirmar = await Shell.Current.DisplayAlert("Excluir", $"Excluir \"{item.Nome}\"?", "Sim", "Não");
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

    private void LimparFormulario()
    {
        _idEmEdicao = null;
        Nome = string.Empty;
        TituloFormulario = "Novo Maanaim";
        EmEdicao = false;
    }
}
