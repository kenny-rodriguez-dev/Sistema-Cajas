using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Application.Dtos;
using Application.Interfaces;
using Domain.Entities;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Gestor")]
// Los dos entran, pero hacen cosas distintas. La diferencia se
// controla dentro de cada metodo, no con el atributo.
public class UsuariosController : ControllerBase
{
    private readonly IRepositorioUsuarios _repositorio;

    public UsuariosController(IRepositorioUsuarios repositorio) => _repositorio = repositorio;

    private int UsuarioActualId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    private string RolActual => User.FindFirst(ClaimTypes.Role)!.Value;
    private bool SoyAdministrador => RolActual == "Administrador";

    // ─── LISTAR ────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var usuarios = await _repositorio.ListarAsync();

        // Para resolver "quien creo a quien" sin una consulta por fila.
        var nombres = usuarios.ToDictionary(u => u.Id, u => u.Username);
        // Un diccionario en memoria: busco el nombre del creador por su Id
        // sin volver a la base. Evita el problema N+1 del que hablamos
        // en la carga masiva.

        return Ok(usuarios.Select(u => new UsuarioListaDto
        {
            Id = u.Id,
            Username = u.Username,
            Correo = u.Correo,
            Rol = u.Rol.Nombre,
            RolId = u.RolId,
            Activo = u.Activo,
            Aprobado = u.Aprobado,
            FechaCreacion = u.FechaCreacion,
            CreadoPor = u.CreadoPorUsuarioId.HasValue && nombres.ContainsKey(u.CreadoPorUsuarioId.Value)
                        ? nombres[u.CreadoPorUsuarioId.Value]
                        : null,
            BloqueadoHasta = u.EstaBloqueado(DateTime.UtcNow) ? u.BloqueadoHasta : null,
            // Si el bloqueo ya venció, mando null. Así la pantalla no
            // muestra "bloqueado hasta las 10:15" cuando ya son las 11.
            // La fecha vencida sigue en la base, pero ya no significa nada.
        }));
    }

    // ─── CREAR ─────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Crear(CrearUsuarioDto dto)
    {
        // Capa 2 (formato) ya la valido [ApiController] antes de entrar aqui.

        // ═══ CAPA 3: "El nombre de usuario no debe estar duplicado" ═══
        if (await _repositorio.ExisteUsernameAsync(dto.Username))
            return Conflict(new { mensaje = $"El usuario '{dto.Username}' ya existe" });

        if (!await _repositorio.ExisteRolAsync(dto.RolId))
            return NotFound(new { mensaje = "El rol no existe" });

        // ═══════════════════════════════════════════════════════
        //   LA REGLA CENTRAL DEL ENUNCIADO
        // ═══════════════════════════════════════════════════════
        //
        // "El usuario administrador puede crear usuarios cajeros y gestores"
        // "El usuario gestor puede agregar usuarios cajeros y gestores PERO
        //  un usuario administrador debe aprobar el usuario creado"
        //
        // Los dos pueden crear. Lo que cambia es si nace aprobado.

        bool naceAprobado = SoyAdministrador;

        var usuario = new Usuario
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            // La contraseña se hashea AQUI y nunca se guarda en claro.
            // El dto.Password vive unos milisegundos en memoria y se va.
            Correo = dto.Correo,
            RolId = dto.RolId,
            Activo = true,
            Aprobado = naceAprobado,
            CreadoPorUsuarioId = UsuarioActualId,
            AprobadoPorUsuarioId = naceAprobado ? UsuarioActualId : null
        };

        await _repositorio.CrearAsync(usuario);

        return Ok(new
        {
            usuario.Id,
            mensaje = naceAprobado
                ? $"Usuario '{usuario.Username}' creado y activo"
                : $"Usuario '{usuario.Username}' creado. Debe ser aprobado por un administrador antes de poder entrar."
        });
    }

    // ─── APROBAR ───────────────────────────────────────────────
    [HttpPost("{id}/aprobar")]
    [Authorize(Roles = "Administrador")]
    // Este SI lleva su propio atributo: aprobar es exclusivo del admin.
    // Si un gestor pudiera aprobar, la regla entera no serviria de nada:
    // crearia y se auto-aprobaria.
    public async Task<IActionResult> Aprobar(int id)
    {
        var usuario = await _repositorio.ObtenerConRolAsync(id);
        if (usuario is null)
            return NotFound(new { mensaje = "El usuario no existe" });

        if (usuario.Aprobado)
            return Conflict(new { mensaje = "Ese usuario ya está aprobado" });

        usuario.Aprobado = true;
        usuario.AprobadoPorUsuarioId = UsuarioActualId;
        await _repositorio.GuardarAsync();

        return Ok(new { mensaje = $"Usuario '{usuario.Username}' aprobado" });
    }

    // ─── ACTUALIZAR ────────────────────────────────────────────
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Actualizar(int id, ActualizarUsuarioDto dto)
    {
        var usuario = await _repositorio.ObtenerConRolAsync(id);
        if (usuario is null)
            return NotFound(new { mensaje = "El usuario no existe" });

        if (usuario.RolId == 1)
            return Conflict(new { mensaje = "No se pueden modificar usuarios administradores desde aquí" });

        if (!await _repositorio.ExisteRolAsync(dto.RolId))
            return NotFound(new { mensaje = "El rol no existe" });

        usuario.Correo = dto.Correo;
        usuario.RolId = dto.RolId;
        usuario.Activo = dto.Activo;
        await _repositorio.GuardarAsync();

        return Ok(new { mensaje = "Usuario actualizado" });
    }

    // ─── ACTIVAR / DESACTIVAR ──────────────────────────────────
    [HttpPost("{id}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var usuario = await _repositorio.ObtenerConRolAsync(id);
        if (usuario is null)
            return NotFound(new { mensaje = "El usuario no existe" });

        if (usuario.Id == UsuarioActualId)
            return Conflict(new { mensaje = "No puedes desactivarte a ti mismo" });
        // Sin esto, un admin distraido se desactiva, el token le sigue
        // sirviendo 60 minutos, y al expirar no puede volver a entrar.

        if (usuario.RolId == 1 && usuario.Activo)
        {
            int admins = await _repositorio.ContarAdministradoresActivosAsync();
            if (admins <= 1)
                return Conflict(new { mensaje = "No puedes desactivar al único administrador activo" });
        }
        // ═══ REGLA DE NEGOCIO QUE EL ENUNCIADO NO PIDE ═══
        //
        // La agrego porque sin ella el sistema se puede dejar inservible:
        // cero administradores significa que nadie puede aprobar usuarios
        // nunca mas, y no hay forma de arreglarlo desde la aplicacion.
        //
        // Este tipo de regla se llama "salvaguarda": no la pide nadie
        // hasta que alguien la rompe.

        usuario.Activo = !usuario.Activo;
        await _repositorio.GuardarAsync();

        return Ok(new
        {
            mensaje = $"Usuario '{usuario.Username}' {(usuario.Activo ? "activado" : "desactivado")}"
        });
    }

    // ─── CARGA MASIVA ──────────────────────────────────────────
    [HttpPost("carga-masiva")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CargaMasiva(
        IFormFile archivo,
        [FromServices] ILectorUsuariosArchivo lector)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { mensaje = "Debes adjuntar un archivo" });

        if (archivo.Length > 5 * 1024 * 1024)
            return BadRequest(new { mensaje = "El archivo no puede superar 5 MB" });

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".csv")
            return BadRequest(new { mensaje = "Solo se aceptan archivos .xlsx o .csv" });

        List<(int fila, CrearUsuarioDto dto)> filas;
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

        var repetidosEnArchivo = filas
            .GroupBy(f => f.dto.Username.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        foreach (var (numeroFila, dto) in filas)
        {
            var erroresFila = new List<string>();

            var contexto = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
            var resultados = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            System.ComponentModel.DataAnnotations.Validator
                .TryValidateObject(dto, contexto, resultados, validateAllProperties: true);
            erroresFila.AddRange(resultados.Select(r => r.ErrorMessage ?? "Dato inválido"));
            // Las MISMAS reglas del formulario, reutilizadas.
            // Una sola fuente de verdad para las validaciones.

            if (await _repositorio.ExisteUsernameAsync(dto.Username))
                erroresFila.Add("Ese nombre de usuario ya está registrado");

            if (repetidosEnArchivo.Contains(dto.Username.ToLowerInvariant()))
                erroresFila.Add("Ese nombre de usuario está repetido dentro del archivo");

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

        foreach (var (_, dto) in filas)
        {
            await _repositorio.CrearAsync(new Usuario
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Correo = dto.Correo,
                RolId = dto.RolId,
                Activo = true,
                Aprobado = true,
                CreadoPorUsuarioId = UsuarioActualId,
                AprobadoPorUsuarioId = UsuarioActualId
            });
        }
        // Aqui inserto uno por uno y no con AddRange, a diferencia de los
        // clientes. La razon: hashear con BCrypt es LENTO a proposito
        // (unos 100 ms por contraseña, para frenar ataques de fuerza bruta).
        // Con 500 usuarios eso serian 50 segundos. Es el limite real de
        // este endpoint, y esta bien conocerlo: para volumenes grandes
        // habria que procesarlo en segundo plano.

        return Ok(new ResultadoCargaDto
        {
            Exitoso = true,
            FilasLeidas = filas.Count,
            FilasImportadas = filas.Count,
            Mensaje = $"{filas.Count} usuario(s) importado(s) correctamente"
        });
    }

    // ─── DESBLOQUEAR ───────────────────────────────────────────
    [HttpPost("{id}/desbloquear")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Desbloquear(int id)
    {
        var usuario = await _repositorio.ObtenerConRolAsync(id);
        if (usuario is null)
            return NotFound(new { mensaje = "El usuario no existe" });

        if (!usuario.EstaBloqueado(DateTime.UtcNow))
            return Conflict(new { mensaje = "Ese usuario no está bloqueado" });

        usuario.Desbloquear();
        // La misma regla de la entidad. El controlador no sabe qué
        // campos hay que limpiar: le pide a la entidad que se desbloquee.

        await _repositorio.GuardarAsync();

        return Ok(new { mensaje = $"Usuario '{usuario.Username}' desbloqueado" });
    }
}