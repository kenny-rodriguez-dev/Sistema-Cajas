namespace Domain.Entities;

// El ticket. Es el "turn" + "attention" del diagrama, unificados:
// el diagrama los separaba, pero para este alcance una sola tabla
// dice lo mismo con menos piezas.
public class Turno
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;
    // "AC0001". Seis caracteres: 2 letras + 4 números,
    // tal como lo exige el enunciado.

    public int TipoAtencionId { get; set; }
    public TipoAtencion TipoAtencion { get; set; } = null!;

    public int CajaId { get; set; }
    public Caja Caja { get; set; } = null!;

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    // Puede ser null: un turno se emite antes de saber quién es
    // el cliente. Se llena cuando el cajero lo atiende.

    public int? AtendidoPorUsuarioId { get; set; }
    public Usuario? AtendidoPorUsuario { get; set; }

    public int AsignadoPorUsuarioId { get; set; }
    public Usuario AsignadoPorUsuario { get; set; } = null!;
    // El gestor que lo asignó. El enunciado pide mostrarle al gestor
    // "todos los turnos que fueron asignados por ese usuario",
    // así que hay que guardar quién fue.

    public DateOnly Fecha { get; set; }
    public DateTime FechaHoraEmision { get; set; } = DateTime.UtcNow;
    public DateTime? FechaHoraAtencion { get; set; }

    public string Estado { get; set; } = "PEN";
    // PEN = pendiente, ATE = atendido, CAN = cancelado
    //
    // Aquí tomé una decisión que vale explicar: el diagrama tiene una
    // tabla "attentionstatus" aparte. Yo uso un texto de 3 letras con
    // una restricción CHECK en la base.
    //
    // ¿Cuándo conviene una tabla de catálogo? Cuando los valores
    // cambian sin que un programador intervenga, o cuando llevan datos
    // extra (una descripción larga, un color, un orden). Aquí son tres
    // valores fijos que el código conoce por nombre: una tabla solo
    // agregaría un JOIN a cada consulta.
    //
    // No hay respuesta única. Lo que importa es que puedas justificarla.
}