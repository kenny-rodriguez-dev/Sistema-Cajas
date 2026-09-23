using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Application.Dtos;
using Application.Interfaces;
using Domain.Entities;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CajasController : ControllerBase
{
    private readonly IRepositorioCajas _repositorio;

    public CajasController(IRepositorioCajas repositorio) => _repositorio = repositorio;

    private int UsuarioActualId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // ─── LISTAR (cambió la forma de los cajeros) ───────────────
    [HttpGet]
    [Authorize(Roles = "Gestor,Administrador")]
    public async Task<IActionResult> Listar()
    {
        var cajas = await _repositorio.ListarConDetalleAsync();

        return Ok(cajas.Select(c => new CajaDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            CajerosAsignados = c.Asignaciones
                .Select(a => new CajeroAsignadoDto
                {
                    Id = a.UsuarioId,
                    Username = a.Usuario.Username
                })
                .ToList(),
            // Ahora cada cajero va con su Id. Es lo que necesita
            // la pantalla para el botón de quitar.
            OcupadaPor = c.Sesiones.FirstOrDefault()?.Usuario.Username
        }));
    }

    // ─── ASIGNAR (sin cambios) ─────────────────────────────────
    [HttpPost("{cajaId}/asignaciones")]
    [Authorize(Roles = "Gestor")]
    public async Task<IActionResult> AsignarCajero(int cajaId, AsignarCajeroDto dto)
    {
        var caja = await _repositorio.ObtenerAsync(cajaId);
        if (caja is null)
            return NotFound(new { mensaje = "La caja no existe" });

        var usuario = await _repositorio.ObtenerUsuarioAsync(dto.UsuarioId);
        if (usuario is null)
            return NotFound(new { mensaje = "El usuario no existe" });

        if (usuario.Rol.Nombre != "Cajero")
            return BadRequest(new { mensaje = "Solo se pueden asignar usuarios con rol Cajero" });

        if (await _repositorio.EstaAsignadoAsync(cajaId, dto.UsuarioId))
            return Conflict(new { mensaje = "Ese cajero ya está asignado a esta caja" });

        int asignados = await _repositorio.ContarAsignadosAsync(cajaId);
        if (asignados >= 2)
            return Conflict(new { mensaje = "Esta caja ya tiene el máximo de 2 cajeros asignados" });

        await _repositorio.AsignarAsync(new AsignacionCaja
        {
            CajaId = cajaId,
            UsuarioId = dto.UsuarioId
        });

        return Ok(new { mensaje = $"{usuario.Username} asignado a {caja.Nombre}" });
    }

    // ─── QUITAR (NUEVO) ────────────────────────────────────────
    [HttpDelete("{cajaId}/asignaciones/{usuarioId}")]
    // DELETE, no POST. En REST el verbo dice qué haces:
    // POST crea una asignación, DELETE la elimina, sobre la MISMA URL.
    [Authorize(Roles = "Gestor")]
    public async Task<IActionResult> QuitarCajero(int cajaId, int usuarioId)
    {
        if (!await _repositorio.EstaAsignadoAsync(cajaId, usuarioId))
            return NotFound(new { mensaje = "Ese cajero no está asignado a esta caja" });

        var sesion = await _repositorio.ObtenerSesionAbiertaDeUsuarioAsync(usuarioId);

        if (sesion is not null && sesion.CajaId == cajaId)
            return Conflict(new
            {
                mensaje = "No se puede quitar: el cajero tiene esta caja abierta en este momento"
            });
        // ═══ LA REGLA QUE NADIE PIDE Y QUE HACE FALTA ═══
        //
        // Sin esto, el gestor quita a un cajero que está cobrando AHORA.
        // Queda una sesión abierta a nombre de alguien que ya no tiene
        // permiso sobre esa caja: el cajero no podría ni cerrarla
        // desde la pantalla, porque la caja ya no aparece en sus cajas.
        //
        // Primero se cierra la sesión, después se quita el permiso.
        // Otra salvaguarda: el enunciado no la pide hasta que alguien
        // la rompe.

        await _repositorio.QuitarAsignacionAsync(cajaId, usuarioId);

        return Ok(new { mensaje = "Cajero quitado de la caja" });
    }

    // ─── ABRIR (sin cambios) ───────────────────────────────────
    [HttpPost("{cajaId}/abrir")]
    [Authorize(Roles = "Cajero")]
    public async Task<IActionResult> AbrirSesion(int cajaId)
    {
        int miId = UsuarioActualId;

        if (!await _repositorio.EstaAsignadoAsync(cajaId, miId))
            return StatusCode(403, new { mensaje = "No estás asignado a esta caja" });

        var miSesionAbierta = await _repositorio.ObtenerSesionAbiertaDeUsuarioAsync(miId);
        if (miSesionAbierta is not null)
            return Conflict(new
            {
                mensaje = $"Ya tienes una sesión abierta en la caja {miSesionAbierta.CajaId}"
            });

        if (await _repositorio.HaySesionAbiertaEnCajaAsync(cajaId))
            return Conflict(new { mensaje = "Otro cajero está operando esta caja" });

        bool abrio = await _repositorio.AbrirSesionAsync(new SesionCaja
        {
            CajaId = cajaId,
            UsuarioId = miId
        });

        if (!abrio)
            return Conflict(new { mensaje = "Otro cajero acaba de tomar esta caja" });

        return Ok(new { mensaje = "Sesión abierta" });
    }

    // ─── CERRAR (sin cambios) ──────────────────────────────────
    [HttpPost("cerrar")]
    [Authorize(Roles = "Cajero")]
    public async Task<IActionResult> CerrarSesion()
    {
        var sesion = await _repositorio.ObtenerSesionAbiertaDeUsuarioAsync(UsuarioActualId);

        if (sesion is null)
            return NotFound(new { mensaje = "No tienes ninguna sesión abierta" });

        await _repositorio.CerrarSesionAsync(sesion);

        return Ok(new { mensaje = "Sesión cerrada" });
    }

    // ─── MIS CAJAS (sin cambios) ───────────────────────────────
    [HttpGet("mis-cajas")]
    [Authorize(Roles = "Cajero")]
    public async Task<IActionResult> MisCajas()
    {
        int miId = UsuarioActualId;
        var cajas = await _repositorio.ListarDeCajeroAsync(miId);
        var miSesion = await _repositorio.ObtenerSesionAbiertaDeUsuarioAsync(miId);

        return Ok(new
        {
            miSesionCajaId = miSesion?.CajaId,
            cajas = cajas.Select(c => new
            {
                c.Id,
                c.Nombre,
                OcupadaPor = c.Sesiones.FirstOrDefault()?.Usuario.Username,
                EsMia = miSesion != null && miSesion.CajaId == c.Id
            })
        });
    }
}