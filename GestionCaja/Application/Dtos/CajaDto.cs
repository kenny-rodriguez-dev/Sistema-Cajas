namespace Application.Dtos;

public class CajeroAsignadoDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class CajaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public List<CajeroAsignadoDto> CajerosAsignados { get; set; } = new();
    // Antes era List<string>, solo con los nombres.
    // Para quitar a alguien necesito su Id, y el nombre no alcanza.
    //
    // ¿Rompe algo cambiar la forma? No: las pantallas de turnos de
    // Next.js y de Angular solo leen "id" y "nombre" de este endpoint.
    // Revisar quién consume un endpoint antes de cambiarlo es parte
    // del trabajo.

    public string? OcupadaPor { get; set; }
}