using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

public class SolicitarRecuperacionDto
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
    public string Correo { get; set; } = string.Empty;
}

public class RestablecerPasswordDto
{
    [Required(ErrorMessage = "El token es obligatorio")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StringLength(30, MinimumLength = 8,
        ErrorMessage = "Debe tener entre 8 y 30 caracteres")]
    [RegularExpression(@"^(?=.*[0-9])(?=.*[A-Z]).+$",
        ErrorMessage = "Debe tener al menos un número y una mayúscula")]
    public string NuevaPassword { get; set; } = string.Empty;
    // Las MISMAS reglas que al crear el usuario. Si aquí fueran más
    // flojas, la recuperación se convertiría en la puerta trasera
    // para poner contraseñas débiles.
}