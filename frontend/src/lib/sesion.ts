// Este archivo es lo ÚNICO que sabe dónde vive el token.
// Si mañana cambio de localStorage a cookies, solo toco este archivo.

export type Sesion = {
    token: string;
    username: string;
    rol: string;
};

const CLAVE = 'sesion';

export function guardarSesion(sesion: Sesion): void {
    if (typeof window === 'undefined') return;
    // GUARDIA IMPORTANTE. Next.js ejecuta código en DOS lugares:
    // en el servidor y en el navegador. "localStorage" solo existe en
    // el navegador; en el servidor la variable "window" ni siquiera está
    // definida, y sin esta línea la aplicación reventaría al renderizar.
    //
    // Es el mismo concepto de la pregunta 2 de tu sustentación:
    // el mismo código corre en dos lugares distintos.

    localStorage.setItem(CLAVE, JSON.stringify(sesion));
    // localStorage solo guarda texto, por eso el JSON.stringify.
}

export function leerSesion(): Sesion | null {
    if (typeof window === 'undefined') return null;

    const texto = localStorage.getItem(CLAVE);
    if (!texto) return null;

    try {
        return JSON.parse(texto) as Sesion;
    } catch {
        return null;
        // Si alguien metió basura ahí a mano, prefiero devolver null
        // que dejar que reviente la aplicación entera.
    }
}

export function borrarSesion(): void {
    if (typeof window === 'undefined') return;
    localStorage.removeItem(CLAVE);
}
