namespace Application.Dtos;

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

    public bool Activo { get; set; }
    // NUEVO. Antes no hacía falta porque solo se listaban los activos.
    // Ahora la pantalla muestra también los inactivos para poder
    // reactivarlos, así que necesita saber cuál es cuál.
}