namespace Domain.Entities;

// Es la tabla "usercash" del diagrama original.
// Mismo patrón que OpcionMenuRol: llave primaria compuesta,
// dos llaves foráneas, y nada más. Ya conoces el molde.
public class AsignacionCaja
{
    public int CajaId { get; set; }
    public Caja Caja { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
}