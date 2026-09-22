// Este es el de DESARROLLO (el que usa ng serve).
// Angular sustituye un archivo por el otro AL COMPILAR, según la
// configuración. Es el equivalente de tu .env.local de Next.js:
// la URL nunca va escrita dentro de un componente.
export const environment = {
    produccion: false,
    apiUrl: 'http://localhost:5219',   // ← TU puerto del backend local
};