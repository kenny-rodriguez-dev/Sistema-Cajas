import { Routes } from '@angular/router';
import { authGuard, rolGuard } from './guards/auth.guard';

export const routes: Routes = [
    { path: '', redirectTo: 'bienvenida', pathMatch: 'full' },

    // ─── PÚBLICAS ───
    {
        path: 'login',
        loadComponent: () => import('./pages/login/login').then((m) => m.Login),
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
                path: 'clientes',
                pathMatch: 'full',
                // pathMatch 'full' = esta ruta solo coincide con /clientes EXACTO.
                // Sin esto, Angular podría intentar usarla para /clientes/nuevo
                // por coincidir el comienzo. Así queda explícito.
                canActivate: [rolGuard(['Cajero'])],
                loadComponent: () =>
                    import('./pages/clientes/lista/clientes-lista').then((m) => m.ClientesLista),
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
                path: 'cajas',
                // La URL sigue siendo /cajas: es la que está guardada en el menú
                // de la base de datos. Lo que cambió es el nombre del componente.
                canActivate: [rolGuard(['Gestor'])],
                loadComponent: () =>
                    import('./pages/asignacion-cajas/asignacion-cajas').then((m) => m.AsignacionCajas),
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
            {
                path: 'atencion',
                canActivate: [rolGuard(['Cajero'])],
                loadComponent: () => import('./pages/atencion/atencion').then((m) => m.Atencion),
            },
        ],
    },

    { path: '**', redirectTo: 'bienvenida' },
];