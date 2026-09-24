using Domain.Entities;

namespace Application.Interfaces;

public interface IRepositorioUsuarios
{
    Task<Usuario?> ObtenerConRolAsync(int usuarioId);
    Task<Usuario?> ObtenerPorUsernameAsync(string username);
    Task<List<Usuario>> ListarAsync();
    Task<bool> ExisteUsernameAsync(string username);
    Task<bool> ExisteRolAsync(int rolId);
    Task CrearAsync(Usuario usuario);
    Task GuardarAsync();
    Task<int> ContarAdministradoresActivosAsync();
}