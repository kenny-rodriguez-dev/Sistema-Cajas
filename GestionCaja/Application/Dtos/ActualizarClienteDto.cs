using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

// Las mismas reglas que CrearClienteDto, con UNA diferencia importante.
public class ActualizarClienteDto
{
    [Required(ErrorMessage = "Los nombres son obligatorios")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Los nombres deben tener entre 2 y 50 caracteres")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 50 caracteres")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
    [StringLength(120)]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [StringLength(13, MinimumLength = 10, ErrorMessage = "El teléfono debe tener entre 10 y 13 dígitos")]
    [RegularExpression(@"^09[0-9]+$", ErrorMessage = "El teléfono debe empezar con 09 y solo contener números")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es obligatoria")]
    [StringLength(100, MinimumLength = 20, ErrorMessage = "La dirección debe tener entre 20 y 100 caracteres")]
    public string Direccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La referencia es obligatoria")]
    [StringLength(100, MinimumLength = 20, ErrorMessage = "La referencia debe tener entre 20 y 100 caracteres")]
    public string ReferenciaDireccion { get; set; } = string.Empty;

    // ═══ LA DIFERENCIA: NO HAY Identificacion ═══
    //
    // La cédula no se edita. Es la identidad del cliente para el
    // negocio: el enunciado dice que se valida y se busca por ella.
    // Si se pudiera cambiar, un contrato firmado con una cédula
    // quedaría apuntando a un cliente que ahora tiene otra.
    //
    // Es la misma razón por la que en usuarios no se edita el username.
    // Y como el campo no existe en el DTO, es IMPOSIBLE mandarlo:
    // aunque alguien lo agregue al JSON, .NET lo ignora.
}