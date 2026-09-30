using System.Text.Json.Serialization;

namespace SeminarioMobile.Data;

// Formato do export feito a partir da API do sistema web (GET /api/locais, /classes, etc.).
public class SeedPayload
{
    [JsonPropertyName("locais")]
    public List<SeedLocal> Locais { get; set; } = [];

    [JsonPropertyName("classes")]
    public List<SeedClasse> Classes { get; set; } = [];

    [JsonPropertyName("igrejas")]
    public List<SeedIgreja> Igrejas { get; set; } = [];

    [JsonPropertyName("observacoes")]
    public List<SeedObservacao> Observacoes { get; set; } = [];

    [JsonPropertyName("seminarios")]
    public List<SeedSeminario> Seminarios { get; set; } = [];
}

public class SeedLocal
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
}

public class SeedClasse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
}

public class SeedIgreja
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; }
}

public class SeedObservacao
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public bool Ativo { get; set; }
}

public class SeedSeminario
{
    public int Id { get; set; }

    [JsonPropertyName("data_evento")]
    public string DataEvento { get; set; } = string.Empty;

    [JsonPropertyName("inscricoes_ate")]
    public string InscricoesAte { get; set; } = string.Empty;

    [JsonPropertyName("valor_adulto")]
    public decimal ValorAdulto { get; set; }

    [JsonPropertyName("valor_crianca")]
    public decimal? ValorCrianca { get; set; }

    public SeedLocal Local { get; set; } = new();
    public SeedClasse Classe { get; set; } = new();
}
