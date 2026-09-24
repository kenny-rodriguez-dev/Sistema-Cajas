# Sistema de Gestión de Caja

Aplicación fullstack para la gestión de caja de una empresa de servicios de internet: usuarios con roles y flujo de aprobación, clientes, asignación de cajeros a cajas, turnos de atención, contratos con historial de cambios y cobro de mensualidades.

Proyecto de práctica propio, basado en el enunciado de una prueba técnica. **Un mismo backend sirve a dos frontends distintos**, uno en Next.js y otro en Angular, para demostrar que la API no depende del cliente que la consume.

**Stack:** .NET 10 / C# · Entity Framework Core · SQL Server · Next.js / React · Angular 22 · Docker

---

## Arranque rápido

```bash
git clone <url-del-repositorio>
cd proyecto-caja
docker compose up --build
```

| Qué | Dónde |
|---|---|
| Frontend Next.js | http://localhost:3000 |
| Frontend Angular | http://localhost:4200 |
| API | http://localhost:5000 |
| Documentación Swagger | http://localhost:5000/swagger |

La base de datos se crea sola: al arrancar, la aplicación aplica sus migraciones y siembra los usuarios de prueba.

> Si tu equipo tiene poca memoria, construye las imágenes de una en una (`docker compose build backend`, luego `frontend`, luego `frontend-angular`) y después `docker compose up -d`. Construirlas en paralelo puede agotar la memoria de Docker.

---

## Usuarios de prueba

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin0001` | `Admin1234` | Administrador |
| `gestor0001` | `Gestor1234` | Gestor |
| `cajero0001` | `Cajero1234` | Cajero |
| `cajero0002` | `Cajero1234` | Cajero |
| `cajero0003` | `Cajero1234` | Cajero |
| `pendiente0001` | `Pendiente1234` | Cajero sin aprobar: su login es rechazado |

---

## Qué hace cada rol

**Administrador**
- Dashboard con indicadores de usuarios, cajas y turnos, filtrable por fechas
- Crea usuarios (nacen aprobados), los edita, activa, desactiva y desbloquea
- Aprueba los usuarios que crean los gestores
- Carga masiva de usuarios desde Excel o CSV

**Gestor**
- Asigna cajeros a cajas (máximo dos por caja)
- Genera turnos por caja y tipo de atención (`AC0001`, `PS0001`) y los cancela
- Crea usuarios, que quedan pendientes de aprobación

**Cajero**
- Abre y cierra su sesión de caja; atiende turnos
- Mantenimiento de clientes: alta, edición, activación, búsqueda por identificación y carga masiva
- Atención al cliente: contratar un servicio, cobrar mensualidades, cambiar de plan, cambiar la forma de pago y cancelar el contrato

Todos los roles: login con recuperación de contraseña y pantalla de bienvenida con indicadores propios. El menú lateral se carga desde la base de datos según el rol.

---

## Los cuatro escenarios de despliegue

Backend y frontend son procesos independientes que se hablan por HTTP. Docker no cambia eso: solo cambia la dirección a la que se llaman. Por eso **ninguna URL está escrita en el código**: todas vienen de configuración.

| Escenario | Comando | El frontend llama al backend en | El backend llama a la base en |
|---|---|---|---|
| Todo local | F5 en Visual Studio + `npm run dev` / `ng serve` | `localhost:5266` | `(localdb)\MSSQLLocalDB` |
| Solo backend en Docker | `docker compose -f docker-compose.solo-backend.yml up` | `localhost:5000` | `sqlserver,1433` |
| Solo frontend en Docker | `docker compose -f docker-compose.solo-frontend.yml up` | `localhost:5266` | `(localdb)\MSSQLLocalDB` |
| Todo en Docker | `docker compose up` | `localhost:5000` | `sqlserver,1433` |

Las reglas que explican la tabla:

1. **De contenedor a contenedor** → por nombre de servicio (`sqlserver`, `backend`)
2. **De fuera de Docker hacia un contenedor** → `localhost` + puerto publicado
3. **De dentro de un contenedor hacia la máquina anfitriona** → `host.docker.internal`
4. **El navegador siempre está fuera de Docker**, aunque la página venga de un contenedor

Antes de cambiar de un archivo compose a otro, baja el anterior (`docker compose -f <archivo> down`): comparten nombres de contenedor.

---

## Desarrollo local

**Backend** — crea `GestionCaja/Backend/appsettings.Development.json` (está en `.gitignore`: contiene secretos):

```json
{
  "ConnectionStrings": {
    "BaseDeDatos": "Server=(localdb)\\MSSQLLocalDB;Database=GestionCaja;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Clave": "clave-de-desarrollo-de-al-menos-32-caracteres-no-usar-en-produccion"
  },
  "FrontendUrl": "http://localhost:3000"
}
```

`FrontendUrl` decide a qué frontend apuntan los enlaces de recuperación de contraseña. Los orígenes permitidos por CORS son una lista aparte, en `Cors:OrigenesPermitidos` de `appsettings.json`.

**Frontend Next.js** — copia `frontend/.env.example` como `frontend/.env.local`.

**Frontend Angular** — la URL del backend está en `frontend-angular/src/environments/`.

**Migraciones** (Consola del Administrador de paquetes, proyecto predeterminado `Infrastructure`):

```powershell
Add-Migration NombreDeLaMigracion -StartupProject Backend
Update-Database -StartupProject Backend
```

Una migración que ya se aplicó **no se edita nunca**: cualquier cambio va en una migración nueva.

---

## Estructura

```
proyecto-caja/
├── GestionCaja/                 Solución .NET, arquitectura por capas
│   ├── Domain/                  Entidades. No depende de nada
│   ├── Application/             DTOs e interfaces. Depende de Domain
│   ├── Infrastructure/          EF Core, repositorios, migraciones
│   └── Backend/                 Controladores, middleware, Program.cs
├── frontend/                    Next.js (App Router)
├── frontend-angular/            Angular 22 (standalone, signals, zoneless)
├── docker-compose.yml
├── docker-compose.solo-backend.yml
├── docker-compose.solo-frontend.yml
├── stored-procedures.sql
└── GestionCaja.postman_collection.json
```

**Regla de dependencias:** cada capa solo depende de las de adentro. No es una convención: lo imponen las referencias entre proyectos, así que el compilador lo hace cumplir. Cuando `Application` necesita algo de `Infrastructure`, declara una interfaz que `Infrastructure` implementa.

### Dos frontends, un backend

| | Next.js | Angular |
|---|---|---|
| Puerto | 3000 | 4200 |
| Interceptor | Función propia que envuelve `fetch` | `HttpInterceptorFn` |
| Protección de rutas | Guardia en el layout | `CanActivateFn` |
| Formularios | Estado manual | Reactive Forms |
| Estado | `useState` | Signals y `computed` |
| Imagen Docker | Node (servidor vivo) | nginx (solo archivos estáticos) |

El backend no cambia en nada entre uno y otro.

---

## Seguridad

- **Contraseñas y tokens de recuperación** con BCrypt: hash con salt, nunca en claro
- **JWT** firmado con HS256. El rol viaja en los claims y el backend lo verifica en cada petición
- **Autorización por rol** con `[Authorize(Roles = "...")]`. El menú oculta opciones, pero quien prohíbe es el backend
- **Bloqueo por intentos fallidos:** cinco contraseñas equivocadas seguidas bloquean la cuenta 15 minutos. El bloqueo se revisa antes que la contraseña
- **Recuperación de contraseña** con tokens de un solo uso, 256 bits de entropía criptográfica y 15 minutos de vida. Restablecer la contraseña también desbloquea la cuenta
- **Mensajes genéricos** en login y recuperación, para no permitir enumerar cuentas
- **Montos calculados en el servidor:** el cliente indica qué mes paga, nunca cuánto
- **Consultas parametrizadas** en todo el SQL directo
- **Secretos fuera del código**, en variables de entorno
- **Swagger solo en desarrollo**, para no publicar el mapa de la API en producción

---

## Validación en cuatro capas

| Capa | Dónde | Ejemplo |
|---|---|---|
| 1. Formulario | Navegador | Campos obligatorios antes de enviar |
| 2. DTO | `[ApiController]` + DataAnnotations | Formato, longitud, expresiones regulares |
| 3. Negocio | Controladores y repositorios | "Esa identificación ya está registrada" |
| 4. Base de datos | Índices únicos, CHECK, FK | Índice único filtrado sobre sesiones abiertas |

La capa 1 es comodidad para el usuario; la 2 es seguridad real. Las capas 3 y 4 cubren reglas que no se pueden validar mirando un dato aislado.

---

## Reglas de negocio destacadas

- **Un contrato nunca se edita, se sustituye.** Cambiar de plan marca el contrato como sustituido y crea uno nuevo que conserva la fecha de fin, dentro de una transacción
- **Un cliente, un contrato activo:** índice único filtrado sobre los estados vigentes
- **Una caja, un cajero operando a la vez:** índice único filtrado sobre las sesiones abiertas
- **Un mes, un pago por cliente:** índice único sobre (cliente, periodo)
- **Cada pago guarda el monto y la forma de pago del día en que se cobró**, no un enlace al valor actual
- **Los turnos no reutilizan números** aunque se cancelen: el contador solo sube

---

## Stored procedures

Los dos están en `stored-procedures.sql` y se crean con las migraciones.

| Procedimiento | Para qué |
|---|---|
| `sp_SiguienteNumeroTurno` | El correlativo de los turnos. Leer y sumar debe ser una sola operación atómica |
| `sp_IndicadoresDashboard` | Todos los indicadores del dashboard en un solo viaje a la base, con tres conjuntos de resultados |

---

## Documentación de la API

- **Swagger:** `http://localhost:5000/swagger` (o el puerto local del backend). Usa el botón *Authorize* con el token que devuelve el login.
- **Postman:** `GestionCaja.postman_collection.json`, con 62 peticiones organizadas por área. Cada una describe qué demuestra y qué respuesta esperar. El login guarda el token automáticamente.

---

## Limitaciones conocidas

- **El correo de recuperación no se envía.** El token se genera, se guarda y se valida igual que en producción; el enlace se imprime en la consola del backend. Falta conectar un proveedor SMTP.
- **El mensaje de bloqueo revela que la cuenta existe.** Es un equilibrio consciente: sin él, un usuario bloqueado que escribe su contraseña correcta recibiría "usuario o contraseña incorrectos" sin entender por qué. La solución completa es limitar intentos por nombre de usuario escrito y por IP, existan o no.
- **El contador de intentos fallidos no es atómico.** Dos intentos simultáneos pueden contarse como uno. Un `UPDATE` atómico, como el del procedimiento de turnos, lo resolvería.
- **"Sesión abierta" en el dashboard cuenta sesiones de caja, no de login.** Con JWT el servidor no guarda quién está conectado: el token vive en el navegador. Saberlo exigiría registrar las sesiones del lado del servidor.
- **`MigrateAsync()` al arrancar** es adecuado para desarrollo y demostraciones, no para producción con varias instancias: allí las migraciones se aplican en un paso separado del despliegue.
- **El token se guarda en `localStorage`**, accesible desde JavaScript. Una cookie `httpOnly` sería más resistente a XSS, a cambio de más complejidad entre dominios.
- **El lector de CSV es básico:** no maneja campos que contengan punto y coma entre comillas. Para producción correspondería CsvHelper.
- **La carga masiva de usuarios no escala:** BCrypt tarda unos 100 ms por contraseña, a propósito. Quinientos usuarios serían cincuenta segundos. Para volúmenes grandes habría que procesarla en segundo plano.
- **No hay pruebas automatizadas.** Las primeras candidatas son la sustitución de contratos, la generación de turnos y el bloqueo por intentos. La entidad `Usuario` ya está preparada: sus métodos reciben la hora como parámetro, así que se pueden probar sin esperar el tiempo real.
