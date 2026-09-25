using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Domain.Entities;

namespace Infrastructure.Services;

public class ServicioDeTokens
{
    private readonly IConfiguration _configuracion;

    public ServicioDeTokens(IConfiguration configuracion) => _configuracion = configuracion;

    public (string token, DateTime expira, string jti) GenerarToken(Usuario usuario)
    // ↑ La tupla ahora devuelve TRES cosas: el login necesita el Jti
    //   para guardar la sesión.
    {
        var jti = Guid.NewGuid().ToString();
        // Antes el Guid se creaba adentro de la lista de claims y se perdía.
        // Ahora lo guardo en una variable para poder devolverlo.

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Username),
            new Claim(ClaimTypes.Role, usuario.Rol.Nombre),
            new Claim(JwtRegisteredClaimNames.Jti, jti)
        };

        var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracion["Jwt:Clave"]!));
        var credenciales = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);

        var minutos = int.Parse(_configuracion["Jwt:MinutosDeVida"]!);
        var expira = DateTime.UtcNow.AddMinutes(minutos);

        var token = new JwtSecurityToken(
            issuer: _configuracion["Jwt:Emisor"],
            audience: _configuracion["Jwt:Audiencia"],
            claims: claims,
            expires: expira,
            signingCredentials: credenciales
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expira, jti);
    }
}