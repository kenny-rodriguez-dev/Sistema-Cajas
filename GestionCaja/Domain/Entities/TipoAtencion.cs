namespace Domain.Entities;

// Es la tabla "attentiontype" del diagrama.
// De aquí salen las 2 letras del código: AC, PS.
public class TipoAtencion
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;
    // Exactamente 2 letras mayúsculas: "AC", "PS"

    public string Descripcion { get; set; } = string.Empty;
    // "Atención al cliente", "Pago de servicio"

    public bool Activo { get; set; } = true;

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}