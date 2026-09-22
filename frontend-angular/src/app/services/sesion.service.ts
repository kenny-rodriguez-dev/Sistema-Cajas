import { Injectable, signal } from '@angular/core';
import { Sesion } from '../models/modelos';

@Injectable({ providedIn: 'root' })
// @Injectable lo convierte en un servicio inyectable.
// "providedIn: 'root'" significa que Angular crea UNA sola instancia
// para toda la aplicación: un singleton.
//
// ¿Te suena? Es exactamente lo que haces en C# con
// builder.Services.AddScoped<IRepositorioClientes, RepositorioClientes>().
// Angular tiene el mismo sistema de inyección de dependencias que .NET,
// solo que en el navegador.
export class SesionService {
    private readonly CLAVE = 'sesion';

    readonly sesion = signal<Sesion | null>(this.leer());
    // ═══ UN SIGNAL ES EL useState DE ANGULAR ═══
    //
    // Guarda un valor y avisa cuando cambia. La diferencia con React:
    // aquí no hay un "setSesion" aparte, el propio signal tiene métodos.
    //
    //   this.sesion()          -> LEER (fíjate en los paréntesis)
    //   this.sesion.set(x)     -> ESCRIBIR
    //
    // Angular 22 viene "zoneless": sin signals no sabría cuándo
    // repintar la pantalla. Por eso el estado va siempre en signals.

    private leer(): Sesion | null {
        if (typeof localStorage === 'undefined') return null;
        const texto = localStorage.getItem(this.CLAVE);
        if (!texto) return null;
        try { return JSON.parse(texto) as Sesion; }
        catch { return null; }
        // Si alguien metió basura a mano, devuelvo null en vez de
        // dejar que reviente la aplicación entera.
    }

    guardar(sesion: Sesion): void {
        localStorage.setItem(this.CLAVE, JSON.stringify(sesion));
        this.sesion.set(sesion);
        // Guardo en disco Y actualizo el signal. Cualquier componente
        // que esté leyendo this.sesion() se repinta solo.
    }

    borrar(): void {
        localStorage.removeItem(this.CLAVE);
        this.sesion.set(null);
    }

    get token(): string | null {
        return this.sesion()?.token ?? null;
    }
    // Un "getter": se usa como si fuera una propiedad (servicio.token),
    // pero por dentro ejecuta código.
}