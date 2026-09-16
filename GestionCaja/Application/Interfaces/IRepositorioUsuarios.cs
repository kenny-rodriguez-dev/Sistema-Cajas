using Domain.Entities;

namespace Application.Interfaces;

public interface IRepositorioUsuarios
{
    Task<Usuario?> ObtenerConRolAsync(int usuarioId);
    Task<List<Usuario>> ListarAsync();
    Task<bool> ExisteUsernameAsync(string username);
    Task<bool> ExisteRolAsync(int rolId);
    Task CrearAsync(Usuario usuario);
    Task GuardarAsync();
    Task<int> ContarAdministradoresActivosAsync();
}