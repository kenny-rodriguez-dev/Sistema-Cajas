using Domain.Entities;

namespace Application.Interfaces;

public interface IRepositorioRecuperacion
{
    Task<Usuario?> BuscarPorCorreoAsync(string correo);
    Task InvalidarTokensDeUsuarioAsync(int usuarioId);
    Task CrearTokenAsync(TokenRecuperacion token);
    Task<List<TokenRecuperacion>> ObtenerTokensVigentesAsync();
    Task GuardarAsync();
}