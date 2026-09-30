using Microsoft.EntityFrameworkCore;
using SeminarioMobile.Data;
using SeminarioMobile.Models;

namespace SeminarioMobile.Services;

public class ObservacaoService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<Observacao>> ListarAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Observacoes.OrderBy(o => o.Id).ToListAsync();
    }

    public async Task CriarAsync(string texto)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.Observacoes.Add(new Observacao { Texto = texto });
        await db.SaveChangesAsync();
    }

    public async Task AtualizarAsync(int id, string texto)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var observacao = await db.Observacoes.FindAsync(id)
            ?? throw new RegraNegocioException("Observação não encontrada");
        observacao.Texto = texto;
        await db.SaveChangesAsync();
    }

    public async Task ExcluirAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var observacao = await db.Observacoes.FindAsync(id)
            ?? throw new RegraNegocioException("Observação não encontrada");
        db.Observacoes.Remove(observacao);
        await db.SaveChangesAsync();
    }

    // Marca esta como a única ativa (exibida na faixa "Obs:" do banner).
    public async Task AtivarAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var observacao = await db.Observacoes.FindAsync(id)
            ?? throw new RegraNegocioException("Observação não encontrada");

        await db.Observacoes.ExecuteUpdateAsync(s => s.SetProperty(o => o.Ativo, false));
        observacao.Ativo = true;
        await db.SaveChangesAsync();
    }
}
