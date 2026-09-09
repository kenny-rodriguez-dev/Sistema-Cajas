namespace Domain.Entities;

// ═══ TABLA INTERMEDIA (muchos a muchos) ═══
//
// Una opción de menú la pueden ver varios roles.
// Un rol ve varias opciones de menú.
//
// Cuando ambos lados son "muchos", NO se puede resolver con una llave
// foránea: no existe dónde ponerla. Se necesita una tabla en el medio,
// donde cada fila dice "este rol ve esta opción".
//
// Es EXACTAMENTE el patrón de la tabla "usercash" del diagrama de
// Viamatica: la que conecta usuarios con cajas. Cuando lleguemos a la
// rebanada 4, ya vas a conocer el molde.
public class OpcionMenuRol
{
    public int OpcionMenuId { get; set; }
    public OpcionMenu OpcionMenu { get; set; } = null!;

    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;

    // Fíjate que NO hay una propiedad "Id".
    // La llave primaria va a ser la COMBINACIÓN de las dos columnas,
    // y la configuro en el AppDbContext.
    //
    // Eso hace imposible por diseño que la misma pareja se repita:
    // no se puede decir dos veces "el Cajero ve Clientes".
    // En el diagrama original eso se marca con "PF": llave primaria
    // Y foránea al mismo tiempo.
}