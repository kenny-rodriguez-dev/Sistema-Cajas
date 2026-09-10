using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public static class SembradorDeDatos
{
    public static async Task SembrarAsync(AppDbContext contexto)
    {
        if (await contexto.Usuarios.AnyAsync()) return;

        var usuarios = new List<Usuario>
        {
            new Usuario { Username = "admin0001",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234"),  Correo = "admin@gestioncaja.com",  RolId = 1, Activo = true, Aprobado = true },
            new Usuario { Username = "gestor0001", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Gestor1234"), Correo = "gestor@gestioncaja.com", RolId = 2, Activo = true, Aprobado = true },
            new Usuario { Username = "cajero0001", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cajero1234"), Correo = "cajero1@gestioncaja.com", RolId = 3, Activo = true, Aprobado = true },
            new Usuario { Username = "cajero0002", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cajero1234"), Correo = "cajero2@gestioncaja.com", RolId = 3, Activo = true, Aprobado = true },
            new Usuario { Username = "cajero0003", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cajero1234"), Correo = "cajero3@gestioncaja.com", RolId = 3, Activo = true, Aprobado = true },
            new Usuario { Username = "pendiente0001", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pendiente1234"), Correo = "pendiente@gestioncaja.com", RolId = 3, Activo = true, Aprobado = false }
        };
        // Tres cajeros a propósito: dos para llenar una caja,
        // y un tercero para comprobar que el "máximo 2" rechaza.

        contexto.Usuarios.AddRange(usuarios);
        await contexto.SaveChangesAsync();

        Console.WriteLine(">>> Usuarios: admin0001/Admin1234, gestor0001/Gestor1234,");
        Console.WriteLine("    cajero0001, cajero0002, cajero0003 (todos con Cajero1234),");
        Console.WriteLine("    pendiente0001/Pendiente1234 (sin aprobar)");
    }
}