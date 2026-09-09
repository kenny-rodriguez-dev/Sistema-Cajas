namespace Application.Dtos;

// Lo que SALE del login.
public class RespuestaLoginDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    // Mando el rol y el username aparte, aunque también vayan dentro
    // del token, para que el frontend pueda pintar "Hola, Kenny (Cajero)"
    // sin tener que abrir el token.
    //
    // Y OJO: eso es solo para MOSTRAR. Las decisiones de permisos las toma
    // siempre el backend leyendo el token. Nunca confíes en que el frontend
    // diga "soy administrador".
}