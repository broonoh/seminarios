using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeminarioMobile.Models;
using SeminarioMobile.Services;

namespace SeminarioMobile.ViewModels;

public partial class ClassesViewModel(ClasseService service) : ObservableObject
{
    private int? _idEmEdicao;

    // Recarregar troca a coleção inteira (em vez de Clear()+Add() item a item), o que
    // dispara um único refresh na CollectionView em vez de N incrementais — evita o
    // travamento visível ao navegar de volta pra esta tela.
    [ObservableProperty]
    private ObservableCollection<Classe> itens = [];

    [ObservableProperty]
    private string nome = string.Empty;

    [ObservableProperty]
    private string tituloFormulario = "Nova classe";

    [ObservableProperty]
    private bool emEdicao;

    [RelayCommand]
    public async Task CarregarAsync()
    {
        Itens = new ObservableCollection<Classe>(await service.ListarAsync());
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
    private void Editar(Classe item)
    {
        _idEmEdicao = item.Id;
        Nome = item.Nome;
        TituloFormulario = "Editar classe";
        EmEdicao = true;
    }

    [RelayCommand]
    private void CancelarEdicao() => LimparFormulario();

    [RelayCommand]
    private async Task ExcluirAsync(Classe item)
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
        TituloFormulario = "Nova classe";
        EmEdicao = false;
    }
}
