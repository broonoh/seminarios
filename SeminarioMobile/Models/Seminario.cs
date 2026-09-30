using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SeminarioMobile.Models;

public partial class Seminario : ObservableObject
{
    public int Id { get; set; }

    public int LocalId { get; set; }
    public Local? Local { get; set; }

    public int ClasseId { get; set; }
    public Classe? Classe { get; set; }

    // Texto livre: "dd/MM/yyyy" ou "dd/MM/yyyy a dd/MM/yyyy" (intervalo).
    public string DataEvento { get; set; } = string.Empty;
    public string InscricoesAte { get; set; } = string.Empty;

    public decimal ValorAdulto { get; set; }
    public decimal? ValorCrianca { get; set; }

    // Estado de UI (card expandido/recolhido na lista) — não é persistido.
    [ObservableProperty]
    [property: NotMapped]
    private bool expandido;
}
