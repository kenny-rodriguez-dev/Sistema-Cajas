using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SesionesDeUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SesionesUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Jti = table.Column<string>(type: "nchar(36)", fixedLength: true, maxLength: 36, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoCierre = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionesUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SesionesUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SesionesUsuario_Jti",
                table: "SesionesUsuario",
                column: "Jti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SesionesUsuario_UsuarioId",
                table: "SesionesUsuario",
                column: "UsuarioId");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_IndicadoresDashboard
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*)                                                             AS Total,
        SUM(CASE WHEN u.Activo = 1 THEN 1 ELSE 0 END)                       AS Activos,
        SUM(CASE WHEN u.Activo = 0 THEN 1 ELSE 0 END)                       AS Inactivos,
        SUM(CASE WHEN u.Aprobado = 0 THEN 1 ELSE 0 END)                     AS PendientesAprobacion,
        SUM(CASE WHEN u.BloqueadoHasta > SYSUTCDATETIME() THEN 1 ELSE 0 END) AS Bloqueados,
        SUM(CASE WHEN r.Nombre = 'Gestor' THEN 1 ELSE 0 END)                AS Gestores,
        SUM(CASE WHEN r.Nombre = 'Cajero' THEN 1 ELSE 0 END)                AS Cajeros,
        (SELECT COUNT(DISTINCT s.UsuarioId) FROM SesionesUsuario s
          WHERE s.FechaCierre IS NULL
            AND s.FechaExpiracion > SYSUTCDATETIME())                       AS ConSesionActiva,
        (SELECT COUNT(*) FROM SesionesCaja WHERE FechaCierre IS NULL)       AS OperandoCaja
    FROM Usuarios u
    INNER JOIN Roles r ON r.Id = u.RolId;

    SELECT
        c.Nombre AS Caja,
        (SELECT COUNT(*) FROM AsignacionesCaja WHERE CajaId = c.Id)         AS CajerosAsignados,
        (SELECT COUNT(*) FROM Turnos
          WHERE CajaId = c.Id AND Estado = 'ATE'
            AND FechaHoraEmision BETWEEN @Desde AND @Hasta)                 AS TurnosAtendidos,
        (SELECT TOP 1 u.Username FROM SesionesCaja s
           INNER JOIN Usuarios u ON u.Id = s.UsuarioId
          WHERE s.CajaId = c.Id AND s.FechaCierre IS NULL)                  AS OcupadaPor
    FROM Cajas c
    WHERE c.Activo = 1
    ORDER BY c.Nombre;

    SELECT
        (SELECT COUNT(*) FROM Turnos
          WHERE FechaHoraEmision BETWEEN @Desde AND @Hasta)                 AS TurnosEmitidos,
        (SELECT COUNT(*) FROM Turnos
          WHERE Estado = 'ATE' AND FechaHoraEmision BETWEEN @Desde AND @Hasta) AS TurnosAtendidos,
        (SELECT COUNT(*) FROM Turnos
          WHERE Estado = 'PEN' AND FechaHoraEmision BETWEEN @Desde AND @Hasta) AS TurnosPendientes,
        (SELECT COUNT(*) FROM Contratos
          WHERE Estado IN ('VIG','REN'))                                    AS ContratosActivos;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_IndicadoresDashboard
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*)                                                             AS Total,
        SUM(CASE WHEN u.Activo = 1 THEN 1 ELSE 0 END)                       AS Activos,
        SUM(CASE WHEN u.Activo = 0 THEN 1 ELSE 0 END)                       AS Inactivos,
        SUM(CASE WHEN u.Aprobado = 0 THEN 1 ELSE 0 END)                     AS PendientesAprobacion,
        SUM(CASE WHEN u.BloqueadoHasta > SYSUTCDATETIME() THEN 1 ELSE 0 END) AS Bloqueados,
        SUM(CASE WHEN r.Nombre = 'Gestor' THEN 1 ELSE 0 END)                AS Gestores,
        SUM(CASE WHEN r.Nombre = 'Cajero' THEN 1 ELSE 0 END)                AS Cajeros,
        (SELECT COUNT(*) FROM SesionesCaja WHERE FechaCierre IS NULL)       AS ConSesionAbierta
    FROM Usuarios u
    INNER JOIN Roles r ON r.Id = u.RolId;

    SELECT
        c.Nombre AS Caja,
        (SELECT COUNT(*) FROM AsignacionesCaja WHERE CajaId = c.Id)         AS CajerosAsignados,
        (SELECT COUNT(*) FROM Turnos
          WHERE CajaId = c.Id AND Estado = 'ATE'
            AND FechaHoraEmision BETWEEN @Desde AND @Hasta)                 AS TurnosAtendidos,
        (SELECT TOP 1 u.Username FROM SesionesCaja s
           INNER JOIN Usuarios u ON u.Id = s.UsuarioId
          WHERE s.CajaId = c.Id AND s.FechaCierre IS NULL)                  AS OcupadaPor
    FROM Cajas c
    WHERE c.Activo = 1
    ORDER BY c.Nombre;

    SELECT
        (SELECT COUNT(*) FROM Turnos
          WHERE FechaHoraEmision BETWEEN @Desde AND @Hasta)                 AS TurnosEmitidos,
        (SELECT COUNT(*) FROM Turnos
          WHERE Estado = 'ATE' AND FechaHoraEmision BETWEEN @Desde AND @Hasta) AS TurnosAtendidos,
        (SELECT COUNT(*) FROM Turnos
          WHERE Estado = 'PEN' AND FechaHoraEmision BETWEEN @Desde AND @Hasta) AS TurnosPendientes,
        (SELECT COUNT(*) FROM Contratos
          WHERE Estado IN ('VIG','REN'))                                    AS ContratosActivos;
END
");

            migrationBuilder.DropTable(
                name: "SesionesUsuario");
        }
    }
}
