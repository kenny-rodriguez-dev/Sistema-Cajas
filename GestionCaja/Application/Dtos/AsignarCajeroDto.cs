using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

public class AsignarCajeroDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "El id del usuario debe ser válido")]
    public int UsuarioId { get; set; }
}