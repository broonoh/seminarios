using CommunityToolkit.Mvvm.ComponentModel;

namespace SeminarioMobile.Models;

// Representa uma aba de mês (ou a aba "Todos") nas telas de Seminários e Banner.
// Não é persistido — comparado por valor para sobreviver a recargas da lista de meses.
public partial class MesOpcao : ObservableObject
{
    public int Ano { get; init; }
    public int Mes { get; init; }
    public bool EhTodos { get; init; }
    public string Rotulo { get; init; } = string.Empty;

    // Mantido em sincronia pelo ViewModel sempre que MesSelecionado muda — usado
    // diretamente pelo binding do chip (mais confiável que comparar dois bindings
    // via MultiBinding, que não estava re-avaliando ao trocar de aba).
    [ObservableProperty]
    private bool selecionado;

    public override bool Equals(object? obj) =>
        obj is MesOpcao outro && outro.Ano == Ano && outro.Mes == Mes && outro.EhTodos == EhTodos;

    public override int GetHashCode() => HashCode.Combine(Ano, Mes, EhTodos);
}
