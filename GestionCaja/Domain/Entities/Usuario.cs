namespace Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;

    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;

    public bool Activo { get; set; } = true;
    public bool Aprobado { get; set; } = false;

    public int? CreadoPorUsuarioId { get; set; }
    public int? AprobadoPorUsuarioId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public ICollection<AsignacionCaja> Asignaciones { get; set; } = new List<AsignacionCaja>();
    public ICollection<SesionCaja> Sesiones { get; set; } = new List<SesionCaja>();

    // ─── BLOQUEO (NUEVO) ───────────────────────────────────────
    public int IntentosFallidos { get; set; } = 0;

    public DateTime? BloqueadoHasta { get; set; }
    // null = no está bloqueado.
    // Con fecha = bloqueado hasta ese momento, en UTC.
    // Mismo patrón de siempre: la ausencia de fecha ES el estado.

    // ═══════════════════════════════════════════════════════════
    //   LA PRIMERA ENTIDAD CON COMPORTAMIENTO
    // ═══════════════════════════════════════════════════════════
    //
    // Hasta ahora todas tus entidades eran bolsas de datos: propiedades
    // y nada más. Toda la lógica vivía en los controladores.
    //
    // Estos métodos son distintos: la regla de "cuántos intentos y
    // cuánto dura el bloqueo" vive DENTRO de la entidad. Esto es lo que
    // DDD llama un modelo de dominio rico, y es la diferencia práctica
    // entre "usé la estructura de carpetas de DDD" y "apliqué DDD".
    //
    // ¿Qué se gana? Que la regla está en UN lugar. El login la usa,
    // el desbloqueo del administrador la usa, la recuperación de
    // contraseña la usa. Si mañana cambia a 3 intentos, se cambia aquí
    // y el resto ni se entera.

    public const int MaximoIntentos = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);
    // "const" y "static readonly" no son propiedades: EF no las
    // convierte en columnas. Son constantes del negocio.

    public bool EstaBloqueado(DateTime ahora) =>
        BloqueadoHasta.HasValue && BloqueadoHasta.Value > ahora;
    // ═══ ¿POR QUÉ RECIBE "ahora" EN VEZ DE USAR DateTime.UtcNow? ═══
    // Porque así se puede PROBAR. Una prueba automática puede pasarle
    // cualquier momento y verificar la regla sin esperar 15 minutos
    // reales. Si la hora estuviera escrita adentro, la entidad sería
    // imposible de probar sin trucos.

    public void RegistrarIntentoFallido(DateTime ahora)
    {
        IntentosFallidos++;

        if (IntentosFallidos >= MaximoIntentos)
        {
            BloqueadoHasta = ahora.Add(DuracionBloqueo);
            IntentosFallidos = 0;
            // Reinicio el contador al bloquear: cuando el bloqueo
            // termine, el usuario tiene otros cinco intentos, no uno.
        }
    }

    public void RegistrarIngresoExitoso()
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
        // Una contraseña correcta borra el historial de fallos.
        // Los intentos tienen que ser SEGUIDOS para bloquear.
    }

    public void Desbloquear()
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
    }
}