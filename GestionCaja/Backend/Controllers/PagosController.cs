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
public class PagosController : ControllerBase
{
    private readonly IRepositorioPagos _repositorio;
    private readonly IRepositorioCajas _repositorioCajas;

    public PagosController(IRepositorioPagos repositorio, IRepositorioCajas repositorioCajas)
    {
        _repositorio = repositorio;
        _repositorioCajas = repositorioCajas;
    }

    private int UsuarioActualId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // ─── REGISTRAR UN PAGO ─────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Registrar(RegistrarPagoDto dto)
    {
        // ═══ 1. HAY QUE TENER LA CAJA ABIERTA ═══
        var sesion = await _repositorioCajas.ObtenerSesionAbiertaDeUsuarioAsync(UsuarioActualId);
        if (sesion is null)
            return Conflict(new { mensaje = "Debes abrir una sesión de caja para registrar pagos" });
        // Dinero entrando a una caja física: tiene que haber una sesión
        // que responda por él. La misma regla que para atender turnos.

        // ═══ 2. EL CONTRATO TIENE QUE EXISTIR Y ESTAR ACTIVO ═══
        var contrato = await _repositorio.ObtenerContratoConServicioAsync(dto.ContratoId);
        if (contrato is null)
            return NotFound(new { mensaje = "El contrato no existe" });

        if (contrato.Estado != "VIG" && contrato.Estado != "REN")
            return Conflict(new
            {
                mensaje = $"No se puede cobrar un contrato en estado {contrato.Estado}"
            });
        // Ni sustituidos ni cancelados: solo el contrato vivo.

        // ═══ 3. LA VERIFICACIÓN AMABLE ═══
        if (await _repositorio.ExistePagoDelPeriodoAsync(contrato.ClienteId, dto.Periodo))
            return Conflict(new { mensaje = $"El periodo {dto.Periodo} ya está pagado" });

        // ═══ 4. EL SERVIDOR PONE EL MONTO ═══
        var pago = new Pago
        {
            ClienteId = contrato.ClienteId,
            ContratoId = contrato.Id,
            Periodo = dto.Periodo,

            Monto = contrato.Servicio.Precio,
            // ↑ LA LÍNEA MÁS IMPORTANTE DEL ARCHIVO.
            // El precio sale de la base, no de la petición.
            // Y se COPIA: si mañana sube el precio, este pago sigue
            // diciendo lo que se cobró hoy.

            FormaPagoId = contrato.FormaPagoId,
            // También copiada: cómo pagaba el cliente ese día.

            RegistradoPorUsuarioId = UsuarioActualId
        };

        // ═══ 5. LA GARANTÍA REAL ═══
        if (!await _repositorio.RegistrarAsync(pago))
            return Conflict(new { mensaje = $"El periodo {dto.Periodo} acaba de ser pagado" });
        // El paso 3 da un buen mensaje casi siempre.
        // El índice único garantiza que no haya doble cobro NUNCA.
        // Capa 3 y capa 4 sobre la misma regla, otra vez.

        return Ok(new
        {
            pago.Id,
            pago.Periodo,
            pago.Monto,
            mensaje = $"Pago de {dto.Periodo} registrado: ${pago.Monto:F2}"
            // :F2 = dos decimales. 30 se muestra como 30.00.
        });
    }

    // ─── HISTORIAL DE PAGOS DE UN CLIENTE ──────────────────────
    [HttpGet("cliente/{clienteId:int}")]
    public async Task<IActionResult> HistorialDeCliente(int clienteId)
    {
        var pagos = await _repositorio.ListarDeClienteAsync(clienteId);

        return Ok(pagos.Select(p => new PagoDto
        {
            Id = p.Id,
            Periodo = p.Periodo,
            Monto = p.Monto,
            FormaPago = p.FormaPago.Descripcion,
            Servicio = p.Contrato.Servicio.Nombre,
            FechaPago = p.FechaPago,
            RegistradoPor = p.RegistradoPorUsuario.Username
        }));
    }
}