namespace Application.Dtos;

public class TurnoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string TipoAtencion { get; set; } = string.Empty;
    public string Caja { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaHoraEmision { get; set; }
    public string? AtendidoPor { get; set; }
}