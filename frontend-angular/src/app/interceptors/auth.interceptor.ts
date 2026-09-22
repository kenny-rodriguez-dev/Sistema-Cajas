import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { SesionService } from '../services/sesion.service';

export const authInterceptor: HttpInterceptorFn = (peticion, siguiente) => {
    // ═══ ESTO ES UNA FUNCIÓN, NO UNA CLASE ═══
    // Angular moderno usa interceptores funcionales. Los tutoriales
    // viejos muestran clases con "implements HttpInterceptor": esa
    // forma sigue funcionando pero ya no es la recomendada.
    //
    // Fíjate en la forma: recibe la petición y "siguiente", igual que
    // tu middleware de C#. Es el MISMO concepto: una tubería donde cada
    // pieza puede tocar la petición y pasarla, o cortarla.

    const sesion = inject(SesionService);
    const router = inject(Router);
    // inject() pide una dependencia. Dentro de una función no hay
    // constructor donde pedirla, así que se usa esto.

    const token = sesion.token;

    const peticionConToken = token
        ? peticion.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
        : peticion;
    // ═══ .clone() ES OBLIGATORIO ═══
    // Las peticiones de Angular son INMUTABLES: no se pueden modificar.
    // Hay que hacer una copia con el cambio aplicado.
    //
    // ¿Por qué inmutables? Porque una petición puede reintentarse, y si
    // alguien la modificó por el camino, el reintento mandaría algo
    // distinto a lo que se pidió.

    return siguiente(peticionConToken).pipe(
        // "siguiente" pasa la petición al resto de la tubería.
        // .pipe() encadena operaciones sobre la respuesta que va a volver.

        catchError((error) => {
            if (error.status === 401) {
                sesion.borrar();
                router.navigate(['/login']);
                // Token vencido: limpio y mando al login. Igual que en tu
                // apiFetch de React. Un solo lugar para toda la aplicación.
            }
            return throwError(() => error);
            // Reenvío el error para que el componente también se entere.
            // Si no lo hiciera, el componente se quedaría esperando para siempre.
        })
    );
};