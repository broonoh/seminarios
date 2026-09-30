using CommunityToolkit.Mvvm.ComponentModel;

namespace SeminarioMobile.Models;

// Uma linha do portal já achatada por classe (um evento pode ter várias classes,
// cada uma vira um EventoPortal — e, se importado, um Seminario separado).
public partial class EventoPortal : ObservableObject
{
    public string EventoId { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public string Classe { get; set; } = string.Empty;
    public string DataEvento { get; set; } = string.Empty;
    public string InscricoesAte { get; set; } = string.Empty;
    public decimal ValorAdulto { get; set; }
    public decimal? ValorCrianca { get; set; }

    [ObservableProperty]
    private bool selecionado;
}
