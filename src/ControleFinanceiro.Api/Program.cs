using System.Text.Json;
using System.Text.Json.Serialization;
using ControleFinanceiro.Application.Contas;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Infrastructure;
using ControleFinanceiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var construtor = WebApplication.CreateBuilder(args);

construtor.Services.AddControllers()
    .AddJsonOptions(opcoesJson =>
        opcoesJson.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
construtor.Services.AddOpenApi();
construtor.Services.RegistrarInfraestrutura(construtor.Configuration);
construtor.Services.AddScoped<ServicoDeContas>();
construtor.Services.AddScoped<ServicoDeCategorias>();
construtor.Services.AddSingleton(TimeProvider.System);

var origensPermitidas = construtor.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? throw new InvalidOperationException("A configuração Cors:AllowedOrigins é obrigatória.");

construtor.Services.AddCors(opcoesCors =>
{
    opcoesCors.AddDefaultPolicy(politica => politica
        .WithOrigins(origensPermitidas)
        .AllowAnyHeader()
        .AllowAnyMethod());
});
construtor.Services.AddHealthChecks();

var aplicacao = construtor.Build();

aplicacao.Use(async (contexto, proximo) =>
{
    try
    {
        await proximo();
    }
    catch (ExcecaoRegraDominio excecao)
    {
        contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
        await contexto.Response.WriteAsJsonAsync(new { erro = excecao.Message });
    }
});

using (var escopo = aplicacao.Services.CreateScope())
{
    var contextoBanco = escopo.ServiceProvider.GetRequiredService<ControleFinanceiroDbContext>();
    contextoBanco.Database.Migrate();
}

if (aplicacao.Environment.IsDevelopment())
{
    aplicacao.MapOpenApi();
}

aplicacao.UseHttpsRedirection();
aplicacao.UseCors();
aplicacao.UseAuthorization();
aplicacao.MapControllers();
aplicacao.MapHealthChecks("/health");

aplicacao.Run();
