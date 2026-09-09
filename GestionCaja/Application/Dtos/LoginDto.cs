using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

// Lo que ENTRA al login. Fíjate en lo que NO tiene:
// ni longitud mínima, ni regex, ni reglas de formato.
//
// ¿Por qué? Porque validar el formato aquí sería un regalo para un atacante:
// le confirmaría las reglas de las contraseñas sin siquiera tener una cuenta.
// En el login solo pregunto que no venga vacío. El resto lo dice la
// verificación misma.
public class LoginDto
{
    [Required(ErrorMessage = "El usuario es obligatorio")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; } = string.Empty;
    // Aquí SÍ se llama "Password" a secas, porque es la contraseña real
    // que el usuario acaba de escribir. Todavía no es un hash.
    // Vive en memoria unos milisegundos y nunca se guarda.
}