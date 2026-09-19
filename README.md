# Sistema de Gestión de Caja

API REST y aplicación web para la gestión de caja de una empresa de servicios de internet: usuarios con roles y aprobación, clientes, cajas y turnos, y contratos con historial.

**Stack:** .NET 10 / C# · Next.js 15 / React / TypeScript · SQL Server · Docker

---

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 22 LTS](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- SQL Server LocalDB (viene con Visual Studio) — solo para desarrollo local

---

## Arranque rápido

```bash
git clone <url-del-repositorio>
cd proyecto-caja
docker compose up --build
```

Abre **http://localhost:3000**.

La base de datos se crea sola: la aplicación aplica sus migraciones al arrancar y siembra los usuarios de prueba.

---

## Usuarios de prueba

| Usuario | Contraseña | Rol | Para qué sirve |
|---|---|---|---|
| `admin0001` | `Admin1234` | Administrador | Dashboard, aprobar usuarios, carga masiva |
| `gestor0001` | `Gestor1234` | Gestor | Asignar cajeros, generar turnos, crear usuarios |
| `cajero0001` | `Cajero1234` | Cajero | Clientes, contratos, operar caja, atender turnos |
| `cajero0002` | `Cajero1234` | Cajero | Comparte la Caja 1 — para probar la exclusión |
| `cajero0003` | `Cajero1234` | Cajero | Sin caja asignada |
| `pendiente0001` | `Pendiente1234` | Cajero | Sin aprobar — su login debe fallar |

---

## Los cuatro escenarios de despliegue

El backend y el frontend son dos procesos independientes que se comunican por HTTP. Docker no cambia eso: solo cambia la dirección a la que se llaman. Por eso **ninguna URL está escrita en el código** — todas vienen de variables de entorno.

| Escenario | Comando | Frontend llama al backend en | Backend llama a la base en |
|---|---|---|---|
| Todo local | F5 en Visual Studio + `npm run dev` | `localhost:5266` | `(localdb)\MSSQLLocalDB` |
| Solo backend en Docker | `docker compose -f docker-compose.solo-backend.yml up` | `localhost:5000` | `sqlserver,1433` |
| Solo frontend en Docker | `docker compose -f docker-compose.solo-frontend.yml up` | `localhost:5266` | `(localdb)\MSSQLLocalDB` |
| Todo en Docker | `docker compose up` | `localhost:5000` | `sqlserver,1433` |

Las tres reglas que explican la tabla:

1. **Contenedor a contenedor** → por nombre de servicio (`sqlserver`, `backend`)
2. **Cualquier cosa fuera de Docker hacia un contenedor** → `localhost` + puerto publicado
3. **El navegador siempre está fuera**, aunque la página venga de un contenedor

> Si un proceso *dentro* de un contenedor necesita alcanzar algo que corre en la máquina anfitriona, usa `host.docker.internal`.

### Comandos útiles

```bash
docker compose up -d            # en segundo plano
docker compose logs -f backend  # ver logs de un servicio
docker compose down             # apagar (los datos sobreviven)
docker compose down -v          # apagar y BORRAR la base de datos
```

---

## Desarrollo local

**Backend** — crea `Backend/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "BaseDeDatos": "Server=(localdb)\\MSSQLLocalDB;Database=GestionCaja;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Clave": "clave-de-desarrollo-de-al-menos-32-caracteres-no-usar-en-produccion"
  }
}
```

Este archivo está en `.gitignore`: contiene secretos y no se versiona.

**Frontend** — copia `frontend/.env.example` como `frontend/.env.local` y ajusta la URL.

**Migraciones** (Consola del Administrador de paquetes, proyecto predeterminado `Infrastructure`):

```powershell
Add-Migration NombreDeLaMigracion -StartupProject Backend
Update-Database -StartupProject Backend
```

---

## Estructura

```
proyecto-caja/
├── GestionCaja/                 Solución .NET (arquitectura por capas)
│   ├── Domain/                  Entidades. No depende de nada
│   ├── Application/             DTOs e interfaces. Depende de Domain
│   ├── Infrastructure/          EF Core, repositorios, migraciones
│   └── Backend/                 Controladores, middleware, Program.cs
├── frontend/                    Next.js (App Router)
│   └── src/
│       ├── app/(privado)/       Pantallas protegidas con layout y sidebar
│       └── lib/                 Interceptor HTTP y manejo de sesión
├── docker-compose.yml
├── docker-compose.solo-backend.yml
├── docker-compose.solo-frontend.yml
└── GestionCaja.postman_collection.json
```

**Regla de dependencias:** cada capa solo puede depender de las de adentro. `Domain` no conoce a nadie; `Application` conoce a `Domain`; `Infrastructure` a las dos; `Backend` a todas. Esto no es una convención: está impuesto por las referencias entre proyectos, así que el compilador lo hace cumplir.

Cuando `Application` necesita algo de `Infrastructure`, declara una interfaz que `Infrastructure` implementa. La capa interna define el contrato; la externa lo cumple.

---

## Seguridad

- Contraseñas y tokens de recuperación con **BCrypt** (hash con salt, nunca en claro)
- **JWT** firmado con HS256; el rol viaja en los claims y lo verifica el backend en cada petición
- **Autorización por rol** con `[Authorize(Roles = "...")]` — el menú oculta opciones, pero es el backend el que prohíbe
- **Tokens de recuperación** de un solo uso, 256 bits de entropía criptográfica, 15 minutos de vida
- **Mensajes genéricos** en login y recuperación, para no permitir enumeración de usuarios
- **Consultas parametrizadas** en todo el SQL directo, contra inyección
- Secretos fuera del código, en variables de entorno

---

## Validación en cuatro capas

| Capa | Dónde | Ejemplo |
|---|---|---|
| 1. Formulario | React | Campos obligatorios antes de enviar |
| 2. DTO | `[ApiController]` + DataAnnotations | Formato, longitud, expresiones regulares |
| 3. Negocio | Servicios y controladores | "La identificación no puede repetirse" |
| 4. Base de datos | Índices únicos, CHECK, FK | Índice único filtrado sobre sesiones abiertas |

La capa 1 es comodidad para el usuario; la 2 es seguridad real. Las capas 3 y 4 cubren reglas que no se pueden validar mirando un dato aislado.

---

## Stored procedures

Ambos viven en migraciones, versionados con el resto del código.

| Procedimiento | Para qué | Por qué no EF Core |
|---|---|---|
| `sp_SiguienteNumeroTurno` | Genera el correlativo de los turnos (`AC0001`) | Leer y sumar debe ser atómico; un `SELECT` seguido de un `INSERT` permite que dos peticiones simultáneas obtengan el mismo número |
| `sp_IndicadoresDashboard` | Los indicadores del dashboard | Seis agregaciones en un solo viaje a la base, con tres conjuntos de resultados |

---

## API

Documentación OpenAPI en `/openapi/v1.json` con el backend corriendo.

La colección de Postman (`GestionCaja.postman_collection.json`) tiene 38 peticiones organizadas por área, cada una con una descripción de qué demuestra y qué respuesta esperar. El login guarda el token automáticamente.

---

## Limitaciones conocidas

- **El correo de recuperación no se envía**: el token se genera, se guarda y se valida igual que en producción, pero el enlace se imprime en la consola del backend en vez de enviarse. Falta conectar un proveedor SMTP.
- **`MigrateAsync()` al arrancar** es adecuado para desarrollo y demos, no para producción con varias instancias: allí las migraciones se aplican en un paso separado del despliegue.
- **El parser de CSV es básico**: no maneja campos con punto y coma dentro de comillas. Para producción correspondería una librería como CsvHelper.
- **El token se guarda en `localStorage`**, accesible desde JavaScript. Una cookie `httpOnly` sería más resistente a XSS, a cambio de más complejidad entre dominios.
- **Sin pruebas automatizadas.** La lógica de contratos y la generación de turnos son las primeras candidatas.
