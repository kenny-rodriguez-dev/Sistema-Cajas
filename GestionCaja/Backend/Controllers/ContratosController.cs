using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Application.Dtos;
using Application.Interfaces;
using Domain.Entities;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cajero")]
// "Los usuarios cajeros atienden los siguientes procesos:
//  contratar, pagos, cambio de servicio, cambio de forma de pago,
//  cancelación de contrato."
public class ContratosController : ControllerBase
{
    private readonly IRepositorioContratos _repositorio;

    public ContratosController(IRepositorioContratos repositorio) => _repositorio = repositorio;

    private int UsuarioActualId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private static ContratoDto AConDto(Contrato c) => new ContratoDto
    {
        Id = c.Id,
        Cliente = $"{c.Cliente.Nombres} {c.Cliente.Apellidos}",
        Identificacion = c.Cliente.Identificacion,
        Servicio = c.Servicio.Nombre,
        VelocidadMbps = c.Servicio.VelocidadMbps,
        Precio = c.Servicio.Precio,
        FormaPago = c.FormaPago.Descripcion,
        Estado = c.Estado,
        FechaInicio = c.FechaInicio,
        FechaFin = c.FechaFin,
        ContratoAnteriorId = c.ContratoAnteriorId
    };
    // Método privado para no repetir esta conversión cinco veces.
    // "static" porque no usa nada de la instancia.

    // ─── CATÁLOGOS ─────────────────────────────────────────────
    [HttpGet("servicios")]
    public async Task<IActionResult> ListarServicios()
    {
        var servicios = await _repositorio.ListarServiciosAsync();
        return Ok(servicios.Select(s => new
        {
            s.Id,
            s.Nombre,
            s.Descripcion,
            s.VelocidadMbps,
            s.Precio,
            Equipos = s.Equipos.Where(e => e.Activo).Select(e => e.Nombre)
        }));
    }

    [HttpGet("formas-pago")]
    public async Task<IActionResult> ListarFormasPago()
    {
        var formas = await _repositorio.ListarFormasPagoAsync();
        return Ok(formas.Select(f => new { f.Id, f.Descripcion }));
    }

    // ─── HISTORIAL ─────────────────────────────────────────────
    [HttpGet("cliente/{clienteId}")]
    public async Task<IActionResult> HistorialDeCliente(int clienteId)
    {
        var contratos = await _repositorio.ListarDeClienteAsync(clienteId);
        return Ok(contratos.Select(AConDto));
        // Aquí se ve el valor de no editar nunca: este endpoint devuelve
        // TODA la historia del cliente, no solo su estado actual.
    }

    // ─── 1. CONTRATAR ──────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Contratar(CrearContratoDto dto)
    {
        if (!await _repositorio.ExisteClienteActivoAsync(dto.ClienteId))
            return NotFound(new { mensaje = "El cliente no existe" });

        var servicio = await _repositorio.ObtenerServicioAsync(dto.ServicioId);
        if (servicio is null)
            return NotFound(new { mensaje = "El servicio no existe" });

        if (!await _repositorio.ExisteFormaPagoAsync(dto.FormaPagoId))
            return NotFound(new { mensaje = "La forma de pago no existe" });

        var vigente = await _repositorio.ObtenerVigenteDeClienteAsync(dto.ClienteId);
        if (vigente is not null)
            return Conflict(new
            {
                mensaje = $"El cliente ya tiene el contrato {vigente.Id} activo. " +
                          "Usa cambio de servicio o cancélalo primero."
            });

        var ahora = DateTime.UtcNow;

        var contrato = new Contrato
        {
            ClienteId = dto.ClienteId,
            ServicioId = dto.ServicioId,
            FormaPagoId = dto.FormaPagoId,
            FechaInicio = ahora,
            FechaFin = ahora.AddMonths(dto.MesesDuracion),
            Estado = "VIG",
            CreadoPorUsuarioId = UsuarioActualId
        };

        if (!await _repositorio.CrearAsync(contrato))
            return Conflict(new { mensaje = "El cliente acaba de recibir otro contrato" });

        return Ok(new { contrato.Id, mensaje = "Contrato creado", contrato.FechaFin });
    }

    // ─── 2. CAMBIO DE SERVICIO ─────────────────────────────────
    [HttpPost("{contratoId}/cambiar-servicio")]
    public async Task<IActionResult> CambiarServicio(int contratoId, CambiarServicioDto dto)
    {
        var actual = await _repositorio.ObtenerConDetalleAsync(contratoId);

        if (actual is null)
            return NotFound(new { mensaje = "El contrato no existe" });

        if (actual.Estado != "VIG" && actual.Estado != "REN")
            return Conflict(new
            {
                mensaje = $"El contrato está en estado {actual.Estado} y no se puede modificar"
            });

        var nuevoServicio = await _repositorio.ObtenerServicioAsync(dto.NuevoServicioId);
        if (nuevoServicio is null)
            return NotFound(new { mensaje = "El servicio no existe" });

        if (nuevoServicio.Id == actual.ServicioId)
            return BadRequest(new { mensaje = "El cliente ya tiene ese servicio" });

        // ═══════════════════════════════════════════════════════
        //   AQUÍ ESTÁ LA REGLA COMPLETA DEL ENUNCIADO
        // ═══════════════════════════════════════════════════════

        var contratoNuevo = new Contrato
        {
            ClienteId = actual.ClienteId,
            ServicioId = nuevoServicio.Id,
            FormaPagoId = actual.FormaPagoId,   // la forma de pago se conserva

            FechaInicio = DateTime.UtcNow,      // el nuevo empieza HOY

            FechaFin = actual.FechaFin,
            // ↑↑↑ LA LÍNEA MÁS IMPORTANTE DE TODA LA REBANADA ↑↑↑
            //
            // "la fecha final del contrato se mantiene"
            //
            // NO es DateTime.UtcNow.AddMonths(12). Se COPIA la del
            // contrato anterior. El cliente no está firmando un plazo
            // nuevo: está cambiando de velocidad dentro del que ya tenía.

            Estado = "REN",                     // "estado de renovación de servicio"
            ContratoAnteriorId = actual.Id,     // el eslabón de la cadena
            CreadoPorUsuarioId = UsuarioActualId
        };

        bool exito = await _repositorio.SustituirAsync(actual, contratoNuevo);
        // Adentro: marca el viejo como SUS e inserta el nuevo,
        // dentro de una transacción. Las dos cosas, o ninguna.

        if (!exito)
            return Conflict(new { mensaje = "No se pudo completar el cambio de servicio" });

        string direccion = nuevoServicio.VelocidadMbps > actual.Servicio.VelocidadMbps
            ? "mayor" : "menor";

        return Ok(new
        {
            contratoAnterior = actual.Id,
            contratoNuevo = contratoNuevo.Id,
            mensaje = $"Cambio a servicio de {direccion} velocidad " +
                      $"({actual.Servicio.VelocidadMbps} → {nuevoServicio.VelocidadMbps} Mbps)",
            fechaFinConservada = contratoNuevo.FechaFin
        });
    }

    // ─── 3. CAMBIO DE FORMA DE PAGO ────────────────────────────
    [HttpPost("{contratoId}/cambiar-forma-pago")]
    public async Task<IActionResult> CambiarFormaPago(int contratoId, CambiarFormaPagoDto dto)
    {
        var contrato = await _repositorio.ObtenerConDetalleAsync(contratoId);

        if (contrato is null)
            return NotFound(new { mensaje = "El contrato no existe" });

        if (contrato.Estado != "VIG" && contrato.Estado != "REN")
            return Conflict(new { mensaje = "El contrato no está activo" });

        if (!await _repositorio.ExisteFormaPagoAsync(dto.NuevaFormaPagoId))
            return NotFound(new { mensaje = "La forma de pago no existe" });

        contrato.FormaPagoId = dto.NuevaFormaPagoId;
        await _repositorio.GuardarAsync();
        // ═══ FÍJATE EN LA DIFERENCIA ═══
        //
        // Aquí SÍ se edita el contrato, con un UPDATE simple.
        //
        // ¿Por qué el cambio de servicio crea uno nuevo y este no?
        // Porque el enunciado lo dice así, y tiene sentido de negocio:
        // el servicio define QUÉ recibe el cliente y CUÁNTO paga.
        // La forma de pago solo define CÓMO paga. No cambia el valor
        // del contrato, así que no amerita un registro histórico.
        //
        // Esta es la clase de decisión que tienes que poder justificar
        // en una sustentación.

        return Ok(new { mensaje = "Forma de pago actualizada" });
    }

    // ─── 4. CANCELACIÓN ────────────────────────────────────────
    [HttpPost("{contratoId}/cancelar")]
    public async Task<IActionResult> Cancelar(int contratoId)
    {
        var contrato = await _repositorio.ObtenerConDetalleAsync(contratoId);

        if (contrato is null)
            return NotFound(new { mensaje = "El contrato no existe" });

        if (contrato.Estado == "CAN")
            return Conflict(new { mensaje = "El contrato ya está cancelado" });

        if (contrato.Estado == "SUS")
            return Conflict(new { mensaje = "Ese contrato fue sustituido; cancela el vigente" });

        var ahora = DateTime.UtcNow;

        contrato.Estado = "CAN";
        contrato.FechaCancelacion = ahora;
        contrato.FechaFin = ahora;
        // "Si el cliente pide cancelación del contrato del servicio
        //  se actualiza el estado Y LA FECHA FIN."
        //
        // Aquí la fecha fin SÍ se toca, y se pone en hoy: el contrato
        // termina ahora, no en la fecha que estaba pactada.
        //
        // Compáralo con el cambio de servicio, donde la fecha se
        // conservaba. Son reglas opuestas a propósito, y cada una
        // responde a una situación distinta del negocio.

        await _repositorio.GuardarAsync();

        return Ok(new { mensaje = "Contrato cancelado", fechaFin = contrato.FechaFin });
    }
}