using Application.Dtos;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]   // -> /api/auth
public class AuthController : ControllerBase
{
    private readonly AppDbContext _contexto;
    private readonly ServicioDeTokens _servicioDeTokens;
    private readonly IRepositorioRecuperacion _repositorioRecuperacion;
    private readonly IServicioRecuperacion _servicioRecuperacion;

    public AuthController(AppDbContext contexto, ServicioDeTokens servicioDeTokens,
        IRepositorioRecuperacion repositorioRecuperacion,
        IServicioRecuperacion servicioRecuperacion)
    {
        _contexto = contexto;
        _servicioDeTokens = servicioDeTokens;
        _repositorioRecuperacion = repositorioRecuperacion;
        _servicioRecuperacion = servicioRecuperacion;
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

    // ─── PASO 1: SOLICITAR ─────────────────────────────────────
    [HttpPost("recuperar")]
    [AllowAnonymous]
    // Obvio pero conviene decirlo: quien olvidó su contraseña
    // no puede tener un token para autenticarse.
    public async Task<IActionResult> SolicitarRecuperacion(SolicitarRecuperacionDto dto)
    {
        var usuario = await _repositorioRecuperacion.BuscarPorCorreoAsync(dto.Correo);

        // ═══════════════════════════════════════════════════════
        //   LA DECISIÓN MÁS IMPORTANTE DE TODO ESTE ARCHIVO
        // ═══════════════════════════════════════════════════════
        //
        // Si el correo NO existe, respondo EXACTAMENTE lo mismo que si
        // existiera. Ni un mensaje distinto, ni un código distinto.
        //
        // ¿Por qué? Si respondiera "ese correo no está registrado",
        // cualquiera podría probar miles de correos y armar una lista
        // de cuentas reales de la empresa. Eso se llama ENUMERACIÓN
        // DE USUARIOS, y es el mismo motivo por el que tu login dice
        // "usuario o contraseña incorrectos" sin especificar cuál falló.
        //
        // Es contraintuitivo: parece menos útil para el usuario honesto.
        // Pero el usuario honesto sabe cuál es su correo; el atacante no.

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
            // 15 minutos. Suficiente para revisar el correo, corto
            // para que un token filtrado sirva de poco.
        });

        // ═══ AQUÍ IRÍA EL ENVÍO DEL CORREO ═══
        //
        // En un sistema real, esta línea sería algo como:
        //     await _servicioCorreo.EnviarAsync(usuario.Correo, enlace);
        // con un proveedor como SendGrid, Mailgun o un SMTP propio.
        //
        // No lo implemento porque requiere credenciales de un servicio
        // externo, y el enunciado no lo pide. En su lugar imprimo el
        // enlace en la consola: el token se genera, se guarda y se
        // valida EXACTAMENTE igual que en producción. Lo único que
        // falta es el transporte.
        //
        // Si te preguntan por esto en una sustentación, esa es la
        // respuesta: "el flujo está completo, falta conectar un
        // proveedor de correo, y es una línea".

        Console.WriteLine("════════════════════════════════════════════");
        Console.WriteLine($"  RECUPERACIÓN para {usuario.Username} ({usuario.Correo})");
        Console.WriteLine($"  Enlace: http://localhost:3000/restablecer?token={tokenPlano}");
        Console.WriteLine($"  Expira: {DateTime.UtcNow.AddMinutes(15):HH:mm:ss} UTC");
        Console.WriteLine("════════════════════════════════════════════");

        return Ok(new
        {
            mensaje = "Si el correo está registrado, recibirás instrucciones para restablecer tu contraseña."
            // Mensaje IDÉNTICO al del caso anterior. Sin excepciones.
        });
    }

    // ─── PASO 2: RESTABLECER ───────────────────────────────────
    [HttpPost("restablecer")]
    [AllowAnonymous]
    public async Task<IActionResult> Restablecer(RestablecerPasswordDto dto)
    {
        var vigentes = await _repositorioRecuperacion.ObtenerTokensVigentesAsync();
        // Ya filtrados: sin usar y sin expirar.

        TokenRecuperacion? tokenValido = null;

        foreach (var candidato in vigentes)
        {
            if (_servicioRecuperacion.VerificarToken(dto.Token, candidato.TokenHash))
            {
                tokenValido = candidato;
                break;
            }
        }
        // Recorro y verifico uno por uno, por lo del salt de BCrypt.

        if (tokenValido is null)
            return BadRequest(new
            {
                mensaje = "El enlace no es válido o ya expiró. Solicita uno nuevo."
            });
        // Un solo mensaje para tres casos distintos: token inexistente,
        // ya usado, o expirado. Distinguirlos le daría información
        // a alguien que esté probando tokens al azar.

        var usuario = tokenValido.Usuario;

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);

        tokenValido.FechaUso = DateTime.UtcNow;
        // ═══ QUEMA EL TOKEN ═══
        // Un solo uso. Aunque alguien tuviera copia del enlace,
        // a partir de este momento no sirve para nada.

        await _repositorioRecuperacion.GuardarAsync();
        // Las dos cosas en un solo SaveChanges: EF las mete en la misma
        // transacción. O se cambia la contraseña Y se quema el token,
        // o no pasa ninguna de las dos.

        Console.WriteLine($">>> Contraseña restablecida para {usuario.Username}");

        return Ok(new { mensaje = "Contraseña actualizada. Ya puedes iniciar sesión." });
    }
}