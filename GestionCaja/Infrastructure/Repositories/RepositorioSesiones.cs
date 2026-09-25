using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositorioSesiones : IRepositorioSesiones
{
    private readonly AppDbContext _contexto;

    public RepositorioSesiones(AppDbContext contexto) => _contexto = contexto;

    public async Task CrearAsync(SesionUsuario sesion)
    {
        _contexto.SesionesUsuario.Add(sesion);
        await _contexto.SaveChangesAsync();
    }

    public async Task<bool> EstaActivaAsync(string jti, DateTime ahora) =>
        await _contexto.SesionesUsuario.AnyAsync(s =>
            s.Jti == jti && s.FechaCierre == null && s.FechaExpiracion > ahora);
    // ═══ AQUÍ HAY UNA TENSIÓN QUE CONVIENE CONOCER ═══
    //
    // La regla "activa = sin cerrar y sin vencer" ya vive en la entidad,
    // en EstaActiva(). ¿Por qué la repito aquí?
    //
    // Porque EF traduce esta consulta a SQL, y no puede traducir un
    // método de C# que escribiste tú: no sabe qué hace por dentro.
    // Si escribiera .AnyAsync(s => s.EstaActiva(ahora)), fallaría al
    // ejecutarse. Así que la condición se escribe en términos que EF
    // sí entiende.
    //
    // Es una tensión real entre el modelo de dominio rico y las consultas
    // a la base. La solución avanzada es el "patrón Specification": la
    // regla se escribe una sola vez como una expresión que sirve tanto
    // para C# como para SQL. Para este proyecto, repetirla es aceptable.
    //
    // Y fíjate que es AnyAsync: solo pregunta "¿existe?", no trae la fila.
    // Esta consulta corre en CADA petición: tiene que ser lo más barata posible.

    public async Task<SesionUsuario?> ObtenerPorJtiAsync(string jti) =>
        await _contexto.SesionesUsuario.FirstOrDefaultAsync(s => s.Jti == jti);

    public async Task<int> CerrarTodasDeUsuarioAsync(int usuarioId, DateTime ahora, string motivo)
    {
        var activas = await _contexto.SesionesUsuario
            .Where(s => s.UsuarioId == usuarioId && s.FechaCierre == null && s.FechaExpiracion > ahora)
            .ToListAsync();

        foreach (var sesion in activas)
            sesion.Cerrar(ahora, motivo);
        // Aquí SÍ uso el método de la entidad: los objetos ya están en
        // memoria, así que es C# normal, no una consulta a traducir.

        return activas.Count;
        // ═══ OJO: ESTE MÉTODO NO GUARDA ═══
        // Solo marca las sesiones. Quien lo llama hace el SaveChanges.
        //
        // ¿Por qué? Porque siempre se usa JUNTO con otro cambio: desactivar
        // a un usuario, cambiarle el rol, restablecer su contraseña. Y esos
        // dos cambios tienen que guardarse JUNTOS.
        //
        // Funciona porque todos los repositorios comparten el mismo
        // DbContext durante una petición (están registrados como Scoped).
        // Un solo SaveChanges guarda todo lo que ese DbContext viene
        // siguiendo, en una sola transacción.
        //
        // Eso tiene nombre: UNIT OF WORK, unidad de trabajo. El DbContext
        // de EF ya es una, sin que tengas que programarla.
    }

    public async Task<Dictionary<int, int>> ContarActivasPorUsuarioAsync(DateTime ahora) =>
        await _contexto.SesionesUsuario
            .Where(s => s.FechaCierre == null && s.FechaExpiracion > ahora)
            .GroupBy(s => s.UsuarioId)
            .Select(g => new { UsuarioId = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.UsuarioId, x => x.Cantidad);
    // Un solo viaje a la base para todos los usuarios, no uno por fila.
    // Evita el problema N+1 en la lista de usuarios.

    public async Task GuardarAsync() => await _contexto.SaveChangesAsync();
}