using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

public class RegistrarPagoDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Debes indicar el contrato")]
    public int ContratoId { get; set; }

    [Required(ErrorMessage = "El periodo es obligatorio")]
    [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$",
        ErrorMessage = "El periodo debe tener el formato AAAA-MM")]
    public string Periodo { get; set; } = string.Empty;
    // Cómo se lee esa expresión:
    //   \d{4}          cuatro dígitos: el año
    //   -              el guion
    //   (0[1-9]|1[0-2]) el mes: de 01 a 09, o de 10 a 12
    // Así "2026-13" o "2026-00" quedan rechazados.

    // ═══ LO QUE NO ESTÁ AQUÍ: EL MONTO ═══
    // No existe el campo, así que es IMPOSIBLE que el navegador lo
    // mande. El precio lo decide el servidor, no quien paga.
}

public class PagoDto
{
    public int Id { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string FormaPago { get; set; } = string.Empty;
    public string Servicio { get; set; } = string.Empty;
    public DateTime FechaPago { get; set; }
    public string RegistradoPor { get; set; } = string.Empty;
}