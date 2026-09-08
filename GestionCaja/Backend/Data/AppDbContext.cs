using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opciones) : base(opciones)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    // Dos tablas nuevas. Cada tabla del proyecto es una línea aquí.

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        // ─── CLIENTE (igual que antes) ───
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
            // HasData mete estas filas DENTRO DE LA MIGRACIÓN.
            // Cuando corras Update-Database, se insertan solas.
            //
            // Solo sirve para datos fijos de catálogo, que nunca cambian
            // y cuyo valor puedo escribir literalmente aquí.
            // Fíjate que les asigno el Id a mano: tiene que ser siempre
            // el mismo en todas las máquinas, o mi código se rompería.
        });

        // ─── USUARIO ───
        modelo.Entity<Usuario>(entidad =>
        {
            entidad.Property(u => u.Username).HasMaxLength(20).IsRequired();

            entidad.HasIndex(u => u.Username).IsUnique();
            // "El nombre de usuario no debe estar duplicado."
            // Capa 4: que lo garantice la base misma.

            entidad.Property(u => u.PasswordHash).HasMaxLength(100).IsRequired();
            // 100 caracteres porque un hash de BCrypt siempre mide 60.
            // Nunca guardas la contraseña, así que su largo real da igual.

            entidad.Property(u => u.Correo).HasMaxLength(120).IsRequired();

            entidad.HasOne(u => u.Rol)              // un usuario tiene UN rol
                   .WithMany(r => r.Usuarios)       // un rol tiene MUCHOS usuarios
                   .HasForeignKey(u => u.RolId)     // conectados por esta columna
                   .OnDelete(DeleteBehavior.Restrict);
            // Esas cuatro líneas son la relación, dicha en voz alta.
            // Aquí EF la habría adivinado sola, pero escribirla explícita
            // es mejor: quien lea el código ve la intención sin adivinar.
            //
            // Restrict = "no dejes borrar un rol si tiene usuarios".
            // La alternativa (Cascade) borraría los usuarios junto con el rol.
            // Eso es exactamente el desastre que quieres evitar.
        });
    }
}