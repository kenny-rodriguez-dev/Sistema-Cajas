namespace Application.Dtos;

// Lo que ve CUALQUIER usuario al entrar
public class BienvenidaDto
{
    public string Username { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public DateTime MiembroDesde { get; set; }

    public int TurnosAtendidosHoy { get; set; }
    // Su significado CAMBIA según el rol:
    //   Cajero -> los que atendió él
    //   Gestor -> los que él asignó y ya fueron atendidos
    //   Admin  -> todos los del sistema
    // El enunciado lo pide así.

    public string DescripcionTurnos { get; set; } = string.Empty;
    // Explica en palabras qué significa ese número para este usuario.
    // Un número sin contexto confunde: "12" no dice si son míos o de todos.

    public int? UsuariosPendientesAprobacion { get; set; }
    // Solo para gestor y admin. Null = no aplica, y el frontend
    // ni siquiera dibuja la tarjeta.
}

// Solo para el administrador
public class DashboardDto
{
    public IndicadoresUsuariosDto Usuarios { get; set; } = new();
    public List<IndicadorCajaDto> Cajas { get; set; } = new();
    public int TurnosEmitidos { get; set; }
    public int TurnosAtendidos { get; set; }
    public int TurnosPendientes { get; set; }
    public int ContratosActivos { get; set; }
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
}

public class IndicadoresUsuariosDto
{
    public int Total { get; set; }
    public int Activos { get; set; }
    public int Inactivos { get; set; }
    public int PendientesAprobacion { get; set; }
    public int ConSesionAbierta { get; set; }
    public int Gestores { get; set; }
    public int Cajeros { get; set; }
    public int Bloqueados { get; set; }
}

public class IndicadorCajaDto
{
    public string Caja { get; set; } = string.Empty;
    public int CajerosAsignados { get; set; }
    public int TurnosAtendidos { get; set; }
    public string? OcupadaPor { get; set; }
}