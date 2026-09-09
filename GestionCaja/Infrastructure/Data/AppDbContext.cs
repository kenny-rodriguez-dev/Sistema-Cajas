using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opciones) : base(opciones)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<OpcionMenu> OpcionesMenu => Set<OpcionMenu>();
    public DbSet<OpcionMenuRol> OpcionesMenuRoles => Set<OpcionMenuRol>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        // ─── CLIENTE ───
        modelo.Entity<Cliente>(entidad =>
        {
            entidad.Property(c => c.Identificacion).HasMaxLength(13).IsRequired();
            entidad.HasIndex(c => c.Identificacion).IsUnique();
            entidad.Property(c => c.Nombres).HasMaxLength(50).IsRequired();
            entidad.Property(c => c.Apellidos).HasMaxLength(50).IsRequired();
            entidad.Property(c => c.Correo).HasMaxLength(120).IsRequired();
            entidad.Property(c => c.Telefono).HasMaxLength(13).IsRequired();
            entidad.Property(c => c.Direccion).HasMaxLength(100).IsRequired();
            entidad.Property(c => c.ReferenciaDireccion).HasMaxLength(100).IsRequired();
        });

        // ─── ROL ───
        modelo.Entity<Rol>(entidad =>
        {
            entidad.Property(r => r.Nombre).HasMaxLength(50).IsRequired();
            entidad.HasIndex(r => r.Nombre).IsUnique();

            entidad.HasData(
                new Rol { Id = 1, Nombre = "Administrador" },
                new Rol { Id = 2, Nombre = "Gestor" },
                new Rol { Id = 3, Nombre = "Cajero" }
            );
        });

        // ─── USUARIO ───
        modelo.Entity<Usuario>(entidad =>
        {
            entidad.Property(u => u.Username).HasMaxLength(20).IsRequired();
            entidad.HasIndex(u => u.Username).IsUnique();
            entidad.Property(u => u.PasswordHash).HasMaxLength(100).IsRequired();
            entidad.Property(u => u.Correo).HasMaxLength(120).IsRequired();

            entidad.HasOne(u => u.Rol)
                   .WithMany(r => r.Usuarios)
                   .HasForeignKey(u => u.RolId)
                   .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── OPCIÓN DE MENÚ ───
        modelo.Entity<OpcionMenu>(entidad =>
        {
            entidad.Property(o => o.Etiqueta).HasMaxLength(50).IsRequired();
            entidad.Property(o => o.Ruta).HasMaxLength(100).IsRequired();

            entidad.HasData(
                new OpcionMenu { Id = 1, Etiqueta = "Bienvenida", Ruta = "/bienvenida", Orden = 1, Activo = true },
                new OpcionMenu { Id = 2, Etiqueta = "Dashboard", Ruta = "/dashboard", Orden = 2, Activo = true },
                new OpcionMenu { Id = 3, Etiqueta = "Usuarios", Ruta = "/usuarios", Orden = 3, Activo = true },
                new OpcionMenu { Id = 4, Etiqueta = "Asignación de turnos", Ruta = "/turnos", Orden = 4, Activo = true },
                new OpcionMenu { Id = 5, Etiqueta = "Procesos de caja", Ruta = "/caja", Orden = 5, Activo = true },
                new OpcionMenu { Id = 6, Etiqueta = "Clientes", Ruta = "/clientes/nuevo", Orden = 6, Activo = true }
            );
            // Varias de esas rutas todavía no existen en el frontend.
            // Está bien: el menú es el mapa del proyecto completo, y las
            // pantallas se van llenando en las rebanadas siguientes.
        });

        // ─── LA TABLA INTERMEDIA ───
        modelo.Entity<OpcionMenuRol>(entidad =>
        {
            entidad.HasKey(x => new { x.OpcionMenuId, x.RolId });
            // ═══ LLAVE PRIMARIA COMPUESTA ═══
            // La llave no es una columna: son DOS juntas.
            // Eso es lo que hace imposible repetir la misma pareja.

            entidad.HasOne(x => x.OpcionMenu)
                   .WithMany(o => o.Roles)
                   .HasForeignKey(x => x.OpcionMenuId);

            entidad.HasOne(x => x.Rol)
                   .WithMany(r => r.OpcionesMenu)
                   .HasForeignKey(x => x.RolId);
            // Dos relaciones "uno a muchos" que, juntas, forman
            // el "muchos a muchos". Así se arma siempre.

            entidad.HasData(
                // Roles:  1 = Administrador,  2 = Gestor,  3 = Cajero

                // Bienvenida la ven todos
                new { OpcionMenuId = 1, RolId = 1 },
                new { OpcionMenuId = 1, RolId = 2 },
                new { OpcionMenuId = 1, RolId = 3 },

                // Dashboard y Usuarios: SOLO administrador
                new { OpcionMenuId = 2, RolId = 1 },
                new { OpcionMenuId = 3, RolId = 1 },

                // Asignación de turnos: gestor
                new { OpcionMenuId = 4, RolId = 2 },

                // Procesos de caja y Clientes: cajero
                new { OpcionMenuId = 5, RolId = 3 },
                new { OpcionMenuId = 6, RolId = 3 }
            );
            // Esas ocho líneas SON los permisos del sistema, y viven
            // en la base de datos. Cambiar quién ve qué es un UPDATE,
            // no un cambio de código.
        });
    }
}