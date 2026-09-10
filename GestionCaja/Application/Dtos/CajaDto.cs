namespace Application.Dtos;

public class CajaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<string> CajerosAsignados { get; set; } = new();
    public string? OcupadaPor { get; set; }
    // Null = la caja está libre. Con nombre = alguien la está operando.
}