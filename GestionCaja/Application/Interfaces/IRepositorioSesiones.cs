using Domain.Entities;

namespace Application.Interfaces;

public interface IRepositorioSesiones
{
    Task CrearAsync(SesionUsuario sesion);
    Task<bool> EstaActivaAsync(string jti, DateTime ahora);
    Task<SesionUsuario?> ObtenerPorJtiAsync(string jti);
    Task<int> CerrarTodasDeUsuarioAsync(int usuarioId, DateTime ahora, string motivo);
    Task<Dictionary<int, int>> ContarActivasPorUsuarioAsync(DateTime ahora);
    Task GuardarAsync();
}