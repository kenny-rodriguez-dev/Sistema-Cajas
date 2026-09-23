using Domain.Entities;

namespace Application.Interfaces;

public interface IRepositorioPagos
{
    Task<Contrato?> ObtenerContratoConServicioAsync(int contratoId);
    Task<bool> ExistePagoDelPeriodoAsync(int clienteId, string periodo);
    Task<bool> RegistrarAsync(Pago pago);
    Task<List<Pago>> ListarDeClienteAsync(int clienteId);
}