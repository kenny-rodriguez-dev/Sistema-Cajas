namespace Backend.Models;

public class Rol
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;
    // Administrador, Gestor, Cajero

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    // PROPIEDAD DE NAVEGACIÓN.
    //
    // Esta línea NO crea una columna en la tabla Roles. Es un atajo de C#
    // que te permite escribir rol.Usuarios y que EF salga a buscarlos.
    //
    // Representa el lado "muchos": un rol tiene muchos usuarios.

    public ICollection<OpcionMenuRol> OpcionesMenu { get; set; } = new List<OpcionMenuRol>();
    // NUEVA. El otro extremo de la relación muchos a muchos.
}