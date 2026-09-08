using Microsoft.AspNetCore.Mvc;
// "using" = traer herramientas de otra librería, para no escribir el nombre largo.

namespace Backend.Controllers;
// El "apellido" de la clase. Sirve para organizar y evitar nombres repetidos.

[ApiController]
// Le dice a .NET: "esto es un controlador de API".
// Regalo importante: si un dato de entrada no cumple las validaciones,
// .NET responde 400 automáticamente sin que yo escriba una línea.

[Route("api/[controller]")]
// La URL base. [controller] se reemplaza por el nombre de la clase sin "Controller".
// PingController  ->  /api/ping

public class PingController : ControllerBase
// Los dos puntos = "hereda de". ControllerBase me regala Ok(), NotFound(), BadRequest().
{
    [HttpGet]
    // Este método responde a peticiones GET hacia /api/ping
    public IActionResult Get()
    // IActionResult = "voy a devolver una respuesta HTTP", sin decir de qué tipo exacto.
    {
        return Ok(new
        {
            mensaje = "El backend está vivo",
            hora = DateTime.UtcNow
        });
        // Ok(...) = HTTP 200 + el objeto convertido a JSON automáticamente.
        // UtcNow y no Now: se guarda siempre en hora universal y se convierte al mostrar.
    }
}
