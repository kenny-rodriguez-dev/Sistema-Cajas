using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Backend.Models;

namespace Backend.Services;

public class ServicioDeTokens
{
    private readonly IConfiguration _configuracion;

    public ServicioDeTokens(IConfiguration configuracion)
    {
        _configuracion = configuracion;
    }
    // Inyección de dependencias otra vez: pido IConfiguration (el acceso
    // a appsettings.json) y .NET me lo entrega. No lo busco yo.

    public (string token, DateTime expira) GenerarToken(Usuario usuario)
    {
        // Ese "(string, DateTime)" es una TUPLA: devolver dos cosas de una vez
        // sin tener que inventar una clase solo para eso.

        // ─── 1. LOS CLAIMS ───
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Username),
            new Claim(ClaimTypes.Role, usuario.Rol.Nombre),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        // Un CLAIM es una afirmación sobre el usuario: "su id es 1",
        // "su rol es Administrador". El token es una bolsa de claims firmada.
        //
        // ClaimTypes.Role es especial: .NET lo reconoce y con él funciona
        // [Authorize(Roles = "Administrador")] sin escribir nada más.
        //
        // Jti es un id único por token, para poder revocarlo más adelante.
        //
        // ═══ REGLA QUE NO SE ROMPE ═══
        // NUNCA metas la contraseña, ni el hash, ni datos sensibles aquí.
        // El contenido del token lo puede leer CUALQUIERA. Ahora verás por qué.

        // ─── 2. LA FIRMA ───
        var clave = _configuracion["Jwt:Clave"]!;
        var llaveSimetrica = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
        // Convierto mi clave secreta (texto) a bytes, que es lo que
        // entiende el algoritmo de firma.

        var credenciales = new SigningCredentials(
            llaveSimetrica,
            SecurityAlgorithms.HmacSha256
        );
        // HmacSha256: el algoritmo que produce la firma.
        // "Simétrica" significa que la misma clave firma y verifica.

        // ─── 3. ARMAR EL TOKEN ───
        var minutos = int.Parse(_configuracion["Jwt:MinutosDeVida"]!);
        var expira = DateTime.UtcNow.AddMinutes(minutos);

        var token = new JwtSecurityToken(
            issuer: _configuracion["Jwt:Emisor"],
            audience: _configuracion["Jwt:Audiencia"],
            claims: claims,
            expires: expira,
            signingCredentials: credenciales
        );
        // issuer  = quién lo emitió
        // audience = para quién es
        // Sirven para que un token de OTRO sistema, aunque esté bien firmado,
        // no sirva en el tuyo.

        string tokenEnTexto = new JwtSecurityTokenHandler().WriteToken(token);
        // Convierte el objeto en ese texto largo con dos puntos que
        // viaja por la red.

        return (tokenEnTexto, expira);
    }
}