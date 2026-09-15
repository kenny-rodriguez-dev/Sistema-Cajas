using Domain.Entities;

namespace Application.Interfaces;

public interface IRepositorioTurnos
{
    Task<int> SiguienteNumeroAsync(int tipoAtencionId, DateOnly fecha);
    Task<TipoAtencion?> ObtenerTipoAsync(int tipoAtencionId);
    Task CrearAsync(Turno turno);
    Task<List<Turno>> ListarDelDiaAsync(DateOnly fecha, int? cajaId);
    Task<Turno?> ObtenerPendienteAsync(int turnoId);
    Task GuardarAsync();
    Task<int> ContarAtendidosDelDiaAsync(DateOnly fecha, int? usuarioId);
    Task<List<TipoAtencion>> ListarTiposAsync();
}