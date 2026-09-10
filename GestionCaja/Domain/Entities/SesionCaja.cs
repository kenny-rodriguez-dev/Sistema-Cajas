namespace Domain.Entities;

// LA TABLA NUEVA, la que no está en el diagrama de Viamatica.
// Cada fila es "el cajero X estuvo operando la caja Y desde tal hora
// hasta tal hora".
public class SesionCaja
{
    public int Id { get; set; }

    public int CajaId { get; set; }
    public Caja Caja { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;

    public DateTime? FechaCierre { get; set; }
    // ═══ ESTE SIGNO DE INTERROGACIÓN ES EL CORAZÓN DE TODO ═══
    //
    // Permite NULL, y ese NULL tiene un significado de negocio preciso:
    //
    //     FechaCierre == null   →   la sesión SIGUE ABIERTA
    //     FechaCierre != null   →   la sesión ya se cerró
    //
    // No necesito un campo "EstaAbierta" aparte: la ausencia de fecha
    // ya lo dice todo. Y en el próximo paso vas a ver que ese null
    // es justo lo que me permite resolver la condición de carrera.
}