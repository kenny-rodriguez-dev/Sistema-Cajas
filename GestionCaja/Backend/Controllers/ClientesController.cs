using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cajero")]
public class ClientesController : ControllerBase
{
    private readonly AppDbContext _contexto;
    // "readonly" = solo se puede asignar en el constructor y nunca más.
    // El guion bajo al inicio es solo convención de C# para campos privados.

    public ClientesController(AppDbContext contexto)
    {
        _contexto = contexto;
    }
    // INYECCIÓN DE DEPENDENCIAS EN ACCIÓN.
    // Yo nunca escribo "new AppDbContext(...)". Solo lo PIDO en el constructor.
    // .NET ve que lo necesito, se acuerda de que lo registré en Program.cs,
    // lo fabrica ya conectado a la base y me lo entrega.
    //
    // Por eso cambiar de base de datos no obliga a tocar esta clase.

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        // async / await / Task: mientras la base de datos responde (que es lento),
        // este hilo queda libre para atender otras peticiones en vez de quedarse
        // parado esperando. Con muchos usuarios eso es la diferencia entre
        // aguantar y caerse.
        //
        // Regla simple: si el método tiene await adentro, va marcado async
        // y devuelve Task<algo>.

        var clientes = await _contexto.Clientes
            .Where(c => c.Activo)
            // ELIMINACIÓN LÓGICA: solo traigo los activos.
            // Los "borrados" siguen en la tabla, pero nadie los ve.
            .Select(c => new ClienteDto
            {
                Id = c.Id,
                Nombres = c.Nombres,
                Apellidos = c.Apellidos,
                Identificacion = c.Identificacion,
                Correo = c.Correo,
                Telefono = c.Telefono,
                Direccion = c.Direccion,
                ReferenciaDireccion = c.ReferenciaDireccion
            })
            // El Select convierte cada entidad en su DTO.
            // Y hay un detalle fino: al ir ANTES del ToListAsync, EF genera un
            // SELECT que trae SOLO esas columnas de SQL Server. No trae la fila
            // completa para después descartar campos.
            .ToListAsync();
        // Recién en esta línea se ejecuta la consulta contra la base.
        // Todo lo de arriba solo iba ARMANDO la consulta.

        return Ok(clientes);   // HTTP 200 + el arreglo en JSON
    }

    [HttpGet("{id}")]
    // La URL queda /api/clientes/5 . Lo que va entre llaves se llama
    // "parámetro de ruta" y .NET lo pasa solo al argumento del mismo nombre.
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var cliente = await _contexto.Clientes
            .FirstOrDefaultAsync(c => c.Id == id && c.Activo);
        // FirstOrDefault = "dame el primero que cumpla, o null si no hay ninguno".
        // Si usara First() y no existiera, lanzaría una excepción. Prefiero el null.

        if (cliente is null)
            return NotFound(new { mensaje = "Cliente no encontrado" });
        // HTTP 404. Fíjate que devolver "no existe" NO es un error del programa:
        // es una respuesta legítima. Por eso no lanzo excepción.

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
        // ═══ IMPORTANTE: LA CAPA 2 YA CORRIÓ ANTES DE ESTA LÍNEA ═══
        //
        // Gracias al atributo [ApiController] de arriba, .NET revisó todos los
        // DataAnnotations del DTO. Si algo falló, respondió 400 con la lista de
        // errores y este método NUNCA se ejecutó.
        //
        // Es decir: si el código llegó hasta aquí, el formato ya está garantizado.
        // No escribí ni una línea para eso.

        // ═══ CAPA 3: LAS REGLAS DE NEGOCIO ═══
        // "Los clientes solo pueden registrarse una sola vez y se valida por su
        //  número de identificación."
        //
        // Esto NO se puede validar con un atributo, porque hay que ir a
        // preguntarle a la base de datos.

        bool yaExiste = await _contexto.Clientes
            .AnyAsync(c => c.Identificacion == dto.Identificacion);
        // AnyAsync devuelve true/false. Genera un SELECT que solo pregunta
        // "¿hay alguna?", sin traer la fila. Es mucho más barato que traerla.

        if (yaExiste)
            return Conflict(new
            {
                mensaje = $"Ya existe un cliente con la identificación {dto.Identificacion}"
            });
        // HTTP 409 Conflict, no 400. La diferencia importa:
        //   400 = "lo que me mandaste está mal escrito"
        //   409 = "está bien escrito, pero choca con algo que ya existe"
        //
        // Ese $ antes de las comillas permite meter variables dentro del texto
        // con llaves. Se llama interpolación de cadenas.

        // Convierto el DTO en entidad. Aquí es donde el mundo exterior
        // se transforma en algo que la base puede guardar.
        var cliente = new Cliente
        {
            Nombres = dto.Nombres,
            Apellidos = dto.Apellidos,
            Identificacion = dto.Identificacion,
            Correo = dto.Correo,
            Telefono = dto.Telefono,
            Direccion = dto.Direccion,
            ReferenciaDireccion = dto.ReferenciaDireccion
            // Id, Activo y FechaCreacion NO se copian del DTO:
            // los pone la base o el valor por defecto de la entidad.
        };

        _contexto.Clientes.Add(cliente);
        // OJO: esto NO toca la base todavía. Solo le dice al DbContext
        // "anota que esta fila hay que insertarla". Queda en memoria.

        await _contexto.SaveChangesAsync();
        // AHORA sí. Esta línea manda el INSERT a SQL Server.
        // Y después de ejecutarse, EF le rellena solo el Id que generó la base.
        // Por eso en la línea siguiente cliente.Id ya tiene valor.

        return CreatedAtAction(
            nameof(ObtenerPorId),        // qué método sirve para ver lo creado
            new { id = cliente.Id },     // con qué parámetro
            new { id = cliente.Id, mensaje = "Cliente creado correctamente" }
        );
        // HTTP 201 Created. Además de responder, agrega una cabecera "Location"
        // con la URL del recurso nuevo: /api/clientes/1
        // Ese es el estándar REST correcto para una creación exitosa.
    }
}