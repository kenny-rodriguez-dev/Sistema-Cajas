using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Application.Dtos;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IRepositorioUsuarios _repositorioUsuarios;
    private readonly IRepositorioSesiones _repositorioSesiones;
    private readonly ServicioDeTokens _servicioDeTokens;
    private readonly IRepositorioRecuperacion _repositorioRecuperacion;
    private readonly IServicioRecuperacion _servicioRecuperacion;
    private readonly IConfiguration _configuracion;

    public AuthController(
        IRepositorioUsuarios repositorioUsuarios,
        IRepositorioSesiones repositorioSesiones,
        ServicioDeTokens servicioDeTokens,
        IRepositorioRecuperacion repositorioRecuperacion,
        IServicioRecuperacion servicioRecuperacion,
        IConfiguration configuracion)
    {
        _repositorioUsuarios = repositorioUsuarios;
        _repositorioSesiones = repositorioSesiones;
        _servicioDeTokens = servicioDeTokens;
        _repositorioRecuperacion = repositorioRecuperacion;
        _servicioRecuperacion = servicioRecuperacion;
        _configuracion = configuracion;
    }

    // ─── LOGIN ─────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var ahora = DateTime.UtcNow;
        var usuario = await _repositorioUsuarios.ObtenerPorUsernameAsync(dto.Username);

        if (usuario is not null && usuario.EstaBloqueado(ahora))
        {
            int minutos = (int)Math.Ceiling((usuario.BloqueadoHasta!.Value - ahora).TotalMinutes);
            return StatusCode(423, new
            {
                mensaje = $"Cuenta bloqueada por demasiados intentos fallidos. Intenta en {minutos} minuto(s)."
            });
        }

        bool credencialesValidas =
            usuario is not null &&
            BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);

        if (!credencialesValidas)
        {
            if (usuario is not null)
            {
                usuario.RegistrarIntentoFallido(ahora);
                await _repositorioUsuarios.GuardarAsync();
            }
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos" });
        }

        usuario!.RegistrarIngresoExitoso();
        await _repositorioUsuarios.GuardarAsync();

        if (!usuario.Activo)
            return Unauthorized(new { mensaje = "El usuario está inactivo" });

        if (!usuario.Aprobado)
            return Unauthorized(new
            {
                mensaje = "El usuario está pendiente de aprobación por un administrador"
            });

        var (token, expira, jti) = _servicioDeTokens.GenerarToken(usuario);

        await _repositorioSesiones.CrearAsync(new SesionUsuario
        {
            UsuarioId = usuario.Id,
            Jti = jti,
            FechaExpiracion = expira
        });
        // ═══ NUEVO: CADA LOGIN QUEDA ANOTADO ═══
        // Sin esta fila, el token recién emitido sería rechazado en su
        // primera petición: el guardia no lo encontraría en la lista.

        return Ok(new RespuestaLoginDto
        {
            Token = token,
            Username = usuario.Username,
            Rol = usuario.Rol.Nombre,
            Expira = expira
        });
    }

    // ─── LOGOUT (NUEVO) ────────────────────────────────────────
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var jti = User.FindFirst("jti")?.Value;

        if (jti is not null)
        {
            var sesion = await _repositorioSesiones.ObtenerPorJtiAsync(jti);
            sesion?.Cerrar(DateTime.UtcNow, "logout");
            await _repositorioSesiones.GuardarAsync();
        }

        return Ok(new { mensaje = "Sesión cerrada" });
        // ═══ POR PRIMERA VEZ, CERRAR SESIÓN ES REAL ═══
        // Antes, el botón de cerrar sesión solo borraba el token del
        // navegador. Si alguien lo había copiado, seguía sirviendo una hora.
        // Ahora ese token queda muerto en el servidor, aunque exista una copia.
    }

    // ─── QUIÉN SOY ─────────────────────────────────────────────
    [HttpGet("yo")]
    [Authorize]
    public IActionResult Yo()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var rol = User.FindFirst(ClaimTypes.Role)?.Value;
        return Ok(new { id, username, rol });
    }

    // ─── SOLICITAR RECUPERACIÓN ────────────────────────────────
    [HttpPost("recuperar")]
    [AllowAnonymous]
    public async Task<IActionResult> SolicitarRecuperacion(SolicitarRecuperacionDto dto)
    {
        var usuario = await _repositorioRecuperacion.BuscarPorCorreoAsync(dto.Correo);

        if (usuario is null)
        {
            return Ok(new
            {
                mensaje = "Si el correo está registrado, recibirás instrucciones para restablecer tu contraseña."
            });
        }

        await _repositorioRecuperacion.InvalidarTokensDeUsuarioAsync(usuario.Id);

        var (tokenPlano, tokenHash) = _servicioRecuperacion.GenerarToken();

        await _repositorioRecuperacion.CrearTokenAsync(new TokenRecuperacion
        {
            UsuarioId = usuario.Id,
            TokenHash = tokenHash,
            FechaExpiracion = DateTime.UtcNow.AddMinutes(15)
        });

        var urlFrontend = _configuracion["FrontendUrl"] ?? "http://localhost:3000";

        Console.WriteLine("════════════════════════════════════════════");
        Console.WriteLine($"  RECUPERACIÓN para {usuario.Username} ({usuario.Correo})");
        Console.WriteLine($"  Enlace: {urlFrontend}/restablecer?token={tokenPlano}");
        Console.WriteLine($"  Expira: {DateTime.UtcNow.AddMinutes(15):HH:mm:ss} UTC");
        Console.WriteLine("════════════════════════════════════════════");

        return Ok(new
        {
            mensaje = "Si el correo está registrado, recibirás instrucciones para restablecer tu contraseña."
        });
    }

    // ─── RESTABLECER ───────────────────────────────────────────
    [HttpPost("restablecer")]
    [AllowAnonymous]
    public async Task<IActionResult> Restablecer(RestablecerPasswordDto dto)
    {
        var ahora = DateTime.UtcNow;
        var vigentes = await _repositorioRecuperacion.ObtenerTokensVigentesAsync();

        TokenRecuperacion? tokenValido = null;
        foreach (var candidato in vigentes)
        {
            if (_servicioRecuperacion.VerificarToken(dto.Token, candidato.TokenHash))
            {
                tokenValido = candidato;
                break;
            }
        }

        if (tokenValido is null)
            return BadRequest(new { mensaje = "El enlace no es válido o ya expiró. Solicita uno nuevo." });

        var usuario = tokenValido.Usuario;
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
        usuario.Desbloquear();

        await _repositorioSesiones.CerrarTodasDeUsuarioAsync(usuario.Id, ahora, "cambio de contraseña");
        // ═══ NUEVO: CAMBIAR LA CONTRASEÑA CIERRA TODAS LAS SESIONES ═══
        // Es práctica estándar de seguridad. Si alguien restablece su
        // contraseña, probablemente sospecha que otro la conoce. Cualquier
        // sesión abierta con la contraseña vieja tiene que morir: si no,
        // el intruso seguiría adentro aunque la clave ya haya cambiado.

        tokenValido.FechaUso = ahora;

        await _repositorioRecuperacion.GuardarAsync();
        // Un solo guardado para CUATRO cambios: contraseña nueva, desbloqueo,
        // sesiones cerradas y token quemado. Todo o nada, en una transacción.
        // Esa es la unidad de trabajo del DbContext en acción.

        return Ok(new { mensaje = "Contraseña actualizada. Ya puedes iniciar sesión." });
    }
}