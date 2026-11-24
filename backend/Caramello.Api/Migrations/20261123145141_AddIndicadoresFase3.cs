using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caramello.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicadoresFase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EstoqueSeguranca",
                table: "Insumos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "PrazoReposicaoDias",
                table: "Insumos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RegistrosBorra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Quantidade = table.Column<long>(type: "INTEGER", nullable: false),
                    Unidade = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Destinacao = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosBorra", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosBorra_Data",
                table: "RegistrosBorra",
                column: "Data");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosBorra");

            migrationBuilder.DropColumn(
                name: "EstoqueSeguranca",
                table: "Insumos");

            migrationBuilder.DropColumn(
                name: "PrazoReposicaoDias",
                table: "Insumos");
        }
    }
}
