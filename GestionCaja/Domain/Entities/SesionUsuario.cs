namespace Domain.Entities;

// Cada fila es un login. Es el "libro del hotel" que JWT no tenía:
// ahora el servidor sabe qué pulseras entregó y cuáles siguen valiendo.
public class SesionUsuario
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string Jti { get; set; } = string.Empty;
    // ═══ EL JTI QUE PUSISTE EN LA REBANADA 2B ═══
    // Es el identificador único de cada token. Te dije entonces que
    // servía "para poder revocarlo más adelante". Este es el momento.
    // Cada token lleva su Jti adentro, y aquí se guarda el mismo:
    // así se sabe a qué sesión pertenece cada petición.

    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracion { get; set; }
    // La misma fecha de vencimiento del token.

    public DateTime? FechaCierre { get; set; }
    public string? MotivoCierre { get; set; }
    // "logout", "desactivado", "cambio de rol", "cambio de contraseña"...
    // Guardar POR QUÉ se cerró sirve para auditar: si alguien pregunta
    // "¿por qué me sacó el sistema?", la respuesta está aquí.

    // ─── Comportamiento: modelo de dominio rico, otra vez ───

    public bool EstaActiva(DateTime ahora) =>
        FechaCierre is null && FechaExpiracion > ahora;

    public void Cerrar(DateTime ahora, string motivo)
    {
        if (FechaCierre is not null) return;
        // Si ya estaba cerrada, no la toco: conservo el motivo original.
        // Una sesión cerrada por "desactivado" no debe cambiar a "logout"
        // si después alguien intenta cerrarla otra vez.
        FechaCierre = ahora;
        MotivoCierre = motivo;
    }
}