using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caramello.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInsumoAtivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Insumos",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Insumos");
        }
    }
}
