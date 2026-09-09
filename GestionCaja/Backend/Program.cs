using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Application.Interfaces;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ─── MITAD 1: REGISTRO ──────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseSqlServer(
        builder.Configuration.GetConnectionString("BaseDeDatos")
    )
);

builder.Services.AddScoped<ServicioDeTokens>();
// Registro mi servicio para poder pedirlo en el constructor del controlador.
// Sin esta línea, .NET no sabría fabricarlo y la app reventaría al arrancar
// con "Unable to resolve service".
//
// Scoped = uno nuevo por cada petición HTTP.

// ─── AUTENTICACIÓN ───
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            // Estas son las preguntas que .NET le hará a CADA token que llegue.
            ValidateIssuerSigningKey = true,   // ¿la firma es mía?
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Clave"]!)
            ),
            ValidateIssuer = true,             // ¿lo emití yo?
            ValidIssuer = builder.Configuration["Jwt:Emisor"],
            ValidateAudience = true,           // ¿es para mi frontend?
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            ValidateLifetime = true,           // ¿no ha expirado?
            ClockSkew = TimeSpan.Zero
            // Por defecto .NET perdona 5 minutos de desfase de reloj.
            // Para desarrollo lo pongo en cero para que la expiración
            // sea exacta y puedas probarla sin esperar de más.
        };
    });

builder.Services.AddAuthorization();

const string PoliticaCors = "PermitirFrontend";

builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy(PoliticaCors, politica =>
    {
        politica
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

builder.Services.AddScoped<IRepositorioClientes, RepositorioClientes>();
// SE LEE ASÍ: "cuando alguien pida un IRepositorioClientes,
//              entrégale un RepositorioClientes".
//
// ESTA LÍNEA ES EL PEGAMENTO DE TODA LA ARQUITECTURA.
// Es el único lugar de todo el proyecto donde la interfaz y su
// implementación se tocan. Cambia esta sola línea y todo el sistema
// pasa a usar otra base de datos, sin tocar nada más.

// ─── SIEMBRA ─────────────────────────────────────────────────

using (var alcance = app.Services.CreateScope())
{
    var contexto = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
    await SembradorDeDatos.SembrarAsync(contexto);
}

// ─── MITAD 2: LA TUBERÍA ────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();
// ═══ ESTE ORDEN NO SE TOCA. Es lo que te advertí hace tres mensajes. ═══
//
// CORS primero: el preflight OPTIONS viaja SIN token. Si Authentication
// fuera antes, lo cortaría con un 401, la respuesta nunca pasaría por CORS,
// y el navegador te reportaría... un error de CORS. Buscarías el problema
// en el lugar equivocado durante horas.
//
// Authentication antes que Authorization, por razones obvias:
// primero "¿quién eres?", después "¿puedes hacer esto?".

app.MapControllers();

app.Run();