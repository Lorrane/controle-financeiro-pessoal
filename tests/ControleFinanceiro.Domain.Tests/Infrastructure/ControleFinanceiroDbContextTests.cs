using ControleFinanceiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Domain.Tests.Infrastructure;

public sealed class ControleFinanceiroDbContextTests
{
    [Fact]
    public void UsesSqliteProvider()
    {
        var options = new DbContextOptionsBuilder<ControleFinanceiroDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new ControleFinanceiroDbContext(options);

        Assert.True(context.Database.IsSqlite());
    }
}
