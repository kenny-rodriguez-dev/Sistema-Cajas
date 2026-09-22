import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { SesionService } from '../services/sesion.service';

export const authGuard: CanActivateFn = (ruta, estado) => {
    // Un GUARD decide si se puede entrar a una ruta.
    // Devuelve true (pasa) o false (no pasa).
    //
    // En React esto lo escribiste a mano con un useEffect dentro del
    // layout. Angular lo tiene como concepto de primera clase: se
    // declara en la ruta y se acabó.

    const sesion = inject(SesionService);
    const router = inject(Router);

    if (sesion.sesion()) return true;

    router.navigate(['/login'], { queryParams: { volver: estado.url } });
    return false;
    // Guardo a dónde quería ir, para poder devolverlo ahí tras el login.
    //
    // Y el recordatorio de siempre: ESTO NO ES SEGURIDAD.
    // Cualquiera lo salta desde las herramientas del navegador. Lo único
    // que de verdad protege los datos es el [Authorize] del backend.
    // Capa 1 y capa 2, igual que en React.
};

export const rolGuard = (rolesPermitidos: string[]): CanActivateFn => {
    // Una función que DEVUELVE un guard. Así puedo configurarlo por ruta:
    //   canActivate: [authGuard, rolGuard(['Administrador'])]
    return () => {
        const sesion = inject(SesionService);
        const router = inject(Router);

        const rol = sesion.sesion()?.rol;

        if (rol && rolesPermitidos.includes(rol)) return true;

        router.navigate(['/bienvenida']);
        return false;
    };
};