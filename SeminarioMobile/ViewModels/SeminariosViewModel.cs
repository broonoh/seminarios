using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeminarioMobile.Helpers;
using SeminarioMobile.Models;
using SeminarioMobile.Services;

namespace SeminarioMobile.ViewModels;

public partial class SeminariosViewModel(
    SeminarioService seminarioService,
    LocalService localService,
    ClasseService classeService) : ObservableObject
{
    private const string FormatoData = "dd/MM/yyyy";
    private int? _idEmEdicao;

    // Recarregar troca a coleção inteira (em vez de Clear()+Add() item a item), o que
    // dispara um único refresh na CollectionView em vez de N incrementais — evita o
    // travamento visível ao navegar de volta pra esta tela.
    [ObservableProperty]
    private ObservableCollection<Seminario> itens = [];

    [ObservableProperty]
    private ObservableCollection<Seminario> itensFiltrados = [];

    [ObservableProperty]
    private ObservableCollection<Local> locais = [];

    [ObservableProperty]
    private ObservableCollection<Classe> classes = [];

    [ObservableProperty]
    private ObservableCollection<MesOpcao> meses = [];

    [ObservableProperty]
    private MesOpcao? mesSelecionado;

    [ObservableProperty]
    private Local? localSelecionado;

    [ObservableProperty]
    private Classe? classeSelecionada;

    [ObservableProperty]
    private DateTime dataInicio = DateTime.Today;

    [ObservableProperty]
    private bool temDataFim;

    [ObservableProperty]
    private DateTime dataFim = DateTime.Today;

    [ObservableProperty]
    private DateTime inscricoesAte = DateTime.Today;

    [ObservableProperty]
    private string valorAdulto = string.Empty;

    [ObservableProperty]
    private string valorCrianca = string.Empty;

    [ObservableProperty]
    private string tituloFormulario = "Novo seminário";

    [RelayCommand]
    public async Task CarregarAsync()
    {
        Locais = new ObservableCollection<Local>(await localService.ListarAsync());
        Classes = new ObservableCollection<Classe>(await classeService.ListarAsync());
        Itens = new ObservableCollection<Seminario>(await seminarioService.ListarAsync());

        AtualizarMeses();
    }

    private void AtualizarMeses()
    {
        var selecionadoAnterior = MesSelecionado;

        Meses = new ObservableCollection<MesOpcao>(DataEventoHelper.ExtrairMeses(Itens, incluirTodos: true));

        var hoje = DateTime.Today;
        var proximaSelecao = (selecionadoAnterior is not null && Meses.Contains(selecionadoAnterior)
                ? selecionadoAnterior
                : Meses.FirstOrDefault(m => !m.EhTodos && m.Ano == hoje.Year && m.Mes == hoje.Month))
            ?? Meses.FirstOrDefault();

        if (Equals(MesSelecionado, proximaSelecao))
            AtualizarItensFiltrados();
        else
            MesSelecionado = proximaSelecao;

        // Necessário mesmo quando a seleção "não muda" de valor: Meses foi recriado do
        // zero acima, então as instâncias de MesOpcao são novas e precisam ser marcadas.
        SincronizarSelecaoChips();
    }

    partial void OnMesSelecionadoChanged(MesOpcao? value)
    {
        AtualizarItensFiltrados();
        SincronizarSelecaoChips();
    }

    private void SincronizarSelecaoChips()
    {
        foreach (var m in Meses) m.Selecionado = Equals(m, MesSelecionado);
    }

    private void AtualizarItensFiltrados()
    {
        var filtrados = MesSelecionado is null || MesSelecionado.EhTodos
            ? Itens
            : Itens.Where(i =>
            {
                var inicio = DataEventoHelper.ParseInicio(i.DataEvento);
                return inicio.Year == MesSelecionado.Ano && inicio.Month == MesSelecionado.Mes;
            });

        ItensFiltrados = new ObservableCollection<Seminario>(filtrados);
    }

    [RelayCommand]
    private void SelecionarMes(MesOpcao mes) => MesSelecionado = mes;

    [RelayCommand]
    private void AlternarExpandido(Seminario item) => item.Expandido = !item.Expandido;

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (LocalSelecionado is null || ClasseSelecionada is null)
        {
            await Shell.Current.DisplayAlert("Erro", "Selecione o Maanaim e a classe.", "OK");
            return;
        }
        if (!decimal.TryParse(ValorAdulto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valorAdultoNum))
        {
            await Shell.Current.DisplayAlert("Erro", "Informe um valor adulto válido.", "OK");
            return;
        }
        decimal? valorCriancaNum = null;
        if (!string.IsNullOrWhiteSpace(ValorCrianca))
        {
            if (!decimal.TryParse(ValorCrianca, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
            {
                await Shell.Current.DisplayAlert("Erro", "Informe um valor de criança válido.", "OK");
                return;
            }
            valorCriancaNum = v;
        }

        var dataInicioTexto = DataInicio.ToString(FormatoData);
        var dataEvento = TemDataFim && DataFim.Date != DataInicio.Date
            ? $"{dataInicioTexto} a {DataFim.ToString(FormatoData)}"
            : dataInicioTexto;

        var seminario = new Seminario
        {
            Id = _idEmEdicao ?? 0,
            LocalId = LocalSelecionado.Id,
            ClasseId = ClasseSelecionada.Id,
            DataEvento = dataEvento,
            InscricoesAte = InscricoesAte.ToString(FormatoData),
            ValorAdulto = valorAdultoNum,
            ValorCrianca = valorCriancaNum,
        };

        try
        {
            if (_idEmEdicao is int)
                await seminarioService.AtualizarAsync(seminario);
            else
                await seminarioService.CriarAsync(seminario);

            LimparFormulario();
            await CarregarAsync();
        }
        catch (RegraNegocioException ex)
        {
            await Shell.Current.DisplayAlert("Erro", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Não foi possível salvar", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private void Editar(Seminario item)
    {
        _idEmEdicao = item.Id;
        LocalSelecionado = Locais.FirstOrDefault(l => l.Id == item.LocalId);
        ClasseSelecionada = Classes.FirstOrDefault(c => c.Id == item.ClasseId);

        var partes = item.DataEvento.Split(" a ", StringSplitOptions.TrimEntries);
        DataInicio = ParseData(partes[0]);
        if (partes.Length > 1)
        {
            TemDataFim = true;
            DataFim = ParseData(partes[1]);
        }
        else
        {
            TemDataFim = false;
        }

        InscricoesAte = ParseData(item.InscricoesAte);
        ValorAdulto = item.ValorAdulto.ToString(CultureInfo.InvariantCulture);
        ValorCrianca = item.ValorCrianca?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        TituloFormulario = "Editar seminário";
    }

    [RelayCommand]
    private void CancelarEdicao() => LimparFormulario();

    [RelayCommand]
    private async Task ExcluirAsync(Seminario item)
    {
        var confirmar = await Shell.Current.DisplayAlert("Excluir", "Excluir este seminário?", "Sim", "Não");
        if (!confirmar) return;
        try
        {
            await seminarioService.ExcluirAsync(item.Id);
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

    private static DateTime ParseData(string texto) =>
        DateTime.TryParseExact(texto, FormatoData, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? data
            : DateTime.Today;

    private void LimparFormulario()
    {
        _idEmEdicao = null;
        LocalSelecionado = null;
        ClasseSelecionada = null;
        // Se uma aba de mês específico estiver selecionada, já sugere o dia 1 daquele mês
        // — facilita cadastrar vários seminários seguidos do mesmo mês.
        DataInicio = MesSelecionado is { EhTodos: false } mes
            ? new DateTime(mes.Ano, mes.Mes, 1)
            : DateTime.Today;
        TemDataFim = false;
        DataFim = DataInicio;
        InscricoesAte = DataInicio;
        ValorAdulto = string.Empty;
        ValorCrianca = string.Empty;
        TituloFormulario = "Novo seminário";
    }
}
