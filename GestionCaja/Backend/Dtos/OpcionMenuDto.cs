namespace Backend.Dtos;

public class OpcionMenuDto
{
    public string Etiqueta { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    // Ni Id, ni Orden, ni Activo: el frontend solo necesita
    // qué texto pintar y a dónde lleva.
}