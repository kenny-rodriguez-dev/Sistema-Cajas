namespace Backend.Models;

public class Usuario
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    // MIRA BIEN EL NOMBRE: PasswordHash, no Password.
    //
    // No es cosmético. El nombre de la propiedad es un contrato con el
    // programador que lea esto en seis meses: aquí NUNCA hay una contraseña
    // real. Si algún día alguien mete texto plano en este campo, el nombre
    // mismo lo delata.

    public string Correo { get; set; } = string.Empty;

    // ─── LA RELACIÓN CON ROL: DOS PROPIEDADES, DOS TRABAJOS ───

    public int RolId { get; set; }
    // Esta SÍ es una columna real en la tabla. Es la LLAVE FORÁNEA:
    // guarda el número del rol al que pertenece este usuario.
    // Es la "F" que aprendiste a buscar en el diagrama de Viamatica.

    public Rol Rol { get; set; } = null!;
    // Esta NO es una columna. Es la NAVEGACIÓN: el atajo para escribir
    // usuario.Rol.Nombre en vez de hacer la consulta a mano.
    //
    // El "null!" se lee: "compilador, ya sé que arranca en null, pero
    // confía en mí, EF lo va a rellenar". Sin él te llena de advertencias.
    //
    // Regla de EF: la llave foránea se llama <Navegación> + "Id".
    // Como la navegación se llama Rol, la llave debe llamarse RolId.
    // Con ese nombre, EF conecta las dos solo, sin configurar nada.

    public bool Activo { get; set; } = true;
    // Eliminación lógica, igual que en Cliente.

    public bool Aprobado { get; set; } = false;
    // "El usuario gestor puede agregar usuarios pero un administrador
    //  debe aprobar el usuario creado."
    //
    // Arranca en false a propósito: nace pendiente. Y esto va a ser
    // parte del login: un usuario no aprobado no debe poder entrar.

    public int? CreadoPorUsuarioId { get; set; }
    public int? AprobadoPorUsuarioId { get; set; }
    // Los "usercreate" y "userapproval" del diagrama original.
    //
    // El signo "?" significa que pueden ser NULL. Y tienen que poder:
    // el primer administrador del sistema no fue creado por nadie.
    //
    // Son referencias a esta misma tabla. Por ahora las dejo como números
    // sueltos; les pongo la navegación en la rebanada 3, cuando exista
    // el flujo de aprobación de verdad.

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
