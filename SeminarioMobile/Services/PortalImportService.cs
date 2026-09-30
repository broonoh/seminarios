using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SeminarioMobile.Models;

namespace SeminarioMobile.Services;

public record ResultadoImportacao(int Importados, int Ignorados);

// Busca os eventos/seminários do portal do presbitério (portal.presbiterio.org.br) e
// importa os selecionados para o banco local. A busca em si (buscarJsonAsync) roda
// dentro do WebView de login embutido no app, via fetch() em JavaScript — reaproveita
// a sessão real do navegador em vez de tentar replicar cookies/headers externamente
// (o backend usa Laravel Sanctum SPA, que valida a origem da requisição).
public class PortalImportService
{
    private const string BaseUrl = "https://portal.presbiterio.org.br/minscricao/api";

    public async Task<List<EventoPortal>> ListarEventosAsync(Func<string, Task<string>> buscarJsonAsync, string codigoGrupo)
    {
        var indexUrl = $"{BaseUrl}/eventos/index/{codigoGrupo}?context=new&kfilter=&page=1&perPage=50&mode=grid";
        var indexJson = await buscarJsonAsync(indexUrl);
        var indexResposta = JsonSerializer.Deserialize<EventosIndexResposta>(indexJson)
            ?? throw new RegraNegocioException("Não foi possível ler a lista de eventos do portal.");

        var eventos = new List<EventoPortal>();
        foreach (var item in indexResposta.Data.Items)
        {
            var detailsUrl = $"{BaseUrl}/eventos/details/{codigoGrupo}/{item.Id}";
            var detailsJson = await buscarJsonAsync(detailsUrl);
            var detalhes = JsonSerializer.Deserialize<EventoDetailsResposta>(detailsJson);
            if (detalhes is null)
                continue;

            var inscricoesAte = FormatarData(detalhes.DatSeminarista);

            foreach (var classe in detalhes.Seminarios)
            {
                eventos.Add(new EventoPortal
                {
                    EventoId = item.Id,
                    Local = classe.DscLocal,
                    Classe = classe.DscClasse,
                    DataEvento = FormatarIntervalo(classe.Inicio, classe.Fim),
                    InscricoesAte = inscricoesAte,
                    ValorAdulto = ParseDecimal(classe.Valor),
                    ValorCrianca = classe.PodeCrianca == "1" ? ParseDecimal(classe.ValorCrianca) : null,
                });
            }
        }

        return eventos;
    }

    public async Task<ResultadoImportacao> ImportarAsync(
        List<EventoPortal> selecionados,
        LocalService localService,
        ClasseService classeService,
        SeminarioService seminarioService)
    {
        var importados = 0;
        var ignorados = 0;

        var locaisExistentes = await localService.ListarAsync();
        var classesExistentes = await classeService.ListarAsync();
        var seminariosExistentes = await seminarioService.ListarAsync();

        foreach (var evento in selecionados)
        {
            var jaExiste = seminariosExistentes.Any(s =>
                string.Equals(s.Local?.Nome, evento.Local, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.Classe?.Nome, evento.Classe, StringComparison.OrdinalIgnoreCase) &&
                s.DataEvento == evento.DataEvento);
            if (jaExiste)
            {
                ignorados++;
                continue;
            }

            var local = locaisExistentes.FirstOrDefault(l => string.Equals(l.Nome, evento.Local, StringComparison.OrdinalIgnoreCase));
            if (local is null)
            {
                await localService.CriarAsync(evento.Local);
                locaisExistentes = await localService.ListarAsync();
                local = locaisExistentes.First(l => string.Equals(l.Nome, evento.Local, StringComparison.OrdinalIgnoreCase));
            }

            var classe = classesExistentes.FirstOrDefault(c => string.Equals(c.Nome, evento.Classe, StringComparison.OrdinalIgnoreCase));
            if (classe is null)
            {
                await classeService.CriarAsync(evento.Classe);
                classesExistentes = await classeService.ListarAsync();
                classe = classesExistentes.First(c => string.Equals(c.Nome, evento.Classe, StringComparison.OrdinalIgnoreCase));
            }

            await seminarioService.CriarAsync(new Seminario
            {
                LocalId = local.Id,
                ClasseId = classe.Id,
                DataEvento = evento.DataEvento,
                InscricoesAte = evento.InscricoesAte,
                ValorAdulto = evento.ValorAdulto,
                ValorCrianca = evento.ValorCrianca,
            });
            seminariosExistentes = await seminarioService.ListarAsync();
            importados++;
        }

        return new ResultadoImportacao(importados, ignorados);
    }

    private static decimal ParseDecimal(string? valor) =>
        decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    private static string FormatarData(string dataHora)
    {
        var data = DateTime.Parse(dataHora, CultureInfo.InvariantCulture);
        return data.ToString("dd/MM/yyyy");
    }

    private static string FormatarIntervalo(string inicio, string fim)
    {
        var dataInicio = DateTime.Parse(inicio, CultureInfo.InvariantCulture);
        var dataFim = DateTime.Parse(fim, CultureInfo.InvariantCulture);
        return dataInicio.Date == dataFim.Date
            ? dataInicio.ToString("dd/MM/yyyy")
            : $"{dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}";
    }

    private class EventosIndexResposta
    {
        [JsonPropertyName("data")]
        public EventosIndexData Data { get; set; } = new();
    }

    private class EventosIndexData
    {
        [JsonPropertyName("items")]
        public List<EventoIndexItem> Items { get; set; } = [];
    }

    private class EventoIndexItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class EventoDetailsResposta
    {
        [JsonPropertyName("dat_seminarista")]
        public string DatSeminarista { get; set; } = string.Empty;

        [JsonPropertyName("seminarios")]
        public List<ClasseDetailsItem> Seminarios { get; set; } = [];
    }

    private class ClasseDetailsItem
    {
        [JsonPropertyName("dsc_classe")]
        public string DscClasse { get; set; } = string.Empty;

        [JsonPropertyName("dsc_local")]
        public string DscLocal { get; set; } = string.Empty;

        [JsonPropertyName("inicio")]
        public string Inicio { get; set; } = string.Empty;

        [JsonPropertyName("fim")]
        public string Fim { get; set; } = string.Empty;

        [JsonPropertyName("valor")]
        public string? Valor { get; set; }

        [JsonPropertyName("valor_crianca")]
        public string? ValorCrianca { get; set; }

        [JsonPropertyName("pode_crianca")]
        public string? PodeCrianca { get; set; }
    }
}
