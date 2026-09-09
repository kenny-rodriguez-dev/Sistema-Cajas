namespace Domain.Entities;

// Cada fila es una opción del menú lateral.
// Que esto viva en la base y no en el código del frontend es la clave:
// agregar una opción o cambiar quién la ve NO requiere volver a publicar
// el frontend. Solo se toca la base.
public class OpcionMenu
{
    public int Id { get; set; }

    public string Etiqueta { get; set; } = string.Empty;
    // El texto que ve el usuario: "Clientes", "Dashboard"...

    public string Ruta { get; set; } = string.Empty;
    // La URL del frontend a la que lleva: "/clientes/nuevo"

    public int Orden { get; set; }
    // Para decidir el orden en pantalla sin depender del Id.
    // Así puedo insertar algo en medio sin renumerar todo.

    public bool Activo { get; set; } = true;

    public ICollection<OpcionMenuRol> Roles { get; set; } = new List<OpcionMenuRol>();
    // Navegación hacia la tabla intermedia.
}