using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public static class SembradorDeDatos
{
    // "static" = no hace falta crear un objeto para usarla.
    // Se llama directo: SembradorDeDatos.SembrarAsync(...)

    public static async Task SembrarAsync(AppDbContext contexto)
    {
        bool yaHayAdministrador = await contexto.Usuarios
            .AnyAsync(u => u.RolId == 1);

        Console.WriteLine("HASH 1: " + BCrypt.Net.BCrypt.HashPassword("Admin1234"));
        Console.WriteLine("HASH 2: " + BCrypt.Net.BCrypt.HashPassword("Admin1234"));

        if (yaHayAdministrador) return;
        // ESTA LÍNEA ES LA IMPORTANTE.
        //
        // El seeder corre cada vez que arrancas la aplicación. Sin este
        // "return", crearías un admin nuevo en cada F5 hasta llenar la tabla.
        //
        // La palabra técnica es IDEMPOTENTE: se puede correr mil veces
        // y el resultado es el mismo que correrlo una.

        var admin = new Usuario
        {
            Username = "admin0001",
            // Cumple las reglas del enunciado: 9 caracteres (entre 8 y 20),
            // letras, al menos un número, sin caracteres especiales.

            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234"),
            // AQUÍ OCURRE LA MAGIA.
            //
            // Escribo la contraseña legible UNA sola vez, aquí, y lo que
            // se guarda es algo como:
            //   $2a$11$N9qo8uLOickgx2ZMRZoMye/Ci0.9kJq...
            //
            // Un hash es de un solo sentido: no existe función que me
            // devuelva "Admin1234" a partir de ese texto. Si alguien roba
            // tu base de datos, se lleva basura ilegible.

            Correo = "admin@gestioncaja.com",
            RolId = 1,
            Activo = true,
            Aprobado = true
            // Aprobado en true: el primer admin no tiene quién lo apruebe.
        };

        contexto.Usuarios.Add(admin);
        await contexto.SaveChangesAsync();

        Console.WriteLine(">>> Administrador inicial creado: admin0001 / Admin1234");
        // Solo para que no se te olvide la contraseña mientras desarrollas.
        // En un sistema real esto jamás se imprime.
    }
}