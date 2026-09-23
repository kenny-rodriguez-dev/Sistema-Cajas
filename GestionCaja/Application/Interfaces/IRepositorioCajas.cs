using Domain.Entities;

namespace Application.Interfaces;

public interface IRepositorioCajas
{
    Task<List<Caja>> ListarConDetalleAsync();
    Task<Caja?> ObtenerAsync(int cajaId);
    Task<Usuario?> ObtenerUsuarioAsync(int usuarioId);
    Task<int> ContarAsignadosAsync(int cajaId);
    Task<bool> EstaAsignadoAsync(int cajaId, int usuarioId);
    Task AsignarAsync(AsignacionCaja asignacion);
    Task<SesionCaja?> ObtenerSesionAbiertaDeUsuarioAsync(int usuarioId);
    Task<bool> HaySesionAbiertaEnCajaAsync(int cajaId);
    Task<bool> AbrirSesionAsync(SesionCaja sesion);   // false = la base la rechazó
    Task CerrarSesionAsync(SesionCaja sesion);
    Task<List<Caja>> ListarDeCajeroAsync(int usuarioId);
    Task QuitarAsignacionAsync(int cajaId, int usuarioId);
}