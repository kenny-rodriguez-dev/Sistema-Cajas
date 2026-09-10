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
    // Propiedad de solo lectura que saca mi id del token.
    // La escribo una vez y la uso en todos los métodos.

    // ───────────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Gestor,Administrador")]
    // La coma significa O: gestor O administrador.
    // Si quisiera exigir AMBOS roles, serían dos atributos [Authorize] apilados.
    public async Task<IActionResult> Listar()
    {
        var cajas = await _repositorio.ListarConDetalleAsync();

        return Ok(cajas.Select(c => new CajaDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            CajerosAsignados = c.Asignaciones.Select(a => a.Usuario.Username).ToList(),
            OcupadaPor = c.Sesiones.FirstOrDefault()?.Usuario.Username
            // El repositorio ya filtró solo las sesiones abiertas,
            // así que si hay una, esa es la que ocupa la caja.
        }));
    }

    // ───────────────────────────────────────────────────────────
    [HttpPost("{cajaId}/asignaciones")]
    [Authorize(Roles = "Gestor")]
    // "Los usuarios gestores se encargan de asignar a los usuarios
    //  cajeros a las cajas."
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

        // ═══ LA REGLA DEL MÁXIMO 2 ═══
        int asignados = await _repositorio.ContarAsignadosAsync(cajaId);
        if (asignados >= 2)
            return Conflict(new
            {
                mensaje = "Esta caja ya tiene el máximo de 2 cajeros asignados"
            });
        //
        // Acuérdate de lo que te dije al leer el diagrama: el esquema NO
        // PUEDE expresar "máximo 2". Un diagrama captura la estructura
        // (uno a muchos), no los límites de negocio.
        //
        // Por eso esta regla vive aquí, en código. Y fíjate que aquí SÍ
        // basta con un "if": a diferencia de la sesión, dos gestores
        // asignando el mismo cajero al mismo segundo es un escenario que
        // no ocurre en la práctica, y si ocurriera, tener 3 asignados un
        // instante no rompe nada crítico. La sesión sí: dos personas
        // cobrando en la misma caja es dinero descuadrado.
        //
        // Esa diferencia de criterio es lo que se te va a preguntar:
        // no todas las reglas necesitan la misma dureza.

        await _repositorio.AsignarAsync(new AsignacionCaja
        {
            CajaId = cajaId,
            UsuarioId = dto.UsuarioId
        });

        return Ok(new { mensaje = $"{usuario.Username} asignado a {caja.Nombre}" });
    }

    // ───────────────────────────────────────────────────────────
    [HttpPost("{cajaId}/abrir")]
    [Authorize(Roles = "Cajero")]
    public async Task<IActionResult> AbrirSesion(int cajaId)
    {
        int miId = UsuarioActualId;

        if (!await _repositorio.EstaAsignadoAsync(cajaId, miId))
            return StatusCode(403, new { mensaje = "No estás asignado a esta caja" });
        // Que seas Cajero no basta: tienes que ser cajero DE ESTA caja.
        // El [Authorize] revisa el rol; esto revisa el permiso concreto.

        var miSesionAbierta = await _repositorio.ObtenerSesionAbiertaDeUsuarioAsync(miId);
        if (miSesionAbierta is not null)
            return Conflict(new
            {
                mensaje = $"Ya tienes una sesión abierta en la caja {miSesionAbierta.CajaId}"
            });

        // ═══ LA VERIFICACIÓN AMABLE ═══
        if (await _repositorio.HaySesionAbiertaEnCajaAsync(cajaId))
            return Conflict(new { mensaje = "Otro cajero está operando esta caja" });
        //
        // Este "if" NO garantiza nada. Existe solo para dar un mensaje
        // claro en el 99.9% de los casos. La garantía real está abajo.

        bool abrio = await _repositorio.AbrirSesionAsync(new SesionCaja
        {
            CajaId = cajaId,
            UsuarioId = miId
        });

        if (!abrio)
            return Conflict(new { mensaje = "Otro cajero acaba de tomar esta caja" });
        // ↑ AQUÍ ESTÁ LA GARANTÍA. Si la base rechazó el INSERT por el
        //   índice único, es porque hubo una carrera y yo la perdí.
        //
        // Dos capas para la misma regla:
        //   el "if"  -> buen mensaje, casi siempre
        //   el índice -> correctitud, siempre

        return Ok(new { mensaje = "Sesión abierta" });
    }

    // ───────────────────────────────────────────────────────────
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
}