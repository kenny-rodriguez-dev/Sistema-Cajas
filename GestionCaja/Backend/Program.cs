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
// LÍNEA NUEVA. Aquí se juntan las tres cosas del paso anterior:
//
//   AddDbContext<AppDbContext>  ->  "guarda mi puente en la caja"
//   UseSqlServer                ->  "y que hable SQL Server"
//   GetConnectionString(...)    ->  "la dirección está en appsettings.json"
//
// Esto es lo que le entrega las "opciones" al constructor del AppDbContext.
// A partir de ahora, cualquier controlador puede pedir un AppDbContext
// y .NET se lo fabrica solo, ya conectado.

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

// ─── MITAD 2: LA TUBERÍA ────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(PoliticaCors);

app.MapControllers();

app.Run();