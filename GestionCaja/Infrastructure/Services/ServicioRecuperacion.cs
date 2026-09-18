using System.Security.Cryptography;
using Application.Interfaces;

namespace Infrastructure.Services;

public class ServicioRecuperacion : IServicioRecuperacion
{
    public (string tokenPlano, string tokenHash) GenerarToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        // ═══ RandomNumberGenerator, NO la clase Random ═══
        //
        // La clase Random de C# es PREDECIBLE: genera números a partir
        // de una semilla, y conociendo unos cuantos resultados se puede
        // calcular la semilla y predecir todos los siguientes.
        // Sirve para un juego, jamás para seguridad.
        //
        // RandomNumberGenerator es CRIPTOGRÁFICAMENTE SEGURO: usa
        // fuentes de entropía del sistema operativo y no se puede
        // predecir ni reconstruir.
        //
        // 32 bytes = 256 bits. Adivinarlo por fuerza bruta es
        // computacionalmente imposible.
        //
        // Esta distinción es pregunta de entrevista de seguridad.

        string tokenPlano = Convert.ToHexString(bytes).ToLowerInvariant();
        // Convierto los bytes a texto hexadecimal para poder mandarlo
        // en un correo o en una URL. Quedan 64 caracteres.
        //
        // Uso hex y no Base64 a propósito: Base64 incluye "+", "/" y "="
        // que tienen significado especial dentro de una URL y habría
        // que escaparlos. El hexadecimal solo usa 0-9 y a-f.

        string tokenHash = BCrypt.Net.BCrypt.HashPassword(tokenPlano);
        // Lo hasheo igual que una contraseña. Lo que se guarda en la
        // base es esto; lo que viaja al usuario es el tokenPlano.

        return (tokenPlano, tokenHash);
    }

    public bool VerificarToken(string tokenPlano, string tokenHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(tokenPlano, tokenHash);
        }
        catch
        {
            return false;
            // Si el hash guardado está corrupto, BCrypt lanza excepción.
            // Prefiero devolver "no coincide" antes que tumbar la API
            // con un dato mal formado.
        }
    }
}