using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class MenuDinamicoPorRol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpcionesMenu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Etiqueta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ruta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcionesMenu", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpcionesMenuRoles",
                columns: table => new
                {
                    OpcionMenuId = table.Column<int>(type: "int", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcionesMenuRoles", x => new { x.OpcionMenuId, x.RolId });
                    table.ForeignKey(
                        name: "FK_OpcionesMenuRoles_OpcionesMenu_OpcionMenuId",
                        column: x => x.OpcionMenuId,
                        principalTable: "OpcionesMenu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpcionesMenuRoles_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "OpcionesMenu",
                columns: new[] { "Id", "Activo", "Etiqueta", "Orden", "Ruta" },
                values: new object[,]
                {
                    { 1, true, "Bienvenida", 1, "/bienvenida" },
                    { 2, true, "Dashboard", 2, "/dashboard" },
                    { 3, true, "Usuarios", 3, "/usuarios" },
                    { 4, true, "Asignación de turnos", 4, "/turnos" },
                    { 5, true, "Procesos de caja", 5, "/caja" },
                    { 6, true, "Clientes", 6, "/clientes/nuevo" }
                });

            migrationBuilder.InsertData(
                table: "OpcionesMenuRoles",
                columns: new[] { "OpcionMenuId", "RolId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 2 },
                    { 1, 3 },
                    { 2, 1 },
                    { 3, 1 },
                    { 4, 2 },
                    { 5, 3 },
                    { 6, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpcionesMenuRoles_RolId",
                table: "OpcionesMenuRoles",
                column: "RolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpcionesMenuRoles");

            migrationBuilder.DropTable(
                name: "OpcionesMenu");
        }
    }
}
