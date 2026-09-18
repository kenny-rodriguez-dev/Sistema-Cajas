namespace Domain.Entities;

public class TokenRecuperacion
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    // ═══ FÍJATE EN EL NOMBRE: TokenHash, NO Token ═══
    //
    // El token que viaja al correo del usuario NUNCA se guarda tal cual.
    // Se guarda su hash, exactamente igual que con las contraseñas.
    //
    // ¿Por qué tanto cuidado con un token que dura 15 minutos?
    // Porque un token válido ES una contraseña temporal: quien lo tenga
    // puede cambiar la clave de esa cuenta. Si alguien lee tu base de
    // datos y encuentra tokens en claro, puede secuestrar cualquier
    // cuenta que tenga uno pendiente.
    //
    // Esta es la clase de decisión que separa una implementación
    // correcta de una que "funciona".

    public DateTime FechaExpiracion { get; set; }
    // Vida corta a propósito. Mientras más tiempo vive un token,
    // más ventana hay para que alguien lo intercepte o lo encuentre.

    public DateTime? FechaUso { get; set; }
    // null = todavía no se usó.
    //
    // Mismo patrón que la FechaCierre de SesionCaja: la ausencia de
    // fecha ES el estado. Y sirve para lo mismo: que el token sea
    // de UN SOLO USO. Usado una vez, queda quemado para siempre.

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}