using Backend.Data;
using Backend.Dtos;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]   // -> /api/auth
public class AuthController : ControllerBase
{
    private readonly AppDbContext _contexto;
    private readonly ServicioDeTokens _servicioDeTokens;

    public AuthController(AppDbContext contexto, ServicioDeTokens servicioDeTokens)
    {
        _contexto = contexto;
        _servicioDeTokens = servicioDeTokens;
    }
    // Pido DOS cosas ahora. .NET me fabrica las dos.

    [HttpPost("login")]   // -> POST /api/auth/login
    public async Task<IActionResult> Login(LoginDto dto)
    {
        // ═══ PASO 1: BUSCAR AL USUARIO ═══
        var usuario = await _contexto.Usuarios
            .Include(u => u.Rol)
            // ESTA LÍNEA ES OBLIGATORIA.
            //
            // Por defecto EF NO trae las relaciones: si pido el usuario,
            // usuario.Rol viene en null y al leer usuario.Rol.Nombre
            // reventaría. Include le dice "traeme también el rol".
            // En SQL eso se traduce en un JOIN.
            .FirstOrDefaultAsync(u => u.Username == dto.Username);

        // ═══ PASO 2: VERIFICAR LA CONTRASEÑA ═══
        bool credencialesValidas =
            usuario is not null &&
            BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
        //
        // Verify hace lo que aprendiste en la 2A: saca el salt del hash
        // guardado, vuelve a hashear lo que el usuario escribió con ESE
        // mismo salt, y compara. Por eso no se puede usar "==".
        //
        // Y fíjate en el orden: el "&&" en C# es de cortocircuito. Si el
        // usuario es null, Verify ni se ejecuta y no revienta.

        if (!credencialesValidas)
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos" });
        //
        // ═══ ESTE MENSAJE GENÉRICO ES A PROPÓSITO ═══
        //
        // Fíjate que un usuario inexistente y una contraseña equivocada
        // dan EXACTAMENTE la misma respuesta.
        //
        // Si dijera "ese usuario no existe", un atacante podría probar
        // miles de nombres y armarse una lista de cuentas reales antes de
        // empezar a adivinar contraseñas. Eso se llama enumeración de usuarios.
        //
        // Es el paso 4 de la respuesta de tu sustentación.

        // ═══ PASO 3: REVISAR EL ESTADO ═══
        // Sus credenciales son correctas, pero eso no basta.

        if (!usuario!.Activo)
            return Unauthorized(new { mensaje = "El usuario está inactivo" });

        if (!usuario.Aprobado)
            return Unauthorized(new
            {
                mensaje = "El usuario está pendiente de aprobación por un administrador"
            });
        // Aquí SÍ soy específico, y no me contradigo: quien llegó hasta esta
        // línea YA demostró que sabe la contraseña. Decirle por qué no puede
        // entrar no le regala información a nadie más.

        // ═══ PASO 4: EMITIR EL TOKEN ═══
        var (token, expira) = _servicioDeTokens.GenerarToken(usuario);
        // Desarmo la tupla en dos variables de una sola línea.

        return Ok(new RespuestaLoginDto
        {
            Token = token,
            Username = usuario.Username,
            Rol = usuario.Rol.Nombre,
            Expira = expira
        });
    }
    [HttpGet("yo")]     // -> GET /api/auth/yo
    [Authorize]
    public IActionResult Yo()
    {
        // Este endpoint no toca la base de datos. Lee del TOKEN.
        //
        // Cuando UseAuthentication validó el token, desempacó sus claims
        // y los dejó en "User", una propiedad que ControllerBase me regala.
        // Está disponible en cualquier controlador, siempre.

        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var rol = User.FindFirst(ClaimTypes.Role)?.Value;
        // FindFirst busca un claim por nombre. El "?" evita que reviente
        // si no existe: devuelve null en vez de tirar excepción.
        //
        // Estos son EXACTAMENTE los claims que metiste en ServicioDeTokens.
        // Dieron la vuelta completa: los firmaste, viajaron al cliente,
        // volvieron, .NET verificó la firma, y aquí los estás leyendo.

        return Ok(new { id, username, rol });
    }
}