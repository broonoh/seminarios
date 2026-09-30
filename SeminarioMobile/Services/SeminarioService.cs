using Microsoft.EntityFrameworkCore;
using SeminarioMobile.Data;
using SeminarioMobile.Models;

namespace SeminarioMobile.Services;

public class SeminarioService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<Seminario>> ListarAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Seminarios
            .Include(s => s.Local)
            .Include(s => s.Classe)
            .OrderBy(s => s.Id)
            .ToListAsync();
    }

    public async Task CriarAsync(Seminario seminario)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await ValidarReferenciasAsync(db, seminario.LocalId, seminario.ClasseId);
        db.Seminarios.Add(seminario);
        await db.SaveChangesAsync();
    }

    public async Task AtualizarAsync(Seminario seminario)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var existente = await db.Seminarios.FindAsync(seminario.Id)
            ?? throw new RegraNegocioException("Seminário não encontrado");
        await ValidarReferenciasAsync(db, seminario.LocalId, seminario.ClasseId);

        existente.LocalId = seminario.LocalId;
        existente.ClasseId = seminario.ClasseId;
        existente.DataEvento = seminario.DataEvento;
        existente.InscricoesAte = seminario.InscricoesAte;
        existente.ValorAdulto = seminario.ValorAdulto;
        existente.ValorCrianca = seminario.ValorCrianca;
        await db.SaveChangesAsync();
    }

    public async Task ExcluirAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var seminario = await db.Seminarios.FindAsync(id)
            ?? throw new RegraNegocioException("Seminário não encontrado");
        db.Seminarios.Remove(seminario);
        await db.SaveChangesAsync();
    }

    private static async Task ValidarReferenciasAsync(AppDbContext db, int localId, int classeId)
    {
        if (!await db.Locais.AnyAsync(l => l.Id == localId))
            throw new RegraNegocioException("Maanaim informado não existe");
        if (!await db.Classes.AnyAsync(c => c.Id == classeId))
            throw new RegraNegocioException("Classe informada não existe");
    }
}
