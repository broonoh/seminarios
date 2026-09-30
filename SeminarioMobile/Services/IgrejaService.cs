using Microsoft.EntityFrameworkCore;
using SeminarioMobile.Data;
using SeminarioMobile.Models;

namespace SeminarioMobile.Services;

public class IgrejaService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<Igreja>> ListarAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Igrejas.OrderBy(i => i.Nome).ToListAsync();
    }

    public async Task CriarAsync(string nome)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Igrejas.Add(new Igreja { Nome = nome });
        await db.SaveChangesAsync();
    }

    public async Task AtualizarAsync(int id, string nome)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var igreja = await db.Igrejas.FindAsync(id)
            ?? throw new RegraNegocioException("Igreja não encontrada");
        igreja.Nome = nome;
        await db.SaveChangesAsync();
    }

    public async Task ExcluirAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var igreja = await db.Igrejas.FindAsync(id)
            ?? throw new RegraNegocioException("Igreja não encontrada");
        db.Igrejas.Remove(igreja);
        await db.SaveChangesAsync();
    }

    // Marca esta como a única ativa (aparece no rodapé do banner).
    public async Task AtivarAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var igreja = await db.Igrejas.FindAsync(id)
            ?? throw new RegraNegocioException("Igreja não encontrada");

        await db.Igrejas.ExecuteUpdateAsync(s => s.SetProperty(i => i.Ativo, false));
        igreja.Ativo = true;
        await db.SaveChangesAsync();
    }
}
