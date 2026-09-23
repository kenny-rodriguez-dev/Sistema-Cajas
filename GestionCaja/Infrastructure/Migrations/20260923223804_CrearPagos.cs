using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CrearPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    Periodo = table.Column<string>(type: "nchar(7)", fixedLength: true, maxLength: 7, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    FormaPagoId = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegistradoPorUsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagos_FormasPago_FormaPagoId",
                        column: x => x.FormaPagoId,
                        principalTable: "FormasPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagos_Usuarios_RegistradoPorUsuarioId",
                        column: x => x.RegistradoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "OpcionesMenu",
                columns: new[] { "Id", "Activo", "Etiqueta", "Orden", "Ruta" },
                values: new object[] { 9, true, "Atención al cliente", 9, "/atencion" });

            migrationBuilder.InsertData(
                table: "OpcionesMenuRoles",
                columns: new[] { "OpcionMenuId", "RolId" },
                values: new object[] { 9, 3 });

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_ClienteId_Periodo",
                table: "Pagos",
                columns: new[] { "ClienteId", "Periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_ContratoId",
                table: "Pagos",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_FormaPagoId",
                table: "Pagos",
                column: "FormaPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_RegistradoPorUsuarioId",
                table: "Pagos",
                column: "RegistradoPorUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DeleteData(
                table: "OpcionesMenuRoles",
                keyColumns: new[] { "OpcionMenuId", "RolId" },
                keyValues: new object[] { 9, 3 });

            migrationBuilder.DeleteData(
                table: "OpcionesMenu",
                keyColumn: "Id",
                keyValue: 9);
        }
    }
}
