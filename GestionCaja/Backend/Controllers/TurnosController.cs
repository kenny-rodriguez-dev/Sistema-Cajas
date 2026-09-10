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
public class TurnosController : ControllerBase
{
    private readonly IRepositorioTurnos _repositorio;
    private readonly IRepositorioCajas _repositorioCajas;

    public TurnosController(IRepositorioTurnos repositorio, IRepositorioCajas repositorioCajas)
    {
        _repositorio = repositorio;
        _repositorioCajas = repositorioCajas;
    }

    private int UsuarioActualId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // ───────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Gestor")]
    // "Los usuarios gestores... asignan los turnos por caja."
    public async Task<IActionResult> Crear(CrearTurnoDto dto)
    {
        var caja = await _repositorioCajas.ObtenerAsync(dto.CajaId);
        if (caja is null)
            return NotFound(new { mensaje = "La caja no existe" });

        var tipo = await _repositorio.ObtenerTipoAsync(dto.TipoAtencionId);
        if (tipo is null)
            return NotFound(new { mensaje = "El tipo de atención no existe" });

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        int numero = await _repositorio.SiguienteNumeroAsync(tipo.Id, hoy);
        // ↑ El stored procedure. Devuelve un número único, garantizado.

        string codigo = $"{tipo.Codigo}{numero:D4}";
        // ═══ ASÍ SE ARMA "AC0001" ═══
        //
        // "D4" significa: número decimal, relleno con ceros hasta 4 dígitos.
        //     1  -> "0001"
        //    27  -> "0027"
        //   999  -> "0999"
        //
        // Junto con las 2 letras del tipo, salen los 6 caracteres exactos
        // que exige el enunciado: 2 mayúsculas + 4 números.

        var turno = new Turno
        {
            Codigo = codigo,
            TipoAtencionId = tipo.Id,
            CajaId = dto.CajaId,
            AsignadoPorUsuarioId = UsuarioActualId,
            Fecha = hoy,
            Estado = "PEN"
        };

        await _repositorio.CrearAsync(turno);

        return Ok(new { turno.Id, turno.Codigo, mensaje = $"Turno {codigo} generado" });
    }

    // ───────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ListarDelDia([FromQuery] int? cajaId)
    {
        // [FromQuery] = el valor viene de la URL: /api/turnos?cajaId=1
        // Al ser int? es opcional: sin él, trae los de todas las cajas.

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var turnos = await _repositorio.ListarDelDiaAsync(hoy, cajaId);

        return Ok(turnos.Select(t => new TurnoDto
        {
            Id = t.Id,
            Codigo = t.Codigo,
            TipoAtencion = t.TipoAtencion.Descripcion,
            Caja = t.Caja.Nombre,
            Estado = t.Estado,
            FechaHoraEmision = t.FechaHoraEmision,
            AtendidoPor = t.AtendidoPorUsuario?.Username
        }));
    }

    // ───────────────────────────────────────────────────────────
    [HttpPost("{turnoId}/atender")]
    [Authorize(Roles = "Cajero")]
    // "Los usuarios cajeros manejan los turnos."
    public async Task<IActionResult> Atender(int turnoId)
    {
        var miSesion = await _repositorioCajas
            .ObtenerSesionAbiertaDeUsuarioAsync(UsuarioActualId);

        if (miSesion is null)
            return Conflict(new
            {
                mensaje = "Debes abrir una sesión de caja antes de atender turnos"
            });
        // ═══ REGLA DE NEGOCIO QUE CONECTA CON LA 4A ═══
        // No puedes atender si no estás operando una caja. Aquí se ve
        // para qué servía la sesión que construiste ayer.

        var turno = await _repositorio.ObtenerPendienteAsync(turnoId);
        if (turno is null)
            return NotFound(new { mensaje = "El turno no existe o ya fue atendido" });

        if (turno.CajaId != miSesion.CajaId)
            return StatusCode(403, new
            {
                mensaje = "Ese turno pertenece a otra caja"
            });

        turno.Estado = "ATE";
        turno.AtendidoPorUsuarioId = UsuarioActualId;
        turno.FechaHoraAtencion = DateTime.UtcNow;

        await _repositorio.GuardarAsync();
        // No hace falta Update(): el DbContext viene siguiendo este objeto
        // desde que lo leyó y detecta solo los cambios. Change tracking.

        return Ok(new { mensaje = $"Turno {turno.Codigo} atendido" });
    }
}