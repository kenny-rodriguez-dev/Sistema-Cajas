namespace Domain.Entities;

// Es "device" del diagrama. El enunciado pide que los servicios
// tengan "Equipos": el router, el decodificador, etc.
public class Equipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public int ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;

    public bool Activo { get; set; } = true;
}