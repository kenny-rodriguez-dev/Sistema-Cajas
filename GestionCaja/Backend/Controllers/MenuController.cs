using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Data;
using Backend.Dtos;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]   // -> /api/menu
[Authorize]
public class MenuController : ControllerBase
{
    private readonly AppDbContext _contexto;

    public MenuController(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerMenu()
    {
        var nombreRol = User.FindFirst(ClaimTypes.Role)?.Value;
        // ═══ AQUÍ ESTÁ LO IMPORTANTE ═══
        //
        // El rol lo saco DEL TOKEN, no de un parámetro de la petición.
        //
        // Si el frontend me mandara "?rol=Administrador", cualquiera podría
        // cambiar esa palabra en la URL y ver el menú completo. El token
        // no se puede modificar sin la clave secreta: ya lo comprobaste
        // rompiéndolo a mano en localStorage.
        //
        // Regla que se repite en todo sistema con autenticación:
        // la identidad viene del token, jamás de lo que diga el cliente.

        if (string.IsNullOrEmpty(nombreRol))
            return Forbid();

        var opciones = await _contexto.OpcionesMenuRoles
            .Where(x => x.Rol.Nombre == nombreRol && x.OpcionMenu.Activo)
            // Filtro por el nombre del rol atravesando la relación.
            // EF traduce esto a un JOIN entre las tres tablas.
            .OrderBy(x => x.OpcionMenu.Orden)
            .Select(x => new OpcionMenuDto
            {
                Etiqueta = x.OpcionMenu.Etiqueta,
                Ruta = x.OpcionMenu.Ruta
            })
            .ToListAsync();

        return Ok(opciones);
    }
}