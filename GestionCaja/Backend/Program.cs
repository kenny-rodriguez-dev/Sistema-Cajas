using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
// Los using ya reflejan las cuatro capas.
// Fíjate que Backend conoce a todas: es la capa de más afuera.

var builder = WebApplication.CreateBuilder(args);

// ╔═══════════════════════════════════════════════════════════╗
// ║  MITAD 1: REGISTRO                                        ║
// ║  TODO lo que empiece con builder.Services va AQUÍ.        ║
// ╚═══════════════════════════════════════════════════════════╝

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseSqlServer(
        builder.Configuration.GetConnectionString("BaseDeDatos")
    )
);

builder.Services.AddScoped<ServicioDeTokens>();

builder.Services.AddScoped<IRepositorioClientes, RepositorioClientes>();
// ↑ ESTA ES LA QUE ESTABA MAL UBICADA.
//   Tiene que estar aquí arriba, con las demás.

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Clave"]!)
            ),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Emisor"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
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

// ╔═══════════════════════════════════════════════════════════╗
// ║  ══════════════  LA FRONTERA  ══════════════              ║
// ║  Aquí la caja de herramientas se CIERRA.                  ║
// ║  Cualquier builder.Services debajo de esta línea          ║
// ║  revienta con "collection is read-only".                  ║
// ╚═══════════════════════════════════════════════════════════╝

builder.Services.AddScoped<IRepositorioCajas, RepositorioCajas>();
builder.Services.AddScoped<IRepositorioTurnos, RepositorioTurnos>();
var app = builder.Build();

// ─── SIEMBRA ─────────────────────────────────────────────────

using (var alcance = app.Services.CreateScope())
{
    var contexto = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
    await SembradorDeDatos.SembrarAsync(contexto);
}
// Fíjate que aquí NO registro nada: solo PIDO algo que ya estaba
// registrado. Pedir sí se puede después de la frontera; registrar no.

// ╔═══════════════════════════════════════════════════════════╗
// ║  MITAD 2: LA TUBERÍA                                      ║
// ║  TODO lo que empiece con app. va AQUÍ.                    ║
// ╚═══════════════════════════════════════════════════════════╝

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();