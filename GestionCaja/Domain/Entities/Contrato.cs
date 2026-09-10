namespace Domain.Entities;

public class Contrato
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public int ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;

    public int FormaPagoId { get; set; }
    public FormaPago FormaPago { get; set; } = null!;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }
    // La que se HEREDA cuando hay sustitución.

    public string Estado { get; set; } = "VIG";
    // VIG = vigente        (el activo)
    // SUS = sustituido     (cambió de servicio)
    // REN = renovación     (nació de una sustitución)
    // CAN = cancelado      (el cliente lo dio de baja)
    //
    // Los dos primeros son literales del enunciado.

    public DateTime? FechaCancelacion { get; set; }
    // "Si el cliente pide cancelación... se actualiza el estado y la fecha fin."

    // ═══════════════════════════════════════════════════════════
    //   LA PIEZA QUE FALTABA EN EL DIAGRAMA ORIGINAL
    // ═══════════════════════════════════════════════════════════
    public int? ContratoAnteriorId { get; set; }
    public Contrato? ContratoAnterior { get; set; }
    //
    // Una relación de la tabla CONSIGO MISMA.
    //
    // Cuando el contrato 5 sustituye al 3, el contrato 5 guarda un 3 aquí.
    // Eso encadena la historia completa:
    //
    //     Contrato 1 (SUS)  ←  Contrato 3 (SUS)  ←  Contrato 5 (VIG)
    //       50 Mbps            100 Mbps             200 Mbps
    //
    // Sin este campo tendrías los tres contratos sueltos, sin forma de
    // saber que son el mismo cliente evolucionando. Recuerda que cuando
    // leímos el esquema en la primera sesión, esto fue una de las cosas
    // que anotamos como faltante. Aquí está el porqué.

    public int CreadoPorUsuarioId { get; set; }
    public Usuario CreadoPorUsuario { get; set; } = null!;

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}