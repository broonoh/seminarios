using Microsoft.EntityFrameworkCore;
using SeminarioMobile.Data;
using SeminarioMobile.Models;

namespace SeminarioMobile.Services;

public class LocalService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<Local>> ListarAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Locais.OrderBy(l => l.Nome).ToListAsync();
    }

    public async Task CriarAsync(string nome)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Locais.Add(new Local { Nome = nome });
        await db.SaveChangesAsync();
    }

    public async Task AtualizarAsync(int id, string nome)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var local = await db.Locais.FindAsync(id)
            ?? throw new RegraNegocioException("Maanaim não encontrado");
        local.Nome = nome;
        await db.SaveChangesAsync();
    }

    public async Task ExcluirAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var local = await db.Locais.FindAsync(id)
            ?? throw new RegraNegocioException("Maanaim não encontrado");

        var emUso = await db.Seminarios.AnyAsync(s => s.LocalId == id);
        if (emUso)
            throw new RegraNegocioException("Este Maanaim está em uso por um ou mais seminários e não pode ser excluído.");

        db.Locais.Remove(local);
        await db.SaveChangesAsync();
    }
}
