using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositorioClientes : IRepositorioClientes
{
    private readonly AppDbContext _contexto;

    public RepositorioClientes(AppDbContext contexto) => _contexto = contexto;

    public async Task<List<Cliente>> ListarAsync(bool incluirInactivos) =>
        await _contexto.Clientes
            .Where(c => incluirInactivos || c.Activo)
            // Si incluirInactivos es true, la condición siempre se cumple
            // y trae todos. Si es false, solo los activos. Una consulta
            // en vez de dos métodos casi iguales.
            .OrderBy(c => c.Apellidos).ThenBy(c => c.Nombres)
            .ToListAsync();

    public async Task<Cliente?> ObtenerPorIdAsync(int id) =>
        await _contexto.Clientes.FirstOrDefaultAsync(c => c.Id == id && c.Activo);

    public async Task<Cliente?> ObtenerParaEditarAsync(int id) =>
        await _contexto.Clientes.FirstOrDefaultAsync(c => c.Id == id);
    // SIN filtrar por Activo. Para reactivar a un cliente hay que
    // poder encontrarlo aunque esté desactivado.

    public async Task<Cliente?> BuscarPorIdentificacionAsync(string identificacion) =>
        await _contexto.Clientes.FirstOrDefaultAsync(c => c.Identificacion == identificacion);
    // Esta búsqueda es rapidísima aunque haya un millón de clientes:
    // la columna tiene el índice único que creaste en la rebanada 1A.
    // SQL Server va directo a la fila sin recorrer la tabla.

    public async Task<bool> ExisteIdentificacionAsync(string identificacion) =>
        await _contexto.Clientes.AnyAsync(c => c.Identificacion == identificacion);

    public async Task<bool> TieneContratoActivoAsync(int clienteId) =>
        await _contexto.Contratos.AnyAsync(c =>
            c.ClienteId == clienteId && (c.Estado == "VIG" || c.Estado == "REN"));

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

    public async Task AgregarVariosAsync(List<Cliente> clientes)
    {
        _contexto.Clientes.AddRange(clientes);
        await _contexto.SaveChangesAsync();
    }

    public async Task GuardarAsync() => await _contexto.SaveChangesAsync();
}