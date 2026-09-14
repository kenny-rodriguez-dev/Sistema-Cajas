# ═══════════════════════════════════════════════════════════
#   ETAPA 1: COMPILAR
# ═══════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
# El SDK trae todo lo necesario para compilar: pesa mas de 1 GB.

WORKDIR /src

# Copio SOLO los archivos de proyecto primero.
COPY Domain/Domain.csproj                 Domain/
COPY Application/Application.csproj       Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY Backend/Backend.csproj               Backend/

RUN dotnet restore Backend/Backend.csproj
# Docker guarda en cache cada linea del Dockerfile. Si una linea no
# cambio, reutiliza el resultado anterior.
# Los .csproj cambian poco; tu codigo cambia todo el tiempo. Al
# restaurar ANTES de copiar el codigo, la descarga de paquetes queda
# cacheada y los siguientes builds se la saltan.
# Copiando todo de una vez, cada cambio de una linea de C# obligaria
# a redescargar todos los paquetes: de 10 segundos a 3 minutos.

# Ahora si, todo el codigo.
COPY Domain/         Domain/
COPY Application/    Application/
COPY Infrastructure/ Infrastructure/
COPY Backend/        Backend/

RUN dotnet publish Backend/Backend.csproj -c Release -o /app/publish
# -c Release = compilacion optimizada, sin informacion de depuracion.

# ═══════════════════════════════════════════════════════════
#   ETAPA 2: EJECUTAR
# ═══════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# Esta imagen es el RUNTIME, no el SDK: sabe ejecutar aplicaciones
# .NET pero no sabe compilarlas. Pesa unos 200 MB en vez de 1 GB.
# Arranco de cero y me traigo SOLO el resultado compilado: el codigo
# fuente, el compilador y los paquetes NuGet se quedan en la etapa 1.
# Dos beneficios: la imagen pesa cinco veces menos, y tu codigo
# fuente no viaja dentro de lo que despliegas.

WORKDIR /app

COPY --from=compilacion /app/publish .
# "--from" copia desde otra etapa, no desde tu disco.

EXPOSE 8080
# Documenta que puerto usa. No lo publica: eso lo hace el compose.

ENV ASPNETCORE_URLS=http://+:8080
# Por defecto .NET escucha en localhost, que DENTRO de un contenedor
# significa "solo yo mismo": nadie de afuera podria conectarse.
# El "+" significa "todas las interfaces de red". Sin esta linea el
# contenedor arranca bien y no le responde a nadie. Error clasico.

ENTRYPOINT ["dotnet", "Backend.dll"]