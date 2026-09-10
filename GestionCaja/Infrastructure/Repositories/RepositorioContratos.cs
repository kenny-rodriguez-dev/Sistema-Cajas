using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositorioContratos : IRepositorioContratos
{
    private readonly AppDbContext _contexto;

    public RepositorioContratos(AppDbContext contexto) => _contexto = contexto;

    public async Task<Contrato?> ObtenerVigenteDeClienteAsync(int clienteId) =>
        await _contexto.Contratos
            .Include(c => c.Servicio)
            .Include(c => c.FormaPago)
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId
                                   && (c.Estado == "VIG" || c.Estado == "REN"));

    public async Task<Contrato?> ObtenerConDetalleAsync(int contratoId) =>
        await _contexto.Contratos
            .Include(c => c.Servicio)
            .Include(c => c.FormaPago)
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == contratoId);

    public async Task<List<Contrato>> ListarDeClienteAsync(int clienteId) =>
        await _contexto.Contratos
            .Include(c => c.Servicio)
            .Include(c => c.FormaPago)
            .Include(c => c.Cliente)
            .Where(c => c.ClienteId == clienteId)
            .OrderByDescending(c => c.FechaRegistro)
            .ToListAsync();

    public async Task<Servicio?> ObtenerServicioAsync(int servicioId) =>
        await _contexto.Servicios.FirstOrDefaultAsync(s => s.Id == servicioId && s.Activo);

    public async Task<bool> ExisteFormaPagoAsync(int formaPagoId) =>
        await _contexto.FormasPago.AnyAsync(f => f.Id == formaPagoId && f.Activo);

    public async Task<bool> ExisteClienteActivoAsync(int clienteId) =>
        await _contexto.Clientes.AnyAsync(c => c.Id == clienteId && c.Activo);

    public async Task<List<Servicio>> ListarServiciosAsync() =>
        await _contexto.Servicios.Include(s => s.Equipos)
            .Where(s => s.Activo).OrderBy(s => s.VelocidadMbps).ToListAsync();

    public async Task<List<FormaPago>> ListarFormasPagoAsync() =>
        await _contexto.FormasPago.Where(f => f.Activo).ToListAsync();

    public async Task<bool> CrearAsync(Contrato contrato)
    {
        try
        {
            _contexto.Contratos.Add(contrato);
            await _contexto.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            _contexto.ChangeTracker.Clear();
            return false;
            // El índice único filtrado rechazó el contrato:
            // ese cliente ya tenía uno activo.
        }
    }

    public async Task<bool> SustituirAsync(Contrato anterior, Contrato nuevo)
    {
        // ═══════════════════════════════════════════════════════════
        //   TRANSACCIÓN: dos escrituras que deben pasar JUNTAS
        // ═══════════════════════════════════════════════════════════
        using var transaccion = await _contexto.Database.BeginTransactionAsync();

        try
        {
            anterior.Estado = "SUS";
            await _contexto.SaveChangesAsync();
            // Primero marco el viejo como sustituido. Esto es OBLIGATORIO
            // que vaya antes: mientras siga en "VIG", el índice único
            // filtrado no dejaría insertar el nuevo. Solo puede haber
            // un contrato activo por cliente.

            _contexto.Contratos.Add(nuevo);
            await _contexto.SaveChangesAsync();

            await transaccion.CommitAsync();
            return true;
            // COMMIT: recién aquí los cambios quedan grabados de verdad.
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            _contexto.ChangeTracker.Clear();
            return false;
            //
            // ═══ POR QUÉ ESTO ES CRÍTICO ═══
            //
            // Sin transacción, si el primer SaveChanges funciona y el
            // segundo falla, el cliente se queda con su contrato marcado
            // como SUSTITUIDO y ningún contrato nuevo.
            //
            // Resultado: un cliente PAGANDO y SIN SERVICIO, y sin forma
            // de saber qué pasó. Eso es corrupción de datos.
            //
            // El rollback deshace todo. O pasan las dos escrituras, o
            // ninguna. Eso se llama ATOMICIDAD, la A de ACID, y es
            // pregunta de entrevista garantizada.
        }
    }

    public async Task GuardarAsync() => await _contexto.SaveChangesAsync();
}