using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

public class CrearUsuarioDto
{
    // Las reglas son literales del enunciado:
    //   8 a 20 caracteres · letras Y al menos un numero · sin especiales
    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    [StringLength(20, MinimumLength = 8,
        ErrorMessage = "Debe tener entre 8 y 20 caracteres")]
    [RegularExpression(@"^(?=.*[0-9])(?=.*[a-zA-Z])[a-zA-Z0-9]+$",
        ErrorMessage = "Solo letras y números, y debe tener al menos uno de cada uno")]
    public string Username { get; set; } = string.Empty;
    // Dos lookaheads encadenados:
    //   (?=.*[0-9])     -> en algun punto hay un digito
    //   (?=.*[a-zA-Z])  -> en algun punto hay una letra
    //   [a-zA-Z0-9]+    -> y SOLO se permiten esos caracteres
    // Los lookaheads verifican sin consumir; el ultimo grupo define
    // el alfabeto completo. Por eso "12345678" falla: no tiene letra.

    // Al menos un numero, al menos una mayuscula, 8 a 30 caracteres
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StringLength(30, MinimumLength = 8,
        ErrorMessage = "Debe tener entre 8 y 30 caracteres")]
    [RegularExpression(@"^(?=.*[0-9])(?=.*[A-Z]).+$",
        ErrorMessage = "Debe tener al menos un número y una mayúscula")]
    public string Password { get; set; } = string.Empty;
    // Aqui el final es .+ y no [a-zA-Z0-9]+ a proposito: en una
    // contraseña SI queremos permitir simbolos. Restringir el alfabeto
    // de una contraseña la debilita.

    [Required]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
    [StringLength(120)]
    public string Correo { get; set; } = string.Empty;

    [Required]
    [Range(2, 3, ErrorMessage = "Solo se pueden crear usuarios Gestor o Cajero")]
    public int RolId { get; set; }
    // 1 = Administrador, 2 = Gestor, 3 = Cajero.
    // El rango 2..3 impide crear administradores desde la API.
    // Nadie deberia poder darse a si mismo el rol maximo.
}

public class ActualizarUsuarioDto
{
    [Required]
    [EmailAddress]
    [StringLength(120)]
    public string Correo { get; set; } = string.Empty;

    [Required]
    [Range(2, 3)]
    public int RolId { get; set; }

    public bool Activo { get; set; }
    // Fijate en lo que NO se puede cambiar aqui: ni el username
    // ni la contraseña. El username es la identidad del usuario en
    // los registros historicos; cambiarlo rompe la trazabilidad.
    // La contraseña solo la cambia su dueño (parte 2B).
}

public class UsuarioListaDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int RolId { get; set; }
    public bool Activo { get; set; }
    public bool Aprobado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? CreadoPor { get; set; }
    // Ni PasswordHash ni nada parecido. El DTO decide que sale,
    // y un hash no tiene por que viajar al navegador jamas.
}