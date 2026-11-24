using Caramello.Api.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Caramello.Api.Infrastructure;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IHttpContextAccessor? httpContextAccessor = null)
    : IdentityDbContext<ApplicationUser>(options)
{
    private bool _salvandoAuditoria;

    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<Descarte> Descartes => Set<Descarte>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<RegistroBorra> RegistrosBorra => Set<RegistroBorra>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_salvandoAuditoria) return await base.SaveChangesAsync(cancellationToken);

        var alteracoes = ChangeTracker.Entries()
            .Where(x => x.Entity is not RegistroAuditoria &&
                        x.Entity is not Microsoft.AspNetCore.Identity.IdentityUser &&
                        x.Entity is not Microsoft.AspNetCore.Identity.IdentityRole &&
                        x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(x => (Entrada: x, Operacao: NomeOperacao(x.State)))
            .ToList();

        var resultado = await base.SaveChangesAsync(cancellationToken);
        if (alteracoes.Count == 0) return resultado;

        _salvandoAuditoria = true;
        try
        {
            var usuario = httpContextAccessor?.HttpContext?.User.Identity?.Name ?? "sistema-local";
            foreach (var alteracao in alteracoes)
            {
                RegistrosAuditoria.Add(new RegistroAuditoria
                {
                    Usuario = usuario,
                    DataUtc = DateTime.UtcNow,
                    Operacao = alteracao.Operacao,
                    Entidade = alteracao.Entrada.Metadata.ClrType.Name,
                    IdentificadorRegistro = ObterChave(alteracao.Entrada)
                });
            }
            await base.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _salvandoAuditoria = false;
        }
        return resultado;
    }

    private static string NomeOperacao(EntityState state) => state switch
    {
        EntityState.Added => "Criação",
        EntityState.Modified => "Edição",
        EntityState.Deleted => "Exclusão",
        _ => state.ToString()
    };

    private static string ObterChave(EntityEntry entry)
    {
        var chave = entry.Metadata.FindPrimaryKey();
        return chave is null ? "não disponível" : string.Join(",", chave.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "null"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<RegistroAuditoria>(entity =>
        {
            entity.Property(x => x.Usuario).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Operacao).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Entidade).HasMaxLength(120).IsRequired();
            entity.Property(x => x.IdentificadorRegistro).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.DataUtc);
        });
        modelBuilder.Entity<Insumo>(entity =>
        {
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Categoria).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Unidade).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Quantidade).HasConversion(v => decimal.ToInt64(v * 1000m), v => v / 1000m);
            entity.Property(x => x.EstoqueMinimo).HasConversion(v => decimal.ToInt64(v * 1000m), v => v / 1000m);
            entity.Property(x => x.EstoqueSeguranca).HasConversion(v => decimal.ToInt64(v * 1000m), v => v / 1000m);
        });
        modelBuilder.Entity<MovimentacaoEstoque>(entity =>
        {
            entity.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Quantidade).HasConversion(v => decimal.ToInt64(v * 1000m), v => v / 1000m);
            entity.Property(x => x.Unidade).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Motivo).HasMaxLength(300);
            entity.HasOne(x => x.Insumo).WithMany(x => x.Movimentacoes).HasForeignKey(x => x.InsumoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.InsumoId, x.Data });
        });
        modelBuilder.Entity<Descarte>(entity =>
        {
            entity.Property(x => x.Material).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Unidade).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Motivo).HasMaxLength(300);
            entity.Property(x => x.Quantidade).HasConversion(v => decimal.ToInt64(v * 1000m), v => v / 1000m);
        });
        modelBuilder.Entity<RegistroBorra>(entity =>
        {
            entity.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Quantidade).HasConversion(v => decimal.ToInt64(v * 1000m), v => v / 1000m);
            entity.Property(x => x.Unidade).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Destinacao).HasMaxLength(120);
            entity.Property(x => x.Observacao).HasMaxLength(300);
            entity.HasIndex(x => x.Data);
        });
    }
}
