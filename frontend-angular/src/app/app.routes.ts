import { Routes } from '@angular/router';
import { authGuard, rolGuard } from './guards/auth.guard';

export const routes: Routes = [
    { path: '', redirectTo: 'bienvenida', pathMatch: 'full' },

    // ─── PÚBLICAS ───
    {
        path: 'login',
        loadComponent: () => import('./pages/login/login').then((m) => m.Login),
        // Fíjate: la ruta del archivo SIN extensión y SIN ".component",
        // y el nombre de la clase tal como está escrito dentro del archivo.
        // Esos dos tienen que coincidir exactamente o el import no resuelve.
    },
    {
        path: 'recuperar',
        loadComponent: () => import('./pages/recuperar/recuperar').then((m) => m.Recuperar),
    },
    {
        path: 'restablecer',
        loadComponent: () => import('./pages/restablecer/restablecer').then((m) => m.Restablecer),
    },

    // ─── ZONA PRIVADA ───
    {
        path: '',
        loadComponent: () => import('./layouts/privado/privado').then((m) => m.Privado),
        canActivate: [authGuard],
        // Un solo guard protege todo el bloque de hijas.

        children: [
            {
                path: 'bienvenida',
                loadComponent: () => import('./pages/bienvenida/bienvenida').then((m) => m.Bienvenida),
            },
            {
                path: 'dashboard',
                canActivate: [rolGuard(['Administrador'])],
                loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
            },
            {
                path: 'clientes/nuevo',
                canActivate: [rolGuard(['Cajero'])],
                loadComponent: () =>
                    import('./pages/clientes/nuevo/nuevo-cliente').then((m) => m.NuevoCliente),
            },
            {
                path: 'clientes/carga',
                canActivate: [rolGuard(['Cajero'])],
                loadComponent: () =>
                    import('./pages/clientes/carga/carga-clientes').then((m) => m.CargaClientes),
            },
            {
                path: 'usuarios',
                canActivate: [rolGuard(['Administrador', 'Gestor'])],
                loadComponent: () => import('./pages/usuarios/usuarios').then((m) => m.Usuarios),
            },
            {
                path: 'turnos',
                canActivate: [rolGuard(['Gestor'])],
                loadComponent: () => import('./pages/turnos/turnos').then((m) => m.Turnos),
            },
            {
                path: 'caja',
                canActivate: [rolGuard(['Cajero'])],
                loadComponent: () => import('./pages/caja/caja').then((m) => m.Caja),
            },
            {
                path: 'en-construccion',
                loadComponent: () =>
                    import('./pages/en-construccion/en-construccion').then((m) => m.EnConstruccion),
            },
        ],
    },

    { path: '**', redirectTo: 'bienvenida' },
];