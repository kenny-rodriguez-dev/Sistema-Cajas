using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

// El DbContext es el PUENTE entre tus objetos de C# y la base de datos.
// Es quien abre la conexión, traduce a SQL, y va anotando qué cambió
// para saber qué guardar cuando le digas.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opciones) : base(opciones)
    {
    }
    // Esto es un CONSTRUCTOR: el método que corre al crear el objeto.
    //
    // Fíjate que no dice en ningún lado a qué base se conecta. Recibe las
    // opciones desde afuera y se las pasa a su clase padre (": base(...)").
    //
    // Eso es INYECCIÓN DE DEPENDENCIAS: la clase no busca lo que necesita,
    // se lo entregan. En el Paso 5 verás quién se lo entrega.

    public DbSet<Cliente> Clientes => Set<Cliente>();
    // Un DbSet ES una tabla, vista desde C#.
    // "Clientes" será el nombre de la tabla; Cliente el molde de cada fila.
    //
    // Cada tabla nueva del proyecto va a ser una línea más aquí.

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        // Aquí afino cómo se construye la tabla, más allá de las convenciones.
        // Esto es la CAPA 4 de validación: las reglas dentro de la base misma.

        modelo.Entity<Cliente>(entidad =>
        {
            entidad.Property(c => c.Identificacion)
                   .HasMaxLength(13)   // el máximo que pide el enunciado
                   .IsRequired();      // en SQL: NOT NULL

            entidad.HasIndex(c => c.Identificacion).IsUnique();
            // ESTA LÍNEA ES IMPORTANTE.
            // "Los clientes solo pueden registrarse una sola vez y se valida
            // por su número de identificación."
            //
            // Un índice único hace que SQL Server RECHACE una cédula repetida,
            // aunque el intento venga por fuera de tu API.
            // Es la última red de seguridad de la que hablamos.

            entidad.Property(c => c.Nombres).HasMaxLength(50).IsRequired();
            entidad.Property(c => c.Apellidos).HasMaxLength(50).IsRequired();
            entidad.Property(c => c.Correo).HasMaxLength(120).IsRequired();
            entidad.Property(c => c.Telefono).HasMaxLength(13).IsRequired();
            entidad.Property(c => c.Direccion).HasMaxLength(100).IsRequired();
            entidad.Property(c => c.ReferenciaDireccion).HasMaxLength(100).IsRequired();
            // Sin HasMaxLength, EF crea cada texto como NVARCHAR(MAX),
            // que es enorme y no se puede indexar bien. Siempre pon el límite.
        });
    }
}