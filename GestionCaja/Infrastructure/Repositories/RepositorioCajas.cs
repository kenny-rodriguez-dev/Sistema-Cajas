using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositorioCajas : IRepositorioCajas
{
    private readonly AppDbContext _contexto;

    public RepositorioCajas(AppDbContext contexto) => _contexto = contexto;
    // El "=>" en vez de llaves es un método de una sola expresión.
    // Es solo una forma corta de escribir lo mismo.

    public async Task<List<Caja>> ListarConDetalleAsync() =>
        await _contexto.Cajas
            .Where(c => c.Activo)
            .Include(c => c.Asignaciones).ThenInclude(a => a.Usuario)
            .Include(c => c.Sesiones.Where(s => s.FechaCierre == null)).ThenInclude(s => s.Usuario)
            .ToListAsync();
    // ThenInclude = "y de eso que incluiste, incluye también lo suyo".
    // Include trae las asignaciones; ThenInclude trae el usuario de cada una.
    // Sin esto, Asignaciones[0].Usuario vendría en null.

    public async Task<Caja?> ObtenerAsync(int cajaId) =>
        await _contexto.Cajas.FirstOrDefaultAsync(c => c.Id == cajaId && c.Activo);

    public async Task<Usuario?> ObtenerUsuarioAsync(int usuarioId) =>
        await _contexto.Usuarios.Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo);

    public async Task<int> ContarAsignadosAsync(int cajaId) =>
        await _contexto.AsignacionesCaja.CountAsync(a => a.CajaId == cajaId);

    public async Task<bool> EstaAsignadoAsync(int cajaId, int usuarioId) =>
        await _contexto.AsignacionesCaja.AnyAsync(a => a.CajaId == cajaId && a.UsuarioId == usuarioId);

    public async Task AsignarAsync(AsignacionCaja asignacion)
    {
        _contexto.AsignacionesCaja.Add(asignacion);
        await _contexto.SaveChangesAsync();
    }

    public async Task<SesionCaja?> ObtenerSesionAbiertaDeUsuarioAsync(int usuarioId) =>
        await _contexto.SesionesCaja
            .FirstOrDefaultAsync(s => s.UsuarioId == usuarioId && s.FechaCierre == null);

    public async Task<bool> HaySesionAbiertaEnCajaAsync(int cajaId) =>
        await _contexto.SesionesCaja.AnyAsync(s => s.CajaId == cajaId && s.FechaCierre == null);

    public async Task<bool> AbrirSesionAsync(SesionCaja sesion)
    {
        try
        {
            _contexto.SesionesCaja.Add(sesion);
            await _contexto.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // ═══ LA RED DE SEGURIDAD CONTRA LA CARRERA ═══
            //
            // Si llegamos aquí, es porque el índice único filtrado
            // rechazó el INSERT: alguien más abrió una sesión en esa
            // caja en el instante entre mi verificación y mi escritura.
            //
            // Es raro, pero es EXACTAMENTE el caso que mi "if" no puede
            // cubrir. Devuelvo false y el controlador responde 409.

            _contexto.ChangeTracker.Clear();
            // Limpio el rastreador: el DbContext quedó con una fila
            // marcada como "pendiente de insertar" que ya nunca se va
            // a insertar. Si no la limpio, la próxima operación con
            // este mismo contexto reintentaría el INSERT fallido.

            return false;
        }
    }

    public async Task CerrarSesionAsync(SesionCaja sesion)
    {
        sesion.FechaCierre = DateTime.UtcNow;
        await _contexto.SaveChangesAsync();
        // Ni siquiera hace falta un Update(): el DbContext viene
        // siguiendo ese objeto desde que lo leyó de la base, y detecta
        // solo que la propiedad cambió. Eso se llama change tracking.
    }
}