using System.ComponentModel.DataAnnotations;
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
public class UsuariosController : ControllerBase
{
    private readonly IRepositorioUsuarios _repositorio;
    private readonly IRepositorioSesiones _sesiones;

    public UsuariosController(IRepositorioUsuarios repositorio, IRepositorioSesiones sesiones)
    {
        _repositorio = repositorio;
        _sesiones = sesiones;
    }

    private int UsuarioActualId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    private string RolActual => User.FindFirst(ClaimTypes.Role)!.Value;
    private bool SoyAdministrador => RolActual == "Administrador";

    // ─── LISTAR ────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var ahora = DateTime.UtcNow;
        var usuarios = await _repositorio.ListarAsync();
        var nombres = usuarios.ToDictionary(u => u.Id, u => u.Username);
        var sesionesPorUsuario = await _sesiones.ContarActivasPorUsuarioAsync(ahora);
        // Dos consultas en total para toda la lista, no una por usuario.

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
            BloqueadoHasta = u.EstaBloqueado(ahora) ? u.BloqueadoHasta : null,
            SesionesActivas = sesionesPorUsuario.GetValueOrDefault(u.Id)
            // GetValueOrDefault: si el usuario no está en el diccionario
            // (no tiene sesiones), devuelve 0 en vez de reventar.
        }));
    }

    // ─── CREAR ─────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Crear(CrearUsuarioDto dto)
    {
        if (await _repositorio.ExisteUsernameAsync(dto.Username))
            return Conflict(new { mensaje = $"El usuario '{dto.Username}' ya existe" });

        if (!await _repositorio.ExisteRolAsync(dto.RolId))
            return NotFound(new { mensaje = "El rol no existe" });

        bool naceAprobado = SoyAdministrador;

        var usuario = new Usuario
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
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

        bool cambioElRol = usuario.RolId != dto.RolId;
        bool seDesactiva = usuario.Activo && !dto.Activo;
        // Guardo estas dos comparaciones ANTES de modificar el usuario:
        // después de asignar los valores nuevos ya no sabría si cambiaron.

        usuario.Correo = dto.Correo;
        usuario.RolId = dto.RolId;
        usuario.Activo = dto.Activo;

        int cerradas = 0;
        if (cambioElRol || seDesactiva)
        {
            cerradas = await _sesiones.CerrarTodasDeUsuarioAsync(
                usuario.Id, DateTime.UtcNow, cambioElRol ? "cambio de rol" : "desactivado");
        }
        // ═══ EL AGUJERO DEL CAMBIO DE ROL, CERRADO ═══
        // Su token dice el rol VIEJO. Si no lo echo, un gestor degradado
        // a cajero seguiría asignando turnos durante una hora. Al volver
        // a entrar, recibe un token nuevo con el rol correcto.

        await _repositorio.GuardarAsync();
        // Un solo guardado: los cambios del usuario y el cierre de sesiones
        // juntos. Mismo DbContext, misma transacción.

        return Ok(new
        {
            mensaje = cerradas > 0
                ? $"Usuario actualizado. Se cerraron {cerradas} sesión(es) activa(s)."
                : "Usuario actualizado"
        });
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

        if (usuario.RolId == 1 && usuario.Activo)
        {
            int admins = await _repositorio.ContarAdministradoresActivosAsync();
            if (admins <= 1)
                return Conflict(new { mensaje = "No puedes desactivar al único administrador activo" });
        }

        usuario.Activo = !usuario.Activo;

        int cerradas = 0;
        if (!usuario.Activo)
            cerradas = await _sesiones.CerrarTodasDeUsuarioAsync(usuario.Id, DateTime.UtcNow, "desactivado");
        // ═══ EL AGUJERO QUE COMPROBASTE AL PRINCIPIO, CERRADO ═══
        // Antes: desactivado en la base, pero con su token seguía entrando
        // una hora. Ahora sus sesiones mueren en el mismo instante, y su
        // próxima petición recibe 401.

        await _repositorio.GuardarAsync();

        var estado = usuario.Activo ? "activado" : "desactivado";
        return Ok(new
        {
            mensaje = cerradas > 0
                ? $"Usuario '{usuario.Username}' {estado}. Se cerraron {cerradas} sesión(es)."
                : $"Usuario '{usuario.Username}' {estado}"
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
        await _repositorio.GuardarAsync();

        return Ok(new { mensaje = $"Usuario '{usuario.Username}' desbloqueado" });
    }

    // ─── CERRAR SESIONES (NUEVO) ───────────────────────────────
    [HttpPost("{id}/cerrar-sesiones")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CerrarSesiones(int id)
    {
        var usuario = await _repositorio.ObtenerConRolAsync(id);
        if (usuario is null)
            return NotFound(new { mensaje = "El usuario no existe" });

        int cerradas = await _sesiones.CerrarTodasDeUsuarioAsync(
            usuario.Id, DateTime.UtcNow, "cerrada por administrador");

        if (cerradas == 0)
            return Conflict(new { mensaje = "Ese usuario no tiene sesiones activas" });

        await _sesiones.GuardarAsync();

        return Ok(new { mensaje = $"Se cerraron {cerradas} sesión(es) de '{usuario.Username}'" });
        // Para el caso "se me perdió el celular con la sesión abierta".
        // Con JWT puro no había forma de hacer esto.
    }

    // ─── CARGA MASIVA (sin cambios) ────────────────────────────
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

            var contexto = new ValidationContext(dto);
            var resultados = new List<ValidationResult>();
            Validator.TryValidateObject(dto, contexto, resultados, validateAllProperties: true);
            erroresFila.AddRange(resultados.Select(r => r.ErrorMessage ?? "Dato inválido"));

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

        return Ok(new ResultadoCargaDto
        {
            Exitoso = true,
            FilasLeidas = filas.Count,
            FilasImportadas = filas.Count,
            Mensaje = $"{filas.Count} usuario(s) importado(s) correctamente"
        });
    }
}