import { leerSesion, borrarSesion } from './sesion';

// Esto es un INTERCEPTOR, el que pide el enunciado de Viamatica.
//
// En Angular hay una clase HttpInterceptor que hace esto oficialmente.
// En React no existe tal cosa: lo construyes tú, envolviendo fetch.
// Distinta herramienta, misma idea: un solo lugar por donde pasan
// TODAS las peticiones, para no repetir la cabecera cien veces.

export async function apiFetch(
    ruta: string,
    opciones: RequestInit = {}
): Promise<Response> {
    const sesion = leerSesion();

    const cabeceras = new Headers(opciones.headers);
    // Headers es un objeto del navegador para manejar cabeceras.
    // Parto de las que me hayan pasado y les agrego las mías.

    cabeceras.set('Content-Type', 'application/json');

    if (sesion) {
        cabeceras.set('Authorization', `Bearer ${sesion.token}`);
        // ESTA LÍNEA ES EL CORAZÓN DEL INTERCEPTOR.
        //
        // A partir de ahora, ninguna pantalla vuelve a preocuparse por el
        // token. Se pega solo. Si mañana cambia el formato, cambio aquí y ya.
        //
        // Y ojo: esta cabecera es la que DISPARA EL PREFLIGHT.
        // La vas a ver aparecer en el experimento del final.
    }

    const respuesta = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}${ruta}`,
        { ...opciones, headers: cabeceras }
        // Copio todas las opciones que me pasaron y le piso las cabeceras
        // por las mías, que ya llevan el token.
    );

    if (respuesta.status === 401 && typeof window !== 'undefined') {
        borrarSesion();
        window.location.href = '/login';
        // MANEJO CENTRALIZADO DEL TOKEN VENCIDO.
        //
        // Un token expira a los 60 minutos. Cuando pase, CUALQUIER petición
        // va a devolver 401. En vez de manejar eso en cada pantalla, lo
        // resuelvo una vez aquí: borro la sesión muerta y mando al login.
        //
        // Esta es la otra mitad de por qué existe un interceptor: no solo
        // agrega cosas a la ida, también reacciona a la vuelta.
    }

    return respuesta;
}
