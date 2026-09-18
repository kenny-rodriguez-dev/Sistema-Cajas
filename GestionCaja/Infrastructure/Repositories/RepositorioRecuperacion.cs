using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositorioRecuperacion : IRepositorioRecuperacion
{
    private readonly AppDbContext _contexto;

    public RepositorioRecuperacion(AppDbContext contexto) => _contexto = contexto;

    public async Task<Usuario?> BuscarPorCorreoAsync(string correo) =>
        await _contexto.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Correo == correo && u.Activo && u.Aprobado);
    // Solo usuarios activos y aprobados. Uno pendiente de aprobación
    // no debería poder recuperar una cuenta que todavía no puede usar.

    public async Task InvalidarTokensDeUsuarioAsync(int usuarioId)
    {
        var vigentes = await _contexto.TokensRecuperacion
            .Where(t => t.UsuarioId == usuarioId && t.FechaUso == null)
            .ToListAsync();

        foreach (var token in vigentes)
            token.FechaUso = DateTime.UtcNow;

        await _contexto.SaveChangesAsync();
        // Al pedir un token nuevo, los anteriores se queman.
        // Sin esto, alguien podría acumular cinco tokens válidos
        // y usarlos más adelante. Solo uno vive a la vez.
    }

    public async Task CrearTokenAsync(TokenRecuperacion token)
    {
        _contexto.TokensRecuperacion.Add(token);
        await _contexto.SaveChangesAsync();
    }

    public async Task<List<TokenRecuperacion>> ObtenerTokensVigentesAsync() =>
        await _contexto.TokensRecuperacion
            .Include(t => t.Usuario)
            .Where(t => t.FechaUso == null && t.FechaExpiracion > DateTime.UtcNow)
            .ToListAsync();
    // ═══ POR QUÉ TRAIGO TODOS Y NO BUSCO EL MÍO ═══
    //
    // Sería natural escribir: Where(t => t.TokenHash == miHash).
    // No se puede, y aquí está el motivo:
    //
    // BCrypt genera un salt distinto cada vez, así que el mismo token
    // produce un hash diferente en cada ejecución. Ya lo comprobaste
    // en la rebanada 2A con los dos hashes distintos de "Admin1234".
    //
    // No puedo buscar por igualdad. Tengo que recorrer los vigentes
    // y probar Verify() contra cada uno.
    //
    // ¿Es ineficiente? Un poco. Pero los tokens vigentes en cualquier
    // momento son unos pocos: duran 15 minutos y cada usuario tiene
    // uno solo. En un sistema con millones de usuarios se usaría
    // SHA-256 en vez de BCrypt, que sí permite buscar por igualdad
    // porque no lleva salt. Es una decisión de escala.

    public async Task GuardarAsync() => await _contexto.SaveChangesAsync();
}