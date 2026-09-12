using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMenuCargaMasiva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "OpcionesMenu",
                columns: new[] { "Id", "Activo", "Etiqueta", "Orden", "Ruta" },
                values: new object[] { 7, true, "Carga masiva", 7, "/clientes/carga" });

            migrationBuilder.InsertData(
                table: "OpcionesMenuRoles",
                columns: new[] { "OpcionMenuId", "RolId" },
                values: new object[] { 7, 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "OpcionesMenuRoles",
                keyColumns: new[] { "OpcionMenuId", "RolId" },
                keyValues: new object[] { 7, 3 });

            migrationBuilder.DeleteData(
                table: "OpcionesMenu",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
