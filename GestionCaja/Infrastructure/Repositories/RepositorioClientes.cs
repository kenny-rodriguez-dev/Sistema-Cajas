using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

// LA IMPLEMENTACIÓN. Aquí sí vive Entity Framework, y aquí SOLO.
//
// Los dos puntos significan "yo cumplo ese contrato". Si me falta
// un método de la interfaz, el compilador no me deja compilar.
public class RepositorioClientes : IRepositorioClientes
{
    private readonly AppDbContext _contexto;

    public RepositorioClientes(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<List<Cliente>> ListarActivosAsync()
    {
        return await _contexto.Clientes
            .Where(c => c.Activo)
            .ToListAsync();
    }

    public async Task<Cliente?> ObtenerPorIdAsync(int id)
    {
        return await _contexto.Clientes
            .FirstOrDefaultAsync(c => c.Id == id && c.Activo);
    }

    public async Task<bool> ExisteIdentificacionAsync(string identificacion)
    {
        return await _contexto.Clientes
            .AnyAsync(c => c.Identificacion == identificacion);
    }

    public async Task AgregarAsync(Cliente cliente)
    {
        _contexto.Clientes.Add(cliente);
        await _contexto.SaveChangesAsync();
    }
}