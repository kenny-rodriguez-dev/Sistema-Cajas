using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CrearTurnos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TiposAtencion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposAtencion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContadoresTurno",
                columns: table => new
                {
                    TipoAtencionId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    UltimoNumero = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContadoresTurno", x => new { x.TipoAtencionId, x.Fecha });
                    table.ForeignKey(
                        name: "FK_ContadoresTurno_TiposAtencion_TipoAtencionId",
                        column: x => x.TipoAtencionId,
                        principalTable: "TiposAtencion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Turnos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nchar(6)", fixedLength: true, maxLength: 6, nullable: false),
                    TipoAtencionId = table.Column<int>(type: "int", nullable: false),
                    CajaId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    AtendidoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    AsignadoPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaHoraEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaHoraAtencion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turnos", x => x.Id);
                    table.CheckConstraint("CK_Turno_Estado", "[Estado] IN ('PEN','ATE','CAN')");
                    table.ForeignKey(
                        name: "FK_Turnos_Cajas_CajaId",
                        column: x => x.CajaId,
                        principalTable: "Cajas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_TiposAtencion_TipoAtencionId",
                        column: x => x.TipoAtencionId,
                        principalTable: "TiposAtencion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_Usuarios_AsignadoPorUsuarioId",
                        column: x => x.AsignadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_Usuarios_AtendidoPorUsuarioId",
                        column: x => x.AtendidoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "TiposAtencion",
                columns: new[] { "Id", "Activo", "Codigo", "Descripcion" },
                values: new object[,]
                {
                    { 1, true, "AC", "Atención al cliente" },
                    { 2, true, "PS", "Pago de servicio" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TiposAtencion_Codigo",
                table: "TiposAtencion",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_AsignadoPorUsuarioId",
                table: "Turnos",
                column: "AsignadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_AtendidoPorUsuarioId",
                table: "Turnos",
                column: "AtendidoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_CajaId",
                table: "Turnos",
                column: "CajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_ClienteId",
                table: "Turnos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_Codigo_Fecha",
                table: "Turnos",
                columns: new[] { "Codigo", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_TipoAtencionId",
                table: "Turnos",
                column: "TipoAtencionId");

            // ═══════════════════════════════════════════════════════════
            //   STORED PROCEDURE (requisito del enunciado)
            // ═══════════════════════════════════════════════════════════
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_SiguienteNumeroTurno
    @TipoAtencionId INT,
    @Fecha          DATE,
    @Numero         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;

    -- Intento subir el contador que ya existe para este tipo y esta fecha.
    -- La doble asignación ""@Numero = UltimoNumero = UltimoNumero + 1""
    -- lee y escribe EN LA MISMA SENTENCIA. No existe ningún instante
    -- entre una cosa y la otra: por eso dos peticiones simultaneas
    -- reciben números distintos, y no el mismo.
    UPDATE ContadoresTurno WITH (UPDLOCK, SERIALIZABLE)
    SET @Numero = UltimoNumero = UltimoNumero + 1
    WHERE TipoAtencionId = @TipoAtencionId
      AND Fecha = @Fecha;

    -- Si no actualizó ninguna fila, es el primer turno del día:
    -- hay que crear el contador.
    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO ContadoresTurno (TipoAtencionId, Fecha, UltimoNumero)
        VALUES (@TipoAtencionId, @Fecha, 1);
        SET @Numero = 1;
    END

    COMMIT TRANSACTION;
END
");

            // Y para poder deshacerlo, en el metodo Down() va:
            //   migrationBuilder.Sql(""DROP PROCEDURE IF EXISTS sp_SiguienteNumeroTurno"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_SiguienteNumeroTurno");
            migrationBuilder.DropTable(
                name: "ContadoresTurno");

            migrationBuilder.DropTable(
                name: "Turnos");

            migrationBuilder.DropTable(
                name: "TiposAtencion");
        }
    }
}
