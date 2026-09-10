namespace Domain.Entities;

public class Caja
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<AsignacionCaja> Asignaciones { get; set; } = new List<AsignacionCaja>();
    public ICollection<SesionCaja> Sesiones { get; set; } = new List<SesionCaja>();
    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}