namespace Domain.Entities;

public class Servicio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public int VelocidadMbps { get; set; }
    // El enunciado pide velocidad y el diagrama original NO la tenía.
    // Es una de las cosas que detectamos al leerlo críticamente.
    //
    // Y no es un campo decorativo: es lo que decide si un cambio es
    // "de mayor o menor velocidad".

    public decimal Precio { get; set; }
    // decimal, NUNCA double o float, para dinero.
    //
    // double guarda los números en binario y no puede representar 0.1
    // exactamente. Sumas mil veces 0.1 y te da 99.99999999. En un sistema
    // de caja eso significa descuadre.
    //
    // decimal guarda en base 10, como lo escribes. Es más lento y ocupa
    // más, y no importa: la exactitud del dinero no se negocia.
    // Esta es pregunta de entrevista clásica.

    public bool Activo { get; set; } = true;

    public ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
}