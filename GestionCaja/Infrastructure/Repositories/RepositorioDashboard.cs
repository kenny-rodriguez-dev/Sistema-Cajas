using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositorioDashboard : IRepositorioDashboard
{
    private readonly AppDbContext _contexto;

    public RepositorioDashboard(AppDbContext contexto) => _contexto = contexto;

    public async Task<DashboardDto> ObtenerIndicadoresAsync(DateTime desde, DateTime hasta)
    {
        var resultado = new DashboardDto { Desde = desde, Hasta = hasta };

        var conexion = _contexto.Database.GetDbConnection();
        // Bajo al nivel de ADO.NET: la conexión cruda, sin EF de por medio.
        // Es la única forma de leer varios conjuntos de resultados.

        await conexion.OpenAsync();

        try
        {
            using var comando = conexion.CreateCommand();
            comando.CommandText = "sp_IndicadoresDashboard";
            comando.CommandType = System.Data.CommandType.StoredProcedure;

            comando.Parameters.Add(new SqlParameter("@Desde", desde));
            comando.Parameters.Add(new SqlParameter("@Hasta", hasta));
            // Parámetros, no texto concatenado. Lo mismo de la 4B:
            // así el valor nunca se interpreta como SQL. Sin inyección.

            using var lector = await comando.ExecuteReaderAsync();

            // ─── CONJUNTO 1: usuarios ───
            if (await lector.ReadAsync())
            {
                resultado.Usuarios = new IndicadoresUsuariosDto
                {
                    Total = lector.GetInt32(lector.GetOrdinal("Total")),
                    Activos = lector.GetInt32(lector.GetOrdinal("Activos")),
                    Inactivos = lector.GetInt32(lector.GetOrdinal("Inactivos")),
                    PendientesAprobacion = lector.GetInt32(lector.GetOrdinal("PendientesAprobacion")),
                    Gestores = lector.GetInt32(lector.GetOrdinal("Gestores")),
                    Cajeros = lector.GetInt32(lector.GetOrdinal("Cajeros")),
                    ConSesionAbierta = lector.GetInt32(lector.GetOrdinal("ConSesionAbierta"))
                };
            }
            // GetOrdinal busca la columna POR NOMBRE y devuelve su posición.
            // Podría escribir GetInt32(0), GetInt32(1)... pero entonces
            // reordenar una columna del SQL rompería el C# en silencio.
            // Por nombre, es a prueba de eso.

            // ─── CONJUNTO 2: cajas ───
            await lector.NextResultAsync();
            // ↑ "pasa al siguiente SELECT". Aquí está la ventaja
            //   de todo esto: mismo viaje, siguiente tabla.

            while (await lector.ReadAsync())
            {
                resultado.Cajas.Add(new IndicadorCajaDto
                {
                    Caja = lector.GetString(lector.GetOrdinal("Caja")),
                    CajerosAsignados = lector.GetInt32(lector.GetOrdinal("CajerosAsignados")),
                    TurnosAtendidos = lector.GetInt32(lector.GetOrdinal("TurnosAtendidos")),
                    OcupadaPor = lector.IsDBNull(lector.GetOrdinal("OcupadaPor"))
                                       ? null
                                       : lector.GetString(lector.GetOrdinal("OcupadaPor"))
                    // IsDBNull ANTES de leer. En SQL, NULL no es "vacío":
                    // es ausencia de valor, y GetString sobre un NULL revienta.
                });
            }

            // ─── CONJUNTO 3: totales ───
            await lector.NextResultAsync();

            if (await lector.ReadAsync())
            {
                resultado.TurnosEmitidos = lector.GetInt32(lector.GetOrdinal("TurnosEmitidos"));
                resultado.TurnosAtendidos = lector.GetInt32(lector.GetOrdinal("TurnosAtendidos"));
                resultado.TurnosPendientes = lector.GetInt32(lector.GetOrdinal("TurnosPendientes"));
                resultado.ContratosActivos = lector.GetInt32(lector.GetOrdinal("ContratosActivos"));
            }
        }
        finally
        {
            await conexion.CloseAsync();
            // finally: la conexión se cierra pase lo que pase.
            // Una conexión que no se cierra se queda ocupando un lugar
            // del pool. Suficientes de esas y la aplicación se cuelga.
        }

        return resultado;
    }

    public async Task<int> ContarPendientesAprobacionAsync() =>
        await _contexto.Usuarios.CountAsync(u => !u.Aprobado && u.Activo);
    // Este SÍ con EF Core: es UN número suelto. No hay nada que ganar
    // bajando a SQL para una sola cuenta.

    public async Task<int> ContarTurnosAtendidosHoyAsync(DateOnly fecha, string rol, int usuarioId)
    {
        var consulta = _contexto.Turnos
            .Where(t => t.Fecha == fecha && t.Estado == "ATE");

        // ═══ EL MISMO ENDPOINT, TRES SIGNIFICADOS ═══
        consulta = rol switch
        {
            "Cajero" => consulta.Where(t => t.AtendidoPorUsuarioId == usuarioId),
            "Gestor" => consulta.Where(t => t.AsignadoPorUsuarioId == usuarioId),
            _ => consulta   // Administrador: sin filtro, todos
        };
        // "switch expression": una forma corta de escribir un switch
        // que devuelve un valor. El "_" es el caso por defecto.
        //
        // Fíjate que la consulta se va ARMANDO sin ejecutarse. EF no
        // toca la base hasta el CountAsync de abajo. Eso permite
        // construirla por partes según las condiciones.

        return await consulta.CountAsync();
    }
}