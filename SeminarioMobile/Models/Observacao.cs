namespace SeminarioMobile.Models;

// Texto exibido na faixa de observação do banner.
public class Observacao
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public bool Ativo { get; set; }
}
