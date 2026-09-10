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
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<AsignacionCaja> AsignacionesCaja => Set<AsignacionCaja>();
    public DbSet<SesionCaja> SesionesCaja => Set<SesionCaja>();
    public DbSet<TipoAtencion> TiposAtencion => Set<TipoAtencion>();
    public DbSet<ContadorTurno> ContadoresTurno => Set<ContadorTurno>();
    public DbSet<Turno> Turnos => Set<Turno>();

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
            entidad.HasOne(u => u.Rol).WithMany(r => r.Usuarios)
                   .HasForeignKey(u => u.RolId).OnDelete(DeleteBehavior.Restrict);
        });

        // ─── MENÚ ───
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
        });

        modelo.Entity<OpcionMenuRol>(entidad =>
        {
            entidad.HasKey(x => new { x.OpcionMenuId, x.RolId });
            entidad.HasOne(x => x.OpcionMenu).WithMany(o => o.Roles).HasForeignKey(x => x.OpcionMenuId);
            entidad.HasOne(x => x.Rol).WithMany(r => r.OpcionesMenu).HasForeignKey(x => x.RolId);
            entidad.HasData(
                new { OpcionMenuId = 1, RolId = 1 }, new { OpcionMenuId = 1, RolId = 2 },
                new { OpcionMenuId = 1, RolId = 3 }, new { OpcionMenuId = 2, RolId = 1 },
                new { OpcionMenuId = 3, RolId = 1 }, new { OpcionMenuId = 4, RolId = 2 },
                new { OpcionMenuId = 5, RolId = 3 }, new { OpcionMenuId = 6, RolId = 3 }
            );
        });

        // ─── CAJA ───
        modelo.Entity<Caja>(entidad =>
        {
            entidad.Property(c => c.Nombre).HasMaxLength(50).IsRequired();
            entidad.HasData(
                new Caja { Id = 1, Nombre = "Caja 1", Activo = true },
                new Caja { Id = 2, Nombre = "Caja 2", Activo = true }
            );
        });

        modelo.Entity<AsignacionCaja>(entidad =>
        {
            entidad.HasKey(x => new { x.CajaId, x.UsuarioId });
            entidad.HasOne(x => x.Caja).WithMany(c => c.Asignaciones).HasForeignKey(x => x.CajaId);
            entidad.HasOne(x => x.Usuario).WithMany(u => u.Asignaciones).HasForeignKey(x => x.UsuarioId);
        });

        modelo.Entity<SesionCaja>(entidad =>
        {
            entidad.HasOne(s => s.Caja).WithMany(c => c.Sesiones).HasForeignKey(s => s.CajaId);
            entidad.HasOne(s => s.Usuario).WithMany(u => u.Sesiones).HasForeignKey(s => s.UsuarioId);
            entidad.HasIndex(s => s.CajaId).IsUnique().HasFilter("[FechaCierre] IS NULL");
            entidad.HasIndex(s => s.UsuarioId).IsUnique().HasFilter("[FechaCierre] IS NULL");
        });

        // ─── TIPO DE ATENCIÓN ───
        modelo.Entity<TipoAtencion>(entidad =>
        {
            entidad.Property(t => t.Codigo).HasMaxLength(2).IsRequired().IsFixedLength();
            // IsFixedLength = CHAR(2) en vez de VARCHAR(2).
            // Cuando el largo SIEMPRE es el mismo, CHAR es más eficiente.

            entidad.HasIndex(t => t.Codigo).IsUnique();
            entidad.Property(t => t.Descripcion).HasMaxLength(100).IsRequired();

            entidad.HasData(
                new TipoAtencion { Id = 1, Codigo = "AC", Descripcion = "Atención al cliente", Activo = true },
                new TipoAtencion { Id = 2, Codigo = "PS", Descripcion = "Pago de servicio", Activo = true }
            );
            // Son los dos ejemplos literales del enunciado.
        });

        // ─── CONTADOR ───
        modelo.Entity<ContadorTurno>(entidad =>
        {
            entidad.HasKey(x => new { x.TipoAtencionId, x.Fecha });
            // Llave compuesta: un contador por tipo y por día.
            // Así "AC" y "PS" numeran independiente, y ambos
            // se reinician mañana.

            entidad.HasOne(x => x.TipoAtencion).WithMany().HasForeignKey(x => x.TipoAtencionId);
        });

        // ─── TURNO ───
        modelo.Entity<Turno>(entidad =>
        {
            entidad.Property(t => t.Codigo).HasMaxLength(6).IsRequired().IsFixedLength();
            // Exactamente 6: "AC0001".

            entidad.HasIndex(t => new { t.Codigo, t.Fecha }).IsUnique();
            // Red de seguridad: aunque el stored procedure falle,
            // no puede haber dos turnos con el mismo código el mismo día.

            entidad.Property(t => t.Estado).HasMaxLength(3).IsRequired().IsFixedLength();

            entidad.ToTable(t => t.HasCheckConstraint(
                "CK_Turno_Estado",
                "[Estado] IN ('PEN','ATE','CAN')"
            ));
            // RESTRICCIÓN CHECK: la base rechaza cualquier otro valor.
            // Es lo que reemplaza a la tabla de catálogo: garantiza los
            // valores válidos sin necesidad de un JOIN.

            entidad.HasOne(t => t.TipoAtencion).WithMany(ta => ta.Turnos)
                   .HasForeignKey(t => t.TipoAtencionId).OnDelete(DeleteBehavior.Restrict);

            entidad.HasOne(t => t.Caja).WithMany(c => c.Turnos)
                   .HasForeignKey(t => t.CajaId).OnDelete(DeleteBehavior.Restrict);

            entidad.HasOne(t => t.Cliente).WithMany()
                   .HasForeignKey(t => t.ClienteId).OnDelete(DeleteBehavior.Restrict);

            entidad.HasOne(t => t.AtendidoPorUsuario).WithMany()
                   .HasForeignKey(t => t.AtendidoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);

            entidad.HasOne(t => t.AsignadoPorUsuario).WithMany()
                   .HasForeignKey(t => t.AsignadoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
            // Tres relaciones distintas hacia Usuario desde la misma tabla.
            // Por eso hay que escribirlas explícitas: EF no puede adivinar
            // cuál llave foránea corresponde a cuál navegación.
        });
    }
}