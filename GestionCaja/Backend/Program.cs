using Microsoft.EntityFrameworkCore;
using Backend.Data;

var builder = WebApplication.CreateBuilder(args);

// ─── MITAD 1: REGISTRO ──────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseSqlServer(
        builder.Configuration.GetConnectionString("BaseDeDatos")
    )
);

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

// ─── SIEMBRA (corre UNA vez al arrancar, no por petición) ────

using (var alcance = app.Services.CreateScope())
{
    var contexto = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
    await SembradorDeDatos.SembrarAsync(contexto);
}
// ¿Por qué este "CreateScope" tan raro?
//
// El DbContext se registró como SCOPED: .NET fabrica uno nuevo por cada
// petición HTTP y lo destruye al terminar. Así dos peticiones simultáneas
// no se pisan los datos.
//
// Pero aquí NO estamos dentro de una petición: estamos arrancando.
// No existe ningún "alcance" al que pedirle un DbContext.
// Entonces creo uno a mano, saco mi DbContext, y el "using" se encarga
// de cerrarlo y liberar la conexión al terminar el bloque.
//
// Esto va DESPUÉS de builder.Build() porque necesita la app ya construida,
// y ANTES de app.Run() porque Run() se queda bloqueado escuchando.

// ─── MITAD 2: LA TUBERÍA ────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(PoliticaCors);

app.MapControllers();

app.Run();