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
        await _contexto.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);
}