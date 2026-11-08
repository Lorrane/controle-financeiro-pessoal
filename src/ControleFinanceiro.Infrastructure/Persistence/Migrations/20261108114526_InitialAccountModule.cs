using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAccountModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasContas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false, collation: "NOCASE"),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasContas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ValorPrevisto = table.Column<string>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CategoriaContaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    DataVencimentoInicial = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    TotalParcelas = table.Column<int>(type: "INTEGER", nullable: true),
                    ParcelaAtual = table.Column<int>(type: "INTEGER", nullable: true),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contas_CategoriasContas_CategoriaContaId",
                        column: x => x.CategoriaContaId,
                        principalTable: "CategoriasContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Compromissos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sequencia = table.Column<int>(type: "INTEGER", nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ValorPrevisto = table.Column<string>(type: "TEXT", nullable: false),
                    CategoriaContaId = table.Column<int>(type: "INTEGER", nullable: false),
                    SituacaoPagamento = table.Column<string>(type: "TEXT", nullable: false),
                    PossuiHistoricoFinanceiro = table.Column<bool>(type: "INTEGER", nullable: false),
                    Cancelado = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanceladoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    MotivoCancelamento = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compromissos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compromissos_CategoriasContas_CategoriaContaId",
                        column: x => x.CategoriaContaId,
                        principalTable: "CategoriasContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Compromissos_Contas_ContaId",
                        column: x => x.ContaId,
                        principalTable: "Contas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistoricosCompromissos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompromissoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValorAnterior = table.Column<string>(type: "TEXT", nullable: false),
                    CategoriaAnteriorId = table.Column<int>(type: "INTEGER", nullable: false),
                    VencimentoAnterior = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ValorNovo = table.Column<string>(type: "TEXT", nullable: false),
                    CategoriaNovaId = table.Column<int>(type: "INTEGER", nullable: false),
                    VencimentoNovo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AlteradoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricosCompromissos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricosCompromissos_CategoriasContas_CategoriaAnteriorId",
                        column: x => x.CategoriaAnteriorId,
                        principalTable: "CategoriasContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoricosCompromissos_CategoriasContas_CategoriaNovaId",
                        column: x => x.CategoriaNovaId,
                        principalTable: "CategoriasContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoricosCompromissos_Compromissos_CompromissoId",
                        column: x => x.CompromissoId,
                        principalTable: "Compromissos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasContas_Nome",
                table: "CategoriasContas",
                column: "Nome",
                unique: true,
                filter: "\"Ativa\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Compromissos_CategoriaContaId_Vencimento",
                table: "Compromissos",
                columns: new[] { "CategoriaContaId", "Vencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_Compromissos_ContaId_Sequencia",
                table: "Compromissos",
                columns: new[] { "ContaId", "Sequencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compromissos_Vencimento",
                table: "Compromissos",
                column: "Vencimento");

            migrationBuilder.CreateIndex(
                name: "IX_Contas_CategoriaContaId",
                table: "Contas",
                column: "CategoriaContaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosCompromissos_CategoriaAnteriorId",
                table: "HistoricosCompromissos",
                column: "CategoriaAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosCompromissos_CategoriaNovaId",
                table: "HistoricosCompromissos",
                column: "CategoriaNovaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosCompromissos_CompromissoId",
                table: "HistoricosCompromissos",
                column: "CompromissoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricosCompromissos");

            migrationBuilder.DropTable(
                name: "Compromissos");

            migrationBuilder.DropTable(
                name: "Contas");

            migrationBuilder.DropTable(
                name: "CategoriasContas");
        }
    }
}
