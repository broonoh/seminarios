namespace SeminarioMobile.Models;

// Local da igreja exibido no rodapé do banner (ex.: "ICM HINTERLAND").
public class Igreja
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; }
}
