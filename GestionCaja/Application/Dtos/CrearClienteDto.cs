using System.ComponentModel.DataAnnotations;
// Aquí viven [Required], [StringLength], [RegularExpression]...
// Son los "DataAnnotations" que menciona el enunciado.

namespace Application.Dtos;

// Esta clase es la CAPA 2 de validación: la que de verdad protege el sistema.
// Cada atributo entre corchetes es una regla sacada del enunciado de Viamatica.
public class CrearClienteDto
{
    [Required(ErrorMessage = "Los nombres son obligatorios")]
    [StringLength(50, MinimumLength = 2,
        ErrorMessage = "Los nombres deben tener entre 2 y 50 caracteres")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios")]
    [StringLength(50, MinimumLength = 2,
        ErrorMessage = "Los apellidos deben tener entre 2 y 50 caracteres")]
    public string Apellidos { get; set; } = string.Empty;

    // "La identificación debe tener mínimo 10 dígitos y máximo 13."
    // "La identificación solo debe estar conformada por números."
    [Required(ErrorMessage = "La identificación es obligatoria")]
    [StringLength(13, MinimumLength = 10,
        ErrorMessage = "La identificación debe tener entre 10 y 13 dígitos")]
    [RegularExpression(@"^[0-9]+$",
        ErrorMessage = "La identificación solo puede contener números")]
    public string Identificacion { get; set; } = string.Empty;
    //
    // Fíjate que uso DOS atributos en vez de una regex gigante que haga todo.
    // Cada uno hace UNA cosa. Ventaja: el mensaje de error dice exactamente
    // qué falló. Si metes todo en una regex, solo puedes dar un mensaje genérico.
    //
    // Ese @ antes de las comillas: le dice a C# "no interpretes las barras
    // invertidas". Sin él tendrías que escribir \\ en vez de \. Siempre úsalo
    // en expresiones regulares.

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
    [StringLength(120)]
    public string Correo { get; set; } = string.Empty;

    // "El número de teléfono debe tener al menos 10 dígitos y solo números
    //  y solo empezar con 09."
    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [StringLength(13, MinimumLength = 10,
        ErrorMessage = "El teléfono debe tener entre 10 y 13 dígitos")]
    [RegularExpression(@"^09[0-9]+$",
        ErrorMessage = "El teléfono debe empezar con 09 y solo contener números")]
    public string Telefono { get; set; } = string.Empty;
    //
    // Cómo se lee ^09[0-9]+$ :
    //   ^      empieza aquí (sin esto, "abc0912345678" pasaría)
    //   09     literalmente los caracteres 0 y 9
    //   [0-9]  cualquier dígito
    //   +      uno o más de lo anterior
    //   $      termina aquí (sin esto, "0912345678xyz" pasaría)
    //
    // El ^ y el $ NO son opcionales. Sin ellos la regex busca la
    // coincidencia en cualquier parte del texto, no en todo el texto.

    // "La dirección debe tener al menos 20 caracteres y máximo 100"
    [Required(ErrorMessage = "La dirección es obligatoria")]
    [StringLength(100, MinimumLength = 20,
        ErrorMessage = "La dirección debe tener entre 20 y 100 caracteres")]
    public string Direccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La referencia de dirección es obligatoria")]
    [StringLength(100, MinimumLength = 20,
        ErrorMessage = "La referencia debe tener entre 20 y 100 caracteres")]
    public string ReferenciaDireccion { get; set; } = string.Empty;
}