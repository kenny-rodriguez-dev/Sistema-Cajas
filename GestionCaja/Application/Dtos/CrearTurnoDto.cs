using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

public class CrearTurnoDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Debes indicar la caja")]
    public int CajaId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Debes indicar el tipo de atención")]
    public int TipoAtencionId { get; set; }
}
