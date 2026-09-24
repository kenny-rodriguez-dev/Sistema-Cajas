/* ============================================================================
   GESTIÓN DE CAJA — STORED PROCEDURES
   ----------------------------------------------------------------------------
   Motor:   SQL Server 2022
   Base:    GestionCaja

   Estos procedimientos se crean automáticamente con las migraciones de
   Entity Framework (proyecto Infrastructure/Migrations):

     sp_SiguienteNumeroTurno   ->  migración CrearTurnos
     sp_IndicadoresDashboard   ->  migración DashboardStoredProcedure,
                                   modificado por BloqueoPorIntentos

   Este archivo reúne su VERSIÓN ACTUAL para consulta y entrega.
   No hace falta ejecutarlo si la base se creó con las migraciones.
   ============================================================================ */

USE GestionCaja;
GO


/* ============================================================================
   1. sp_SiguienteNumeroTurno
   ----------------------------------------------------------------------------
   Entrega el siguiente número correlativo para un tipo de atención en una
   fecha. Con él se arma el código del turno: AC0001, AC0002, PS0001...

   POR QUÉ UN STORED PROCEDURE:
   Leer el último número y sumarle uno debe ser UNA operación indivisible.
   Con un SELECT seguido de un INSERT desde la aplicación, dos cajeros
   simultáneos leerían el mismo valor y generarían el mismo ticket.

   La asignación doble  @Numero = UltimoNumero = UltimoNumero + 1  lee y
   escribe en la misma sentencia. UPDLOCK + SERIALIZABLE bloquean la fila
   mientras dura la transacción.

   Un índice único no serviría aquí: rechazaría al segundo cajero, que sí
   tiene derecho a su ticket. No hay que rechazarlo: hay que darle el
   número siguiente.
   ============================================================================ */

CREATE OR ALTER PROCEDURE sp_SiguienteNumeroTurno
    @TipoAtencionId INT,
    @Fecha          DATE,
    @Numero         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;

    UPDATE ContadoresTurno WITH (UPDLOCK, SERIALIZABLE)
    SET @Numero = UltimoNumero = UltimoNumero + 1
    WHERE TipoAtencionId = @TipoAtencionId
      AND Fecha = @Fecha;

    -- Primer turno del día para este tipo: el contador todavía no existe.
    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO ContadoresTurno (TipoAtencionId, Fecha, UltimoNumero)
        VALUES (@TipoAtencionId, @Fecha, 1);
        SET @Numero = 1;
    END

    COMMIT TRANSACTION;
END
GO

/* Ejemplo de uso:

   DECLARE @n INT;
   EXEC sp_SiguienteNumeroTurno
        @TipoAtencionId = 1,
        @Fecha          = '2026-09-24',
        @Numero         = @n OUTPUT;
   SELECT @n AS NumeroEntregado;
*/


/* ============================================================================
   2. sp_IndicadoresDashboard
   ----------------------------------------------------------------------------
   Devuelve TRES conjuntos de resultados en un solo viaje a la base:

     1. Indicadores de usuarios (una fila)
     2. Indicadores por caja    (una fila por caja)
     3. Totales del periodo     (una fila)

   POR QUÉ UN STORED PROCEDURE:
   Con consultas separadas desde la aplicación serían varios viajes a la
   base, uno por indicador. Aquí sale todo en uno. La aplicación los lee en
   orden con NextResultAsync().

   SUM(CASE WHEN ... THEN 1 ELSE 0 END) cuenta varias condiciones distintas
   recorriendo la tabla una sola vez.

   Bloqueados se compara con SYSUTCDATETIME() y no con GETDATE(): el campo
   BloqueadoHasta se guarda en UTC, y GETDATE() devuelve la hora local del
   servidor.
   ============================================================================ */

CREATE OR ALTER PROCEDURE sp_IndicadoresDashboard
    @Desde DATETIME2,
    @Hasta DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Conjunto 1: indicadores de usuarios ──────────────────────────────
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

    -- ── Conjunto 2: una fila por caja ────────────────────────────────────
    SELECT
        c.Nombre AS Caja,
        (SELECT COUNT(*) FROM AsignacionesCaja WHERE CajaId = c.Id)         AS CajerosAsignados,
        (SELECT COUNT(*) FROM Turnos
          WHERE CajaId = c.Id AND Estado = 'ATE'
            AND FechaHoraEmision BETWEEN @Desde AND @Hasta)                 AS TurnosAtendidos,
        (SELECT TOP 1 u.Username
           FROM SesionesCaja s
           INNER JOIN Usuarios u ON u.Id = s.UsuarioId
          WHERE s.CajaId = c.Id AND s.FechaCierre IS NULL)                  AS OcupadaPor
    FROM Cajas c
    WHERE c.Activo = 1
    ORDER BY c.Nombre;

    -- ── Conjunto 3: totales del periodo ──────────────────────────────────
    SELECT
        (SELECT COUNT(*) FROM Turnos
          WHERE FechaHoraEmision BETWEEN @Desde AND @Hasta)                 AS TurnosEmitidos,
        (SELECT COUNT(*) FROM Turnos
          WHERE Estado = 'ATE' AND FechaHoraEmision BETWEEN @Desde AND @Hasta) AS TurnosAtendidos,
        (SELECT COUNT(*) FROM Turnos
          WHERE Estado = 'PEN' AND FechaHoraEmision BETWEEN @Desde AND @Hasta) AS TurnosPendientes,
        (SELECT COUNT(*) FROM Contratos
          WHERE Estado IN ('VIG', 'REN'))                                   AS ContratosActivos;
END
GO

/* Ejemplo de uso:

   EXEC sp_IndicadoresDashboard
        @Desde = '2026-09-01T00:00:00',
        @Hasta = '2026-09-30T23:59:59.9999999';
*/
