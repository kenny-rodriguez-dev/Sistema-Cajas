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

    public async Task<List<string>> ObtenerIdentificacionesAsync(List<string> identificaciones) =>
    await _contexto.Clientes
        .Where(c => identificaciones.Contains(c.Identificacion))
        .Select(c => c.Identificacion)
        .ToListAsync();
    // ═══ UNA SOLA CONSULTA, NO UNA POR FILA ═══
    //
    // Lo ingenuo sería recorrer las 500 filas preguntando una por una:
    // 500 viajes a la base de datos.
    //
    // Contains() se traduce a "WHERE Identificacion IN (...)": UN viaje.
    // Ese problema tiene nombre y te lo van a preguntar: N+1 queries.

    public async Task AgregarVariosAsync(List<Cliente> clientes)
    {
        _contexto.Clientes.AddRange(clientes);
        await _contexto.SaveChangesAsync();
        // AddRange + un solo SaveChanges = EF agrupa todos los INSERT
        // en una sola transacción. Si uno falla, no entra ninguno.
    }
}