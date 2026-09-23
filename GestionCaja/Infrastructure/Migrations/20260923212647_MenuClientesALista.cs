using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MenuClientesALista : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "OpcionesMenu",
                keyColumn: "Id",
                keyValue: 6,
                column: "Ruta",
                value: "/clientes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "OpcionesMenu",
                keyColumn: "Id",
                keyValue: 6,
                column: "Ruta",
                value: "/clientes/nuevo");
        }
    }
}
