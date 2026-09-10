using Domain.Entities;

namespace Application.Interfaces;

public interface IRepositorioContratos
{
    Task<Contrato?> ObtenerVigenteDeClienteAsync(int clienteId);
    Task<Contrato?> ObtenerConDetalleAsync(int contratoId);
    Task<List<Contrato>> ListarDeClienteAsync(int clienteId);
    Task<Servicio?> ObtenerServicioAsync(int servicioId);
    Task<bool> ExisteFormaPagoAsync(int formaPagoId);
    Task<bool> ExisteClienteActivoAsync(int clienteId);
    Task<List<Servicio>> ListarServiciosAsync();
    Task<List<FormaPago>> ListarFormasPagoAsync();

    Task<bool> CrearAsync(Contrato contrato);
    Task<bool> SustituirAsync(Contrato anterior, Contrato nuevo);
    Task GuardarAsync();
}