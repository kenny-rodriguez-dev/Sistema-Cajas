using Domain.Entities;

namespace Application.Interfaces;

public interface IRepositorioUsuarios
{
    Task<Usuario?> ObtenerConRolAsync(int usuarioId);
}