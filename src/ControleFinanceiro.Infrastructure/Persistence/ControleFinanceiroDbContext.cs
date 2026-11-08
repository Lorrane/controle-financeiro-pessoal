using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence;

public sealed class ControleFinanceiroDbContext(
    DbContextOptions<ControleFinanceiroDbContext> options) : DbContext(options)
{
}
