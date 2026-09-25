using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Backend.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ╔═══════════════════════════════════════════════════════════╗
// ║  MITAD 1: REGISTRO                                        ║
// ╚═══════════════════════════════════════════════════════════╝

builder.Services.AddControllers();

builder.Services.AddOpenApi(opciones =>
{
    opciones.AddDocumentTransformer((documento, contexto, cancelacion) =>
    {
        documento.Info = new()
        {
            Title = "Gestión de Caja API",
            Version = "v1",
            Description = "API para la gestión de caja de una empresa de servicios de internet."
        };
        return Task.CompletedTask;
        // Un transformador en línea: pone título y descripción al documento.
    });

    opciones.AddDocumentTransformer<TransformadorSeguridadBearer>();
    // El nuestro, con el esquema de seguridad.
});

builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("BaseDeDatos")));

// ─── Servicios ───
builder.Services.AddScoped<ServicioDeTokens>();
builder.Services.AddScoped<IServicioRecuperacion, ServicioRecuperacion>();
builder.Services.AddScoped<ILectorClientesArchivo, LectorClientesExcel>();
builder.Services.AddScoped<ILectorUsuariosArchivo, LectorUsuariosExcel>();

// ─── Repositorios: uno por área del negocio ───
builder.Services.AddScoped<IRepositorioClientes, RepositorioClientes>();
builder.Services.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
builder.Services.AddScoped<IRepositorioCajas, RepositorioCajas>();
builder.Services.AddScoped<IRepositorioTurnos, RepositorioTurnos>();
builder.Services.AddScoped<IRepositorioContratos, RepositorioContratos>();
builder.Services.AddScoped<IRepositorioPagos, RepositorioPagos>();
builder.Services.AddScoped<IRepositorioDashboard, RepositorioDashboard>();
builder.Services.AddScoped<IRepositorioRecuperacion, RepositorioRecuperacion>();
builder.Services.AddScoped<IRepositorioSesiones, RepositorioSesiones>();
// Cada línea: "cuando alguien pida la interfaz, entrégale esta clase".
// Es el único lugar del sistema donde contrato e implementación se tocan.

// ─── Autenticación ───
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Clave"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Emisor"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        opciones.Events = new JwtBearerEvents
        {
            OnTokenValidated = async contexto =>
            {
                // ═══ SE EJECUTA DESPUÉS DE QUE LA FIRMA SEA VÁLIDA ═══
                // Hasta aquí .NET ya comprobó que el token es auténtico
                // y no venció. Esto agrega UNA pregunta más: ¿la sesión
                // sigue viva en la base de datos?
                //
                // Es el guardia del concierto que por fin tiene una lista
                // de a quién echaron.

                var jti = contexto.Principal?.FindFirst("jti")?.Value;

                if (jti is null)
                {
                    contexto.Fail("El token no tiene identificador de sesión");
                    return;
                }

                var sesiones = contexto.HttpContext.RequestServices
                    .GetRequiredService<IRepositorioSesiones>();
                // Aquí no hay constructor donde pedir la dependencia:
                // es una función suelta. Así que se le pide al proveedor
                // de servicios de la petición actual. Devuelve el mismo
                // repositorio (y el mismo DbContext) que usará el controlador.

                if (!await sesiones.EstaActivaAsync(jti, DateTime.UtcNow))
                    contexto.Fail("La sesión fue cerrada");
                // Fail = la autenticación falla, aunque la firma sea perfecta.
                // La petición recibe 401, y el interceptor de tus dos
                // frontends manda al usuario al login solo.
            }
        };
    });

builder.Services.AddAuthorization();

// ─── CORS ───
var origenesPermitidos = builder.Configuration
    .GetSection("Cors:OrigenesPermitidos")
    .Get<string[]>() ?? [];

const string PoliticaCors = "PermitirFrontend";

builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy(PoliticaCors, politica =>
    {
        politica
            .WithOrigins(origenesPermitidos)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ╔═══════════════════════════════════════════════════════════╗
// ║  LA FRONTERA                                              ║
// ╚═══════════════════════════════════════════════════════════╝

var app = builder.Build();

// ─── Migraciones y siembra ───
using (var alcance = app.Services.CreateScope())
{
    var contexto = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
    await contexto.Database.MigrateAsync();
    await SembradorDeDatos.SembrarAsync(contexto);
}

// ╔═══════════════════════════════════════════════════════════╗
// ║  MITAD 2: LA TUBERÍA                                      ║
// ╚═══════════════════════════════════════════════════════════╝

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // El documento en texto: /openapi/v1.json

    app.UseSwaggerUI(opciones =>
    {
        opciones.SwaggerEndpoint("/openapi/v1.json", "Gestión de Caja API v1");
        // Le dice a la pantalla de dónde leer el documento.

        opciones.DocumentTitle = "Gestión de Caja - API";

        opciones.EnablePersistAuthorization();
        // Guarda el token en el navegador: si recargas la página, no
        // tienes que volver a pegarlo.
    });
    // ═══ SOLO EN DESARROLLO, A PROPÓSITO ═══
    // Swagger es un mapa completo de tu API: cada endpoint, cada campo,
    // cada respuesta. En producción, publicarlo es regalarle a un
    // atacante el plano del edificio.
}

app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();