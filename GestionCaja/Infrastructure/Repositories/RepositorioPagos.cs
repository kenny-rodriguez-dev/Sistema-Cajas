using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositorioPagos : IRepositorioPagos
{
    private readonly AppDbContext _contexto;

    public RepositorioPagos(AppDbContext contexto) => _contexto = contexto;

    public async Task<Contrato?> ObtenerContratoConServicioAsync(int contratoId) =>
        await _contexto.Contratos
            .Include(c => c.Servicio)
            // El Include es obligatorio: de ahí sale el PRECIO.
            .FirstOrDefaultAsync(c => c.Id == contratoId);

    public async Task<bool> ExistePagoDelPeriodoAsync(int clienteId, string periodo) =>
        await _contexto.Pagos.AnyAsync(p => p.ClienteId == clienteId && p.Periodo == periodo);

    public async Task<bool> RegistrarAsync(Pago pago)
    {
        try
        {
            _contexto.Pagos.Add(pago);
            await _contexto.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            _contexto.ChangeTracker.Clear();
            return false;
            // El índice único rechazó el pago: otro cajero cobró ese
            // mes a ese cliente en el mismo instante. Carrera perdida.
        }
    }

    public async Task<List<Pago>> ListarDeClienteAsync(int clienteId) =>
        await _contexto.Pagos
            .Include(p => p.Contrato).ThenInclude(c => c.Servicio)
            .Include(p => p.FormaPago)
            .Include(p => p.RegistradoPorUsuario)
            .Where(p => p.ClienteId == clienteId)
            .OrderByDescending(p => p.Periodo)
            .ToListAsync();
    // OrderByDescending sobre el texto del periodo funciona porque
    // "AAAA-MM" ordena igual alfabética que cronológicamente.
    // El mismo truco que usaste con las fechas del dashboard.
}