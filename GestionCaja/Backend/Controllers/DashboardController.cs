using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Application.Dtos;
using Application.Interfaces;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IRepositorioDashboard _repositorio;
    private readonly IRepositorioUsuarios _repositorioUsuarios;

    public DashboardController(
        IRepositorioDashboard repositorio,
        IRepositorioUsuarios repositorioUsuarios)
    {
        _repositorio = repositorio;
        _repositorioUsuarios = repositorioUsuarios;
    }

    private int UsuarioActualId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    private string RolActual => User.FindFirst(ClaimTypes.Role)!.Value;

    // ─── BIENVENIDA: para todos los roles ───────────────────────
    [HttpGet("bienvenida")]
    public async Task<IActionResult> Bienvenida()
    {
        var usuario = await _repositorioUsuarios.ObtenerConRolAsync(UsuarioActualId);
        if (usuario is null)
            return NotFound(new { mensaje = "Usuario no encontrado" });

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        int turnos = await _repositorio.ContarTurnosAtendidosHoyAsync(
            hoy, RolActual, UsuarioActualId);

        string descripcion = RolActual switch
        {
            "Cajero" => "Turnos que atendiste hoy",
            "Gestor" => "Turnos que asignaste y ya fueron atendidos hoy",
            _ => "Total de turnos atendidos hoy en el sistema"
        };
        // El mismo número necesita tres explicaciones distintas.
        // Sin esto, un gestor vería "8" sin saber si son suyos o de todos.

        int? pendientes = null;
        if (RolActual == "Gestor" || RolActual == "Administrador")
            pendientes = await _repositorio.ContarPendientesAprobacionAsync();
        // "Para el usuario gestor mostrar usuarios creados pendientes
        //  de aprobación." Al cajero no le aplica: queda en null y el
        //  frontend no dibuja esa tarjeta.

        return Ok(new BienvenidaDto
        {
            Username = usuario.Username,
            Rol = usuario.Rol.Nombre,
            Correo = usuario.Correo,
            MiembroDesde = usuario.FechaCreacion,
            TurnosAtendidosHoy = turnos,
            DescripcionTurnos = descripcion,
            UsuariosPendientesAprobacion = pendientes
        });
    }

    // ─── DASHBOARD: solo administrador ──────────────────────────
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    // "Interfaz Dashboard (SOLO PARA ADMINISTRADOR)"
    public async Task<IActionResult> Indicadores(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta)
    {
        // "Mostrar indicadores de las cajas, cajeros y gestores
        //  con filtros con fecha."
        //
        // Los dos son opcionales (el "?"): sin ellos, uso hoy por defecto.

        var hoy = DateTime.UtcNow.Date;

        var fechaDesde = desde?.Date ?? hoy;
        var fechaHasta = (hasta?.Date ?? hoy).AddDays(1).AddTicks(-1);
        // ═══ ESTE DETALLE IMPORTA MÁS DE LO QUE PARECE ═══
        //
        // Si el usuario filtra "hasta el 11 de septiembre", espera que
        // se incluya TODO ese día. Pero "2026-09-11" como DateTime es
        // "2026-09-11 00:00:00", o sea la medianoche del inicio.
        //
        // Sin este ajuste, un turno emitido a las 10 de la mañana del 11
        // quedaría FUERA del rango, y el usuario vería un número menor
        // sin entender por qué.
        //
        // AddDays(1).AddTicks(-1) lleva la fecha al último instante del
        // día: 2026-09-11 23:59:59.9999999.
        //
        // Es un error clásico en reportes, y de los que nadie detecta
        // hasta que alguien cuadra los números a mano.

        if (fechaDesde > fechaHasta)
            return BadRequest(new { mensaje = "La fecha inicial no puede ser mayor que la final" });

        var indicadores = await _repositorio.ObtenerIndicadoresAsync(fechaDesde, fechaHasta);

        return Ok(indicadores);
    }
}