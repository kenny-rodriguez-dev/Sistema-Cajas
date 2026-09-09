using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public static class SembradorDeDatos
{
    public static async Task SembrarAsync(AppDbContext contexto)
    {
        // ═══ ¿POR QUÉ LOS USUARIOS NO VAN EN HasData COMO LOS ROLES? ═══
        //
        // Porque la contraseña hay que hashearla, y BCrypt genera un salt
        // distinto cada vez: el mismo "Admin1234" produce un hash diferente
        // en cada ejecución. Ya lo comprobaste en la rebanada 2A.
        //
        // HasData necesita valores FIJOS: compara lo que hay con lo que
        // debería haber para decidir si algo cambió. Con un hash distinto
        // cada vez, EF creería que el usuario cambió en cada migración.
        //
        // Regla: HasData para catálogos fijos; sembrador para lo que se
        // calcula al momento.

        if (await contexto.Usuarios.AnyAsync()) return;
        // Idempotente: si ya hay usuarios, no hace nada.

        var usuarios = new List<Usuario>
        {
            new Usuario
            {
                Username = "admin0001",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234"),
                Correo = "admin@gestioncaja.com",
                RolId = 1,
                Activo = true,
                Aprobado = true
            },
            new Usuario
            {
                Username = "gestor0001",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Gestor1234"),
                Correo = "gestor@gestioncaja.com",
                RolId = 2,
                Activo = true,
                Aprobado = true
            },
            new Usuario
            {
                Username = "cajero0001",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cajero1234"),
                Correo = "cajero@gestioncaja.com",
                RolId = 3,
                Activo = true,
                Aprobado = true
            },
            new Usuario
            {
                Username = "pendiente0001",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pendiente1234"),
                Correo = "pendiente@gestioncaja.com",
                RolId = 3,
                Activo = true,
                Aprobado = false
                // ESTE ES A PROPÓSITO: sirve para probar el rechazo por
                // falta de aprobación que programaste en la 2B y que
                // hasta ahora nunca se había ejecutado.
            }
        };

        contexto.Usuarios.AddRange(usuarios);
        await contexto.SaveChangesAsync();

        Console.WriteLine(">>> Usuarios creados:");
        Console.WriteLine("    admin0001     / Admin1234");
        Console.WriteLine("    gestor0001    / Gestor1234");
        Console.WriteLine("    cajero0001    / Cajero1234");
        Console.WriteLine("    pendiente0001 / Pendiente1234  (sin aprobar)");
    }
}