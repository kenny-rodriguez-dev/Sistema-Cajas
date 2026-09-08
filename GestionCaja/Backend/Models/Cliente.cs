namespace Backend.Models;
// El "apellido" de la clase. Como está en la carpeta Models,
// por convención el namespace lleva .Models al final.

// Esta clase es una TABLA. Cada propiedad de aquí será una COLUMNA,
// y cada cliente guardado será una FILA.
public class Cliente
{
    public int Id { get; set; }
    // Entity Framework tiene convenciones: si ve una propiedad llamada "Id"
    // (o "ClienteId"), la convierte en llave primaria automáticamente.
    // Además, al ser int, SQL Server la hace autoincremental: 1, 2, 3...
    // Yo nunca le asigno valor; la base lo hace sola.

    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    // "{ get; set; }" = propiedad automática: se puede leer y escribir.
    //
    // ¿Y ese "= string.Empty"? C# moderno asume que un string NO puede ser nulo.
    // Si no le doy un valor inicial, el compilador me advierte que podría
    // quedar en null. Le doy texto vacío y se calla. No es adorno.

    public string Identificacion { get; set; } = string.Empty;
    // OJO CON ESTA. Es string, NO int, aunque solo tenga números.
    //
    // Dos razones, y las dos son pregunta de entrevista:
    //   1) Si la cédula empieza en 0, un int se lo come: 0912345678 -> 912345678
    //   2) Nunca vas a sumar ni multiplicar cédulas. Si no haces
    //      matemática con algo, no es un número: es un texto.
    //
    // Misma lógica para teléfonos y códigos postales.

    public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string ReferenciaDireccion { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
    // ELIMINACIÓN LÓGICA, que el enunciado pide.
    // Borrar un cliente = poner esto en false. La fila nunca se va.
    // Así los contratos y pagos que apuntan a él no quedan huérfanos.

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    // UtcNow y no Now: se guarda siempre en hora universal y se convierte
    // al mostrar. Si no, un servidor en otro país te desordena todo.
}