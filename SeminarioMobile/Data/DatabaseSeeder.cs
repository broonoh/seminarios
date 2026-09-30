using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SeminarioMobile.Models;

namespace SeminarioMobile.Data;

// Popula o banco local a partir do SeedData.json (carga inicial exportada do
// sistema web) apenas na primeira execução — mesmo espírito do seed.py atual.
public static class DatabaseSeeder
{
    private const string NomeArquivo = "SeedData.json";

    public static async Task PopularSeNecessarioAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Locais.AnyAsync())
            return;

        using var stream = await FileSystem.OpenAppPackageFileAsync(NomeArquivo);
        var payload = await JsonSerializer.DeserializeAsync<SeedPayload>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (payload is null)
            return;

        db.Locais.AddRange(payload.Locais.Select(l => new Local { Id = l.Id, Nome = l.Nome }));
        db.Classes.AddRange(payload.Classes.Select(c => new Classe { Id = c.Id, Nome = c.Nome }));
        db.Igrejas.AddRange(payload.Igrejas.Select(i => new Igreja { Id = i.Id, Nome = i.Nome, Ativo = i.Ativo }));
        db.Observacoes.AddRange(payload.Observacoes.Select(o => new Observacao { Id = o.Id, Texto = o.Texto, Ativo = o.Ativo }));
        await db.SaveChangesAsync();

        db.Seminarios.AddRange(payload.Seminarios.Select(s => new Seminario
        {
            Id = s.Id,
            LocalId = s.Local.Id,
            ClasseId = s.Classe.Id,
            DataEvento = s.DataEvento,
            InscricoesAte = s.InscricoesAte,
            ValorAdulto = s.ValorAdulto,
            ValorCrianca = s.ValorCrianca,
        }));
        await db.SaveChangesAsync();
    }
}
