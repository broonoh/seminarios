using Microsoft.EntityFrameworkCore;
using SeminarioMobile.Data;
using SeminarioMobile.Models;

namespace SeminarioMobile.Services;

public class ClasseService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<Classe>> ListarAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Classes.OrderBy(c => c.Nome).ToListAsync();
    }

    public async Task CriarAsync(string nome)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Classes.Add(new Classe { Nome = nome });
        await db.SaveChangesAsync();
    }

    public async Task AtualizarAsync(int id, string nome)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var classe = await db.Classes.FindAsync(id)
            ?? throw new RegraNegocioException("Classe não encontrada");
        classe.Nome = nome;
        await db.SaveChangesAsync();
    }

    public async Task ExcluirAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var classe = await db.Classes.FindAsync(id)
            ?? throw new RegraNegocioException("Classe não encontrada");

        var emUso = await db.Seminarios.AnyAsync(s => s.ClasseId == id);
        if (emUso)
            throw new RegraNegocioException("Esta classe está em uso por um ou mais seminários e não pode ser excluída.");

        db.Classes.Remove(classe);
        await db.SaveChangesAsync();
    }
}
