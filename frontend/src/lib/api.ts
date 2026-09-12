import { leerSesion, borrarSesion } from './sesion';

export async function apiFetch(
    ruta: string,
    opciones: RequestInit = {}
): Promise<Response> {
    const sesion = leerSesion();

    const cabeceras = new Headers(opciones.headers);

    const esFormData = opciones.body instanceof FormData;

    if (!esFormData) {
        cabeceras.set('Content-Type', 'application/json');
    }
    // ═══ POR QUÉ ESTE "IF" ES OBLIGATORIO ═══
    //
    // Cuando mandas un FormData, el Content-Type no es solo
    // "multipart/form-data": tiene que llevar pegado un BOUNDARY,
    // un texto aleatorio que separa las partes del formulario:
    //
    //     multipart/form-data; boundary=----WebKitFormBoundaryX7aK9
    //
    // Ese boundary lo genera el navegador, y solo él lo conoce.
    // Si yo escribo el Content-Type a mano, se lo piso y el servidor
    // recibe un formulario sin saber dónde empieza y termina cada parte.
    // Resultado: 400 y un error que parece de otra cosa.
    //
    // La regla: con FormData, NO toques el Content-Type. Déjaselo al
    // navegador. Es exactamente lo mismo que te dije de Postman.

    if (sesion) {
        cabeceras.set('Authorization', `Bearer ${sesion.token}`);
    }

    const respuesta = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}${ruta}`,
        { ...opciones, headers: cabeceras }
    );

    if (respuesta.status === 401 && typeof window !== 'undefined') {
        borrarSesion();
        window.location.href = '/login';
    }

    return respuesta;
}
