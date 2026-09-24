using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Application.Dtos;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Services;

namespace Backend.Controllers;
// Fíjate en los using: ya no está Infrastructure.Data ni EntityFramework.
// Este controlador ya no sabe que existe una base de datos.

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IRepositorioUsuarios _repositorioUsuarios;
    private readonly ServicioDeTokens _servicioDeTokens;
    private readonly IRepositorioRecuperacion _repositorioRecuperacion;
    private readonly IServicioRecuperacion _servicioRecuperacion;
    private readonly IConfiguration _configuracion;

    public AuthController(
        IRepositorioUsuarios repositorioUsuarios,
        ServicioDeTokens servicioDeTokens,
        IRepositorioRecuperacion repositorioRecuperacion,
        IServicioRecuperacion servicioRecuperacion,
        IConfiguration configuracion)
    {
        _repositorioUsuarios = repositorioUsuarios;
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
        // Una sola lectura de la hora para todo el método. Si la pidiera
        // en cada línea, dos comprobaciones podrían ver momentos distintos.

        var usuario = await _repositorioUsuarios.ObtenerPorUsernameAsync(dto.Username);

        // ═══ PASO 1: EL BLOQUEO, ANTES QUE NADA ═══
        if (usuario is not null && usuario.EstaBloqueado(ahora))
        {
            int minutos = (int)Math.Ceiling((usuario.BloqueadoHasta!.Value - ahora).TotalMinutes);
            // Ceiling redondea hacia arriba: con 30 segundos restantes
            // dice "1 minuto", no "0 minutos".

            return StatusCode(423, new
            {
                mensaje = $"Cuenta bloqueada por demasiados intentos fallidos. Intenta en {minutos} minuto(s)."
            });
            // 423 Locked: "el recurso está bloqueado". Es el código que
            // mejor describe esto. Otros sistemas usan 429 (demasiadas
            // peticiones); los dos son defendibles.
            //
            // Y fíjate que la contraseña NO SE REVISÓ. Ni la correcta
            // abre la cuenta mientras dure el bloqueo.
        }

        // ═══ PASO 2: LA CONTRASEÑA ═══
        bool credencialesValidas =
            usuario is not null &&
            BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);

        if (!credencialesValidas)
        {
            if (usuario is not null)
            {
                usuario.RegistrarIntentoFallido(ahora);
                await _repositorioUsuarios.GuardarAsync();
                // La entidad decide si con este intento se bloquea.
                // El controlador ni sabe cuántos intentos hacen falta.
            }

            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos" });
            // Mismo mensaje genérico de siempre, exista o no el usuario.
        }

        // ═══ PASO 3: CONTRASEÑA CORRECTA ═══
        usuario!.RegistrarIngresoExitoso();
        await _repositorioUsuarios.GuardarAsync();
        // El contador vuelve a cero, AUNQUE más abajo el login falle por
        // estar inactivo o sin aprobar. Quien llegó aquí demostró que
        // conoce la contraseña: no hay por qué seguir contándole fallos.

        if (!usuario.Activo)
            return Unauthorized(new { mensaje = "El usuario está inactivo" });

        if (!usuario.Aprobado)
            return Unauthorized(new
            {
                mensaje = "El usuario está pendiente de aprobación por un administrador"
            });

        var (token, expira) = _servicioDeTokens.GenerarToken(usuario);

        return Ok(new RespuestaLoginDto
        {
            Token = token,
            Username = usuario.Username,
            Rol = usuario.Rol.Nombre,
            Expira = expira
        });
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
        // ═══ NUEVO: RESTABLECER TAMBIÉN DESBLOQUEA ═══
        // Piensa en el caso real: un cajero olvida su contraseña, la
        // intenta cinco veces, queda bloqueado, y pide recuperarla.
        // Si esto no desbloqueara, restablecería su contraseña y aun así
        // no podría entrar durante 15 minutos.
        //
        // Demostró que controla su correo: es prueba suficiente de que
        // la cuenta es suya. Este tipo de interacción entre dos
        // funcionalidades es lo que se escapa cuando se construye cada
        // una por separado.

        tokenValido.FechaUso = DateTime.UtcNow;

        await _repositorioRecuperacion.GuardarAsync();
        // Contraseña nueva, desbloqueo y token quemado en una sola
        // transacción: los tres objetos los sigue el mismo DbContext.

        return Ok(new { mensaje = "Contraseña actualizada. Ya puedes iniciar sesión." });
    }
}