using System.Globalization;
using ControleFinanceiro.Domain.Contas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ControleFinanceiro.Infrastructure.Persistence;

/// <summary>Mapeia o domínio financeiro para as tabelas persistidas no SQLite.</summary>
public sealed class ControleFinanceiroDbContext(
    DbContextOptions<ControleFinanceiroDbContext> opcoes) : DbContext(opcoes)
{
    /// <summary>Obtém o conjunto persistido de categorias.</summary>
    public DbSet<CategoriaConta> Categorias => Set<CategoriaConta>();
    /// <summary>Obtém o conjunto persistido de contas.</summary>
    public DbSet<Conta> Contas => Set<Conta>();
    /// <summary>Obtém o conjunto persistido de compromissos individuais.</summary>
    public DbSet<Compromisso> Compromissos => Set<Compromisso>();
    /// <summary>Obtém os registros de auditoria dos compromissos.</summary>
    public DbSet<HistoricoCompromisso> HistoricosCompromisso => Set<HistoricoCompromisso>();

    /// <summary>Configura tabelas, relacionamentos, restrições e conversões SQLite.</summary>
    /// <param name="modelBuilder">Construtor do modelo do Entity Framework Core.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var conversorDecimal = new ValueConverter<decimal, string>(
            valor => valor.ToString(CultureInfo.InvariantCulture),
            valor => decimal.Parse(valor, CultureInfo.InvariantCulture));

        modelBuilder.Entity<CategoriaConta>(entidade =>
        {
            entidade.ToTable("CategoriasContas");
            entidade.HasKey(categoria => categoria.Id);
            entidade.Property(categoria => categoria.Nome)
                .HasMaxLength(80)
                .UseCollation("NOCASE")
                .IsRequired();
            entidade.Property(categoria => categoria.Cor).HasMaxLength(7);
            entidade.Property(categoria => categoria.Ativa).IsRequired();
            entidade.HasIndex(categoria => categoria.Nome)
                .IsUnique()
                .HasFilter("\"Ativa\" = 1");
        });

        modelBuilder.Entity<Conta>(entidade =>
        {
            entidade.ToTable("Contas");
            entidade.HasKey(conta => conta.Id);
            entidade.Property(conta => conta.Nome).HasMaxLength(120).IsRequired();
            entidade.Property(conta => conta.ValorPrevisto)
                .HasConversion(conversorDecimal)
                .HasColumnType("TEXT")
                .IsRequired();
            entidade.Property(conta => conta.Descricao).HasMaxLength(500);
            entidade.Property(conta => conta.Tipo).HasConversion<string>().IsRequired();
            entidade.Property(conta => conta.DataVencimentoInicial).HasColumnType("TEXT");
            entidade.Property(conta => conta.SequenciaReferenciaMensal).IsRequired();
            entidade.Property(conta => conta.Ativa).IsRequired();
            entidade.HasOne(conta => conta.Categoria)
                .WithMany(categoria => categoria.Contas)
                .HasForeignKey(conta => conta.CategoriaContaId)
                .OnDelete(DeleteBehavior.Restrict);
            entidade.HasMany(conta => conta.Compromissos)
                .WithOne(compromisso => compromisso.Conta)
                .HasForeignKey(compromisso => compromisso.ContaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Compromisso>(entidade =>
        {
            entidade.ToTable("Compromissos");
            entidade.HasKey(compromisso => compromisso.Id);
            entidade.Property(compromisso => compromisso.Vencimento).HasColumnType("TEXT");
            entidade.Property(compromisso => compromisso.ValorPrevisto)
                .HasConversion(conversorDecimal)
                .HasColumnType("TEXT")
                .IsRequired();
            entidade.Property(compromisso => compromisso.SituacaoPagamento)
                .HasConversion<string>()
                .IsRequired();
            entidade.Property(compromisso => compromisso.PossuiHistoricoFinanceiro).IsRequired();
            entidade.Property(compromisso => compromisso.Cancelado).IsRequired();
            entidade.Property(compromisso => compromisso.MotivoCancelamento).HasMaxLength(200);
            entidade.HasIndex(compromisso => new { compromisso.ContaId, compromisso.Sequencia })
                .IsUnique();
            entidade.HasIndex(compromisso => compromisso.Vencimento);
            entidade.HasIndex(compromisso => new { compromisso.CategoriaContaId, compromisso.Vencimento });
            entidade.HasOne<CategoriaConta>()
                .WithMany()
                .HasForeignKey(compromisso => compromisso.CategoriaContaId)
                .OnDelete(DeleteBehavior.Restrict);
            entidade.HasMany(compromisso => compromisso.Historico)
                .WithOne()
                .HasForeignKey(historico => historico.CompromissoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HistoricoCompromisso>(entidade =>
        {
            entidade.ToTable("HistoricosCompromissos");
            entidade.HasKey(historico => historico.Id);
            entidade.Property(historico => historico.ValorAnterior)
                .HasConversion(conversorDecimal)
                .HasColumnType("TEXT");
            entidade.Property(historico => historico.ValorNovo)
                .HasConversion(conversorDecimal)
                .HasColumnType("TEXT");
            entidade.Property(historico => historico.VencimentoAnterior).HasColumnType("TEXT");
            entidade.Property(historico => historico.VencimentoNovo).HasColumnType("TEXT");
            entidade.Property(historico => historico.Motivo).HasMaxLength(200).IsRequired();
            entidade.HasOne<CategoriaConta>()
                .WithMany()
                .HasForeignKey(historico => historico.CategoriaAnteriorId)
                .OnDelete(DeleteBehavior.Restrict);
            entidade.HasOne<CategoriaConta>()
                .WithMany()
                .HasForeignKey(historico => historico.CategoriaNovaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
