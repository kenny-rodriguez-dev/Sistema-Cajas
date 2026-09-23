using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MenuAsignacionCajas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "OpcionesMenu",
                columns: new[] { "Id", "Activo", "Etiqueta", "Orden", "Ruta" },
                values: new object[] { 8, true, "Asignación de cajas", 8, "/cajas" });

            migrationBuilder.InsertData(
                table: "OpcionesMenuRoles",
                columns: new[] { "OpcionMenuId", "RolId" },
                values: new object[] { 8, 2 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "OpcionesMenuRoles",
                keyColumns: new[] { "OpcionMenuId", "RolId" },
                keyValues: new object[] { 8, 2 });

            migrationBuilder.DeleteData(
                table: "OpcionesMenu",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
