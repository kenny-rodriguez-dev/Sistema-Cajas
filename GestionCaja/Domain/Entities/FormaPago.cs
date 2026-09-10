namespace Domain.Entities;

// Es "methodpayment". Catálogo simple.
public class FormaPago
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}