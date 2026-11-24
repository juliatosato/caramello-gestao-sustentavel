using Caramello.Api.Domain;
using Caramello.Api.Dtos;
using Caramello.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Caramello.Api.Services;

public class InsumoService(AppDbContext db) : IInsumoService
{
    public async Task<IReadOnlyList<Insumo>> ListarAsync(CancellationToken cancellationToken) =>
        await db.Insumos.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome).ToListAsync(cancellationToken);

    public Task<Insumo?> ObterAsync(int id, CancellationToken cancellationToken) =>
        db.Insumos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Ativo, cancellationToken);

    public async Task<Insumo> CriarAsync(InsumoInput input, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var item = new Insumo
        {
            Nome = input.Nome.Trim(),
            Categoria = input.Categoria.Trim(),
            Unidade = input.Unidade,
            Quantidade = input.Quantidade,
            EstoqueMinimo = input.EstoqueMinimo,
            PrazoReposicaoDias = input.PrazoReposicaoDias,
            EstoqueSeguranca = input.EstoqueSeguranca
        };
        db.Insumos.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        if (input.Quantidade > 0)
        {
            db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                InsumoId = item.Id,
                Tipo = TipoMovimentacao.Entrada,
                Quantidade = input.Quantidade,
                Unidade = item.Unidade,
                Motivo = "Saldo inicial",
                Data = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return item;
    }

    public async Task<Insumo?> EditarAsync(int id, InsumoEdicaoInput input, CancellationToken cancellationToken)
    {
        var item = await db.Insumos.FirstOrDefaultAsync(x => x.Id == id && x.Ativo, cancellationToken);
        if (item is null) return null;

        var possuiHistorico = await db.MovimentacoesEstoque.AnyAsync(
            x => x.InsumoId == id,
            cancellationToken);
        if (possuiHistorico && item.Unidade != input.Unidade)
        {
            throw new RegraNegocioException("A unidade não pode ser alterada após existirem movimentações.");
        }

        item.Nome = input.Nome.Trim();
        item.Categoria = input.Categoria.Trim();
        item.Unidade = input.Unidade;
        item.EstoqueMinimo = input.EstoqueMinimo;
        item.PrazoReposicaoDias = input.PrazoReposicaoDias;
        item.EstoqueSeguranca = input.EstoqueSeguranca;
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<bool> DesativarAsync(int id, CancellationToken cancellationToken)
    {
        var item = await db.Insumos.FirstOrDefaultAsync(x => x.Id == id && x.Ativo, cancellationToken);
        if (item is null) return false;
        item.Ativo = false;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MovimentacaoEstoque?> MovimentarAsync(
        int id,
        MovimentacaoInput input,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var item = await db.Insumos.FirstOrDefaultAsync(x => x.Id == id && x.Ativo, cancellationToken);
        if (item is null) return null;

        var saida = input.Tipo is TipoMovimentacao.Consumo or TipoMovimentacao.Descarte;
        if (saida && input.Quantidade > item.Quantidade)
        {
            throw new RegraNegocioException("A saída não pode ser superior ao estoque disponível.");
        }

        item.Quantidade += saida ? -input.Quantidade : input.Quantidade;
        var movimentacao = new MovimentacaoEstoque
        {
            InsumoId = item.Id,
            Tipo = input.Tipo,
            Quantidade = input.Quantidade,
            Unidade = item.Unidade,
            Motivo = input.Motivo?.Trim() ?? string.Empty,
            Data = DateTime.UtcNow
        };
        db.MovimentacoesEstoque.Add(movimentacao);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return movimentacao;
    }

    public async Task<IReadOnlyList<MovimentacaoDto>> ListarMovimentacoesAsync(
        int? insumoId,
        CancellationToken cancellationToken)
    {
        var query = db.MovimentacoesEstoque.AsNoTracking().Include(x => x.Insumo).AsQueryable();
        if (insumoId.HasValue) query = query.Where(x => x.InsumoId == insumoId.Value);

        return await query.OrderByDescending(x => x.Data).Select(x => new MovimentacaoDto(
            x.Id,
            x.InsumoId,
            x.Insumo.Nome,
            x.Unidade,
            x.Tipo,
            x.Quantidade,
            x.Motivo,
            x.Data)).ToListAsync(cancellationToken);
    }
}
