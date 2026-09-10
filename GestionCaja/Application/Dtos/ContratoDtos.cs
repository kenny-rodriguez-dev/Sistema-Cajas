using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

// Varios DTOs en un archivo porque son cortos y del mismo tema.
// Que cada clase tenga su archivo es lo normal, pero agrupar por
// tema cuando son pequeños también es una práctica aceptada.

public class CrearContratoDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Debes indicar el cliente")]
    public int ClienteId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Debes indicar el servicio")]
    public int ServicioId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Debes indicar la forma de pago")]
    public int FormaPagoId { get; set; }

    [Range(1, 36, ErrorMessage = "La duración debe estar entre 1 y 36 meses")]
    public int MesesDuracion { get; set; } = 12;
}

public class CambiarServicioDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Debes indicar el nuevo servicio")]
    public int NuevoServicioId { get; set; }
}

public class CambiarFormaPagoDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Debes indicar la nueva forma de pago")]
    public int NuevaFormaPagoId { get; set; }
}

public class ContratoDto
{
    public int Id { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
    public string Servicio { get; set; } = string.Empty;
    public int VelocidadMbps { get; set; }
    public decimal Precio { get; set; }
    public string FormaPago { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int? ContratoAnteriorId { get; set; }
}