using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Dtos;
using Application.Interfaces;
using Domain.Entities;

namespace Backend.Controllers;
// ═══ MIRA LOS USING ═══
// Desapareció "using Microsoft.EntityFrameworkCore".
// Este archivo ya no sabe que existe Entity Framework.

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cajero")]
public class ClientesController : ControllerBase
{
    private readonly IRepositorioClientes _repositorio;
    // Pido la INTERFAZ, no la clase concreta.
    //
    // Esta clase no sabe si al otro lado hay SQL Server, PostgreSQL o
    // una lista en memoria. Solo sabe que le van a cumplir el contrato.
    // Eso es la inversión de dependencias, en una línea.

    public ClientesController(IRepositorioClientes repositorio)
    {
        _repositorio = repositorio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var clientes = await _repositorio.ListarActivosAsync();

        var respuesta = clientes.Select(c => new ClienteDto
        {
            Id = c.Id,
            Nombres = c.Nombres,
            Apellidos = c.Apellidos,
            Identificacion = c.Identificacion,
            Correo = c.Correo,
            Telefono = c.Telefono,
            Direccion = c.Direccion,
            ReferenciaDireccion = c.ReferenciaDireccion
        });

        return Ok(respuesta);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var cliente = await _repositorio.ObtenerPorIdAsync(id);

        if (cliente is null)
            return NotFound(new { mensaje = "Cliente no encontrado" });

        return Ok(new ClienteDto
        {
            Id = cliente.Id,
            Nombres = cliente.Nombres,
            Apellidos = cliente.Apellidos,
            Identificacion = cliente.Identificacion,
            Correo = cliente.Correo,
            Telefono = cliente.Telefono,
            Direccion = cliente.Direccion,
            ReferenciaDireccion = cliente.ReferenciaDireccion
        });
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearClienteDto dto)
    {
        // Capa 2 (formato) ya la validó [ApiController] antes de llegar aquí.

        // Capa 3: la regla de negocio. Ahora sin una línea de SQL a la vista.
        if (await _repositorio.ExisteIdentificacionAsync(dto.Identificacion))
            return Conflict(new
            {
                mensaje = $"Ya existe un cliente con la identificación {dto.Identificacion}"
            });

        var cliente = new Cliente
        {
            Nombres = dto.Nombres,
            Apellidos = dto.Apellidos,
            Identificacion = dto.Identificacion,
            Correo = dto.Correo,
            Telefono = dto.Telefono,
            Direccion = dto.Direccion,
            ReferenciaDireccion = dto.ReferenciaDireccion
        };

        await _repositorio.AgregarAsync(cliente);

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = cliente.Id },
            new { id = cliente.Id, mensaje = "Cliente creado correctamente" }
        );
    }
}