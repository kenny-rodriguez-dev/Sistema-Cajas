namespace Domain.Entities;

// Esta tabla NO está en el diagrama y no representa nada del negocio.
// Es puramente técnica: lleva la cuenta de "por dónde va la numeración".
//
// ¿Por qué una tabla y no contar los turnos existentes con un COUNT?
// Porque si mañana se borra o cancela un turno, el COUNT bajaría y
// se repetiría un número que ya se entregó. Un contador solo sube.
public class ContadorTurno
{
    public int TipoAtencionId { get; set; }
    public TipoAtencion TipoAtencion { get; set; } = null!;

    public DateOnly Fecha { get; set; }
    // DateOnly = solo fecha, sin hora. Es un tipo de C# moderno que
    // se guarda como DATE en SQL Server.
    //
    // La numeración se reinicia cada día: mañana vuelve a AC0001.
    // Por eso la fecha es parte de la llave.

    public int UltimoNumero { get; set; }
}