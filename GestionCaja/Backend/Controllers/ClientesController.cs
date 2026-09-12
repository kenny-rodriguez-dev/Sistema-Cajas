using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Dtos;
using Application.Interfaces;
using Domain.Entities;
using System.ComponentModel.DataAnnotations;

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

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargaMasiva(
    IFormFile archivo,
    [FromServices] ILectorClientesArchivo lector)
    {
        // IFormFile = un archivo subido. .NET lo arma solo a partir
        // de una petición multipart/form-data.
        //
        // [FromServices] = pide esta dependencia solo para este método,
        // en vez de meterla en el constructor. Útil cuando solo la usa uno.

        // ─── VALIDACIONES DEL ARCHIVO ───
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { mensaje = "Debes adjuntar un archivo" });

        if (archivo.Length > 5 * 1024 * 1024)
            return BadRequest(new { mensaje = "El archivo no puede superar 5 MB" });
        // Límite de tamaño. Sin esto, alguien sube un archivo de 2 GB
        // y tumba el servidor. Se llama denegación de servicio.

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".csv")
            return BadRequest(new { mensaje = "Solo se aceptan archivos .xlsx o .csv" });
        // Valido por extensión, no por el Content-Type que manda el
        // navegador: ese se puede falsificar trivialmente.

        // ─── LEER ───
        List<(int fila, CrearClienteDto dto)> filas;
        try
        {
            Console.WriteLine($">>> Archivo: {archivo.FileName} | {archivo.Length} bytes");
            // Espía temporal: quiero saber QUÉ llegó de verdad.

            using var flujo = archivo.OpenReadStream();
            filas = lector.Leer(flujo, extension);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> FALLÓ LA LECTURA: {ex.GetType().Name}: {ex.Message}");
            // ↑ El tipo de excepción y su mensaje. Esto es lo que faltaba.

            return BadRequest(new
            {
                mensaje = "No se pudo leer el archivo",
                detalle = ex.Message
                // Devolver el detalle está bien EN DESARROLLO.
                // En producción NUNCA: un mensaje de excepción puede
                // filtrar rutas del servidor, nombres de tablas, versiones
                // de librerías... información gratis para un atacante.
                // Ahí solo va el mensaje genérico y el detalle al log.
            });
        }

        // ─── VALIDAR TODO ANTES DE GUARDAR NADA ───
        var errores = new List<ErrorFilaDto>();

        // Las identificaciones que YA existen en la base.
        var yaExisten = await _repositorio.ObtenerIdentificacionesAsync(
            filas.Select(f => f.dto.Identificacion).ToList()
        );

        // Las que vienen repetidas DENTRO del mismo archivo.
        var repetidasEnArchivo = filas
            .GroupBy(f => f.dto.Identificacion)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();
        // Detalle que a todo el mundo se le olvida: el archivo puede
        // traer la misma cédula dos veces. La base no te avisa hasta
        // que ya insertaste la primera.

        foreach (var (numeroFila, dto) in filas)
        {
            var erroresFila = new List<string>();

            // ═══ LAS MISMAS REGLAS DEL FORMULARIO, REUTILIZADAS ═══
            var contexto = new ValidationContext(dto);
            var resultados = new List<ValidationResult>();

            Validator.TryValidateObject(dto, contexto, resultados, validateAllProperties: true);
            erroresFila.AddRange(resultados.Select(r => r.ErrorMessage ?? "Dato inválido"));
            //
            // Esto ejecuta a mano los mismos DataAnnotations que
            // [ApiController] ejecuta solo en el formulario.
            //
            // Resultado: si mañana cambias una regla en CrearClienteDto,
            // cambia en los dos lados a la vez. Una sola fuente de verdad.

            if (yaExisten.Contains(dto.Identificacion))
                erroresFila.Add("Esa identificación ya está registrada");

            if (repetidasEnArchivo.Contains(dto.Identificacion))
                erroresFila.Add("Esa identificación está repetida dentro del archivo");

            if (erroresFila.Count > 0)
                errores.Add(new ErrorFilaDto { Fila = numeroFila, Errores = erroresFila });
        }

        // ═══════════════════════════════════════════════════════════
        //   TODO O NADA
        // ═══════════════════════════════════════════════════════════
        if (errores.Count > 0)
        {
            return BadRequest(new ResultadoCargaDto
            {
                Exitoso = false,
                FilasLeidas = filas.Count,
                FilasImportadas = 0,
                Errores = errores,
                Mensaje = $"Se encontraron errores en {errores.Count} fila(s). " +
                          "No se importó ningún registro."
            });
        }
        //
        // ═══ POR QUÉ TODO O NADA, Y NO "IMPORTA LAS BUENAS" ═══
        //
        // Si importara las válidas y rechazara las malas, el usuario
        // corrige el archivo, lo vuelve a subir... y las que ya entraron
        // salen como duplicadas. Tendría que editar el archivo para
        // quitarlas a mano, sabiendo exactamente cuáles pasaron.
        //
        // Con todo o nada, corrige y reintenta. Punto.
        //
        // ¿Es siempre la respuesta correcta? No. Para un archivo de
        // 50.000 filas donde una tiene un error, es frustrante; ahí
        // conviene importar lo bueno y devolver un archivo con lo malo.
        // Lo que importa es que sea una decisión, no un accidente.

        // ─── GUARDAR ───
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