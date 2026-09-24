using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositorioUsuarios : IRepositorioUsuarios
{
    private readonly AppDbContext _contexto;

    public RepositorioUsuarios(AppDbContext contexto) => _contexto = contexto;

    public async Task<Usuario?> ObtenerConRolAsync(int usuarioId) =>
        await _contexto.Usuarios.Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

    public async Task<Usuario?> ObtenerPorUsernameAsync(string username) =>
        await _contexto.Usuarios.Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Username == username);
    // NUEVO. Lo necesita el login para dejar de usar el DbContext directo.

    public async Task<List<Usuario>> ListarAsync() =>
        await _contexto.Usuarios.Include(u => u.Rol)
            .OrderByDescending(u => u.FechaCreacion)
            .ToListAsync();

    public async Task<bool> ExisteUsernameAsync(string username) =>
        await _contexto.Usuarios.AnyAsync(u => u.Username == username);

    public async Task<bool> ExisteRolAsync(int rolId) =>
        await _contexto.Roles.AnyAsync(r => r.Id == rolId);

    public async Task CrearAsync(Usuario usuario)
    {
        _contexto.Usuarios.Add(usuario);
        await _contexto.SaveChangesAsync();
    }

    public async Task GuardarAsync() => await _contexto.SaveChangesAsync();

    public async Task<int> ContarAdministradoresActivosAsync() =>
        await _contexto.Usuarios.CountAsync(u => u.RolId == 1 && u.Activo);
}