namespace Backend.Dtos;

// Lo que SALE de la API. Sin atributos de validación: aquí no se valida nada,
// solo se decide QUÉ campos ve el mundo exterior.
public class ClienteDto
{
    public int Id { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string ReferenciaDireccion { get; set; } = string.Empty;
    // Aquí SÍ va el Id (el frontend lo necesita para editar o borrar),
    // pero NO va "Activo": es un detalle interno del sistema.
}
