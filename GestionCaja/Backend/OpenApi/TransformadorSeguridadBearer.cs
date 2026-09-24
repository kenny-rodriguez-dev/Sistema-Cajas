using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
// En .NET 10 es "Microsoft.OpenApi" a secas.
// En .NET 9 era "Microsoft.OpenApi.Models": si copias un tutorial
// viejo, ese using es lo primero que falla.

namespace Backend.OpenApi;

// Un "transformador de documento": código que modifica el documento
// OpenAPI después de que .NET lo genera. Aquí le agrega la información
// de que la API usa tokens Bearer.
internal sealed class TransformadorSeguridadBearer(
    IAuthenticationSchemeProvider proveedorEsquemas) : IOpenApiDocumentTransformer
// Ese paréntesis después del nombre es un CONSTRUCTOR PRIMARIO (C# 12).
// Es la forma corta de pedir una dependencia: equivale a declarar el
// campo, el constructor y la asignación, todo en una línea.
{
    public async Task TransformAsync(
        OpenApiDocument documento,
        OpenApiDocumentTransformerContext contexto,
        CancellationToken cancelacion)
    {
        var esquemas = await proveedorEsquemas.GetAllSchemesAsync();

        if (!esquemas.Any(e => e.Name == "Bearer"))
            return;
        // Solo actúa si la autenticación JWT está registrada.
        // "Bearer" es el nombre que le puso AddJwtBearer en Program.cs.

        // ─── 1. Declarar el esquema de seguridad ───
        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "Pega aquí el token que devuelve POST /api/auth/login"
            }
        };
        // Esto es lo que hace aparecer el botón "Authorize" en Swagger.

        // ─── 2. Pedirlo en cada endpoint ───
        foreach (var operacion in documento.Paths.Values.SelectMany(ruta => ruta.Operations))
        {
            operacion.Value.Security ??= [];
            operacion.Value.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", documento)] = []
            });
        }
        // Sin este paso, el botón aparece pero Swagger NO manda el token:
        // solo lo adjunta a los endpoints que declaran necesitarlo.
        //
        // Efecto secundario: el login también muestra el candado. No pasa
        // nada, el login no lo lee. Quitarlo exigiría revisar endpoint por
        // endpoint cuáles son anónimos, y no vale la complejidad.
    }
}