using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositorioTurnos : IRepositorioTurnos
{
    private readonly AppDbContext _contexto;

    public RepositorioTurnos(AppDbContext contexto) => _contexto = contexto;

    public async Task<int> SiguienteNumeroAsync(int tipoAtencionId, DateOnly fecha)
    {
        // ═══ AQUÍ SE LLAMA AL STORED PROCEDURE ═══

        var parametroNumero = new SqlParameter
        {
            ParameterName = "@Numero",
            SqlDbType = SqlDbType.Int,
            Direction = ParameterDirection.Output
            // Output = "este parámetro no lleva datos hacia allá,
            //           trae datos de vuelta".
        };

        await _contexto.Database.ExecuteSqlRawAsync(
            "EXEC sp_SiguienteNumeroTurno @TipoAtencionId, @Fecha, @Numero OUTPUT",
            new SqlParameter("@TipoAtencionId", tipoAtencionId),
            new SqlParameter("@Fecha", fecha.ToDateTime(TimeOnly.MinValue)),
            parametroNumero
        );
        // ═══ POR QUÉ SqlParameter Y NO CONCATENAR TEXTO ═══
        //
        // Si escribiera:  $"EXEC sp_... {tipoAtencionId}"
        // estaría metiendo el valor directo dentro del SQL. Eso es
        // INYECCIÓN SQL: si el valor viniera de un usuario malicioso,
        // podría meter comandos y ejecutarlos en tu base.
        //
        // Con SqlParameter el valor viaja SEPARADO de la sentencia.
        // SQL Server nunca lo interpreta como código, solo como dato.
        //
        // Esto es lo que EF Core hace por ti automáticamente en todas
        // las consultas normales. Cuando bajas a SQL crudo, te toca a ti.

        return (int)parametroNumero.Value;
    }

    public async Task<TipoAtencion?> ObtenerTipoAsync(int tipoAtencionId) =>
        await _contexto.TiposAtencion
            .FirstOrDefaultAsync(t => t.Id == tipoAtencionId && t.Activo);

    public async Task<List<TipoAtencion>> ListarTiposAsync() =>
    await _contexto.TiposAtencion.Where(t => t.Activo).OrderBy(t => t.Codigo).ToListAsync();

    public async Task CrearAsync(Turno turno)
    {
        _contexto.Turnos.Add(turno);
        await _contexto.SaveChangesAsync();
    }

    public async Task<List<Turno>> ListarDelDiaAsync(DateOnly fecha, int? cajaId) =>
        await _contexto.Turnos
            .Include(t => t.TipoAtencion)
            .Include(t => t.Caja)
            .Include(t => t.AtendidoPorUsuario)
            .Where(t => t.Fecha == fecha)
            .Where(t => cajaId == null || t.CajaId == cajaId)
            // Filtro condicional: si cajaId viene null, la condición es
            // siempre verdadera y no filtra nada. Evita tener dos consultas.
            .OrderBy(t => t.Codigo)
            .ToListAsync();

    public async Task<Turno?> ObtenerPendienteAsync(int turnoId) =>
        await _contexto.Turnos
            .FirstOrDefaultAsync(t => t.Id == turnoId && t.Estado == "PEN");

    public async Task GuardarAsync() => await _contexto.SaveChangesAsync();

    public async Task<int> ContarAtendidosDelDiaAsync(DateOnly fecha, int? usuarioId) =>
        await _contexto.Turnos
            .Where(t => t.Fecha == fecha && t.Estado == "ATE")
            .Where(t => usuarioId == null || t.AtendidoPorUsuarioId == usuarioId)
            .CountAsync();
    // Este método es para la pantalla de bienvenida de la rebanada 7.
    // Lo dejo listo desde ya.
}