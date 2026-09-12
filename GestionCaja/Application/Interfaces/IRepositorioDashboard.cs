using Application.Dtos;

namespace Application.Interfaces;

public interface IRepositorioDashboard
{
    Task<DashboardDto> ObtenerIndicadoresAsync(DateTime desde, DateTime hasta);
    Task<int> ContarPendientesAprobacionAsync();
    Task<int> ContarTurnosAtendidosHoyAsync(DateOnly fecha, string rol, int usuarioId);
}