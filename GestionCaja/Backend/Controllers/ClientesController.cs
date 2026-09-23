using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Dtos;
using Application.Interfaces;
using Domain.Entities;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cajero")]
public class ClientesController : ControllerBase
{
    private readonly IRepositorioClientes _repositorio;

    public ClientesController(IRepositorioClientes repositorio) => _repositorio = repositorio;

    private static ClienteDto ADto(Cliente c) => new ClienteDto
    {
        Id = c.Id,
        Nombres = c.Nombres,
        Apellidos = c.Apellidos,
        Identificacion = c.Identificacion,
        Correo = c.Correo,
        Telefono = c.Telefono,
        Direccion = c.Direccion,
        ReferenciaDireccion = c.ReferenciaDireccion,
        Activo = c.Activo
    };
    // La conversión en un solo lugar. Antes estaba repetida en
    // Listar y en ObtenerPorId.

    // ─── LISTAR ────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool incluirInactivos = false)
    {
        // /api/clientes                        -> solo activos
        // /api/clientes?incluirInactivos=true  -> todos
        // El "= false" hace el parámetro opcional. Quien ya llamaba
        // sin él sigue recibiendo lo mismo que antes: no rompí a nadie.
        var clientes = await _repositorio.ListarAsync(incluirInactivos);
        return Ok(clientes.Select(ADto));
    }

    // ─── BUSCAR POR IDENTIFICACIÓN (NUEVO) ─────────────────────
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string? identificacion)
    {
        // "Si un usuario cliente ya se registró se debe buscar por
        //  identificación." Literal del enunciado.

        if (string.IsNullOrWhiteSpace(identificacion)
            || identificacion.Length < 10
            || identificacion.Length > 13
            || !identificacion.All(char.IsDigit))
            return BadRequest(new { mensaje = "La identificación debe tener entre 10 y 13 dígitos" });
        // Valido a mano porque viene por la URL, no en un DTO.
        // All(char.IsDigit) = "todos los caracteres son dígitos".

        var cliente = await _repositorio.BuscarPorIdentificacionAsync(identificacion);

        if (cliente is null)
            return NotFound(new { mensaje = "No hay ningún cliente con esa identificación" });

        return Ok(ADto(cliente));
        // Devuelve también los inactivos, con su marca. Si el cliente
        // existió y se dio de baja, el cajero debe saberlo: reactivarlo,
        // no intentar registrarlo de nuevo y chocar con el 409.
    }

    // ─── OBTENER UNO ───────────────────────────────────────────
    [HttpGet("{id:int}")]
    // ═══ :int ES UNA RESTRICCIÓN DE RUTA ═══
    // Sin ella, /api/clientes/buscar podría confundirse con "el cliente
    // cuyo id es la palabra buscar". Con :int, esta ruta SOLO acepta
    // números, y "buscar" cae en la de arriba sin ambigüedad.
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var cliente = await _repositorio.ObtenerPorIdAsync(id);
        if (cliente is null)
            return NotFound(new { mensaje = "Cliente no encontrado" });
        return Ok(ADto(cliente));
    }

    // ─── CREAR ─────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Crear(CrearClienteDto dto)
    {
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

    // ─── ACTUALIZAR (NUEVO) ────────────────────────────────────
    [HttpPut("{id:int}")]
    // PUT: reemplazar los datos de un recurso que ya existe.
    // POST crea, PUT actualiza, sobre la misma URL base.
    public async Task<IActionResult> Actualizar(int id, ActualizarClienteDto dto)
    {
        var cliente = await _repositorio.ObtenerParaEditarAsync(id);
        if (cliente is null)
            return NotFound(new { mensaje = "Cliente no encontrado" });

        cliente.Nombres = dto.Nombres;
        cliente.Apellidos = dto.Apellidos;
        cliente.Correo = dto.Correo;
        cliente.Telefono = dto.Telefono;
        cliente.Direccion = dto.Direccion;
        cliente.ReferenciaDireccion = dto.ReferenciaDireccion;
        // La identificación NO se toca. No está en el DTO.

        await _repositorio.GuardarAsync();
        // Change tracking: EF detecta solas las propiedades que cambiaron
        // y genera un UPDATE solo con esas columnas.

        return Ok(new { mensaje = "Cliente actualizado" });
    }

    // ─── ACTIVAR / DESACTIVAR (NUEVO) ──────────────────────────
    [HttpPost("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var cliente = await _repositorio.ObtenerParaEditarAsync(id);
        if (cliente is null)
            return NotFound(new { mensaje = "Cliente no encontrado" });

        if (cliente.Activo && await _repositorio.TieneContratoActivoAsync(id))
            return Conflict(new
            {
                mensaje = "No se puede desactivar: el cliente tiene un contrato vigente. Cancélalo primero."
            });
        // ═══ OTRA SALVAGUARDA ═══
        // Desactivar a un cliente que paga un servicio lo dejaría
        // "borrado" con un contrato activo colgando: seguiría generando
        // cobros a nombre de alguien que ya no aparece en ninguna lista.
        //
        // Primero se cancela el contrato, después se da de baja al
        // cliente. Mismo patrón que "primero se cierra la caja, después
        // se quita al cajero".

        cliente.Activo = !cliente.Activo;
        await _repositorio.GuardarAsync();

        return Ok(new
        {
            mensaje = $"Cliente {(cliente.Activo ? "activado" : "desactivado")}"
        });
        // Esto ES la eliminación lógica que pide el enunciado: la fila
        // nunca se borra, solo cambia de estado.
    }

    // ─── CARGA MASIVA (sin cambios) ────────────────────────────
    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva(
        IFormFile archivo,
        [FromServices] ILectorClientesArchivo lector)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { mensaje = "Debes adjuntar un archivo" });

        if (archivo.Length > 5 * 1024 * 1024)
            return BadRequest(new { mensaje = "El archivo no puede superar 5 MB" });

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".csv")
            return BadRequest(new { mensaje = "Solo se aceptan archivos .xlsx o .csv" });

        List<(int fila, CrearClienteDto dto)> filas;
        try
        {
            using var flujo = archivo.OpenReadStream();
            filas = lector.Leer(flujo, extension);
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensaje = "No se pudo leer el archivo", detalle = ex.Message });
        }

        if (filas.Count == 0)
            return BadRequest(new { mensaje = "El archivo no tiene filas de datos" });

        var errores = new List<ErrorFilaDto>();

        var yaExisten = await _repositorio.ObtenerIdentificacionesAsync(
            filas.Select(f => f.dto.Identificacion).ToList());

        var repetidasEnArchivo = filas
            .GroupBy(f => f.dto.Identificacion)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        foreach (var (numeroFila, dto) in filas)
        {
            var erroresFila = new List<string>();

            var contexto = new ValidationContext(dto);
            var resultados = new List<ValidationResult>();
            Validator.TryValidateObject(dto, contexto, resultados, validateAllProperties: true);
            erroresFila.AddRange(resultados.Select(r => r.ErrorMessage ?? "Dato inválido"));

            if (yaExisten.Contains(dto.Identificacion))
                erroresFila.Add("Esa identificación ya está registrada");

            if (repetidasEnArchivo.Contains(dto.Identificacion))
                erroresFila.Add("Esa identificación está repetida dentro del archivo");

            if (erroresFila.Count > 0)
                errores.Add(new ErrorFilaDto { Fila = numeroFila, Errores = erroresFila });
        }

        if (errores.Count > 0)
        {
            return BadRequest(new ResultadoCargaDto
            {
                Exitoso = false,
                FilasLeidas = filas.Count,
                FilasImportadas = 0,
                Errores = errores,
                Mensaje = $"Se encontraron errores en {errores.Count} fila(s). No se importó ningún registro."
            });
        }

        var clientes = filas.Select(f => new Cliente
        {
            Nombres = f.dto.Nombres,
            Apellidos = f.dto.Apellidos,
            Identificacion = f.dto.Identificacion,
            Correo = f.dto.Correo,
            Telefono = f.dto.Telefono,
            Direccion = f.dto.Direccion,
            ReferenciaDireccion = f.dto.ReferenciaDireccion
        }).ToList();

        await _repositorio.AgregarVariosAsync(clientes);

        return Ok(new ResultadoCargaDto
        {
            Exitoso = true,
            FilasLeidas = filas.Count,
            FilasImportadas = clientes.Count,
            Mensaje = $"{clientes.Count} cliente(s) importado(s) correctamente"
        });
    }
}