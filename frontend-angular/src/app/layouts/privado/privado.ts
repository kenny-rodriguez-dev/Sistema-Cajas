import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MenuService } from '../../services/menu.service';
import { SesionService } from '../../services/sesion.service';
import { AuthService } from '../../services/auth.service';
import { OpcionMenu } from '../../models/modelos';

@Component({
    selector: 'app-privado',
    standalone: true,
    imports: [RouterOutlet, RouterLink, RouterLinkActive],
    templateUrl: './privado.html',
})
export class Privado {
    private menuService = inject(MenuService);
    private auth = inject(AuthService);
    private router = inject(Router);

    sesion = inject(SesionService);   // público: la plantilla lo lee

    menu = signal<OpcionMenu[]>([]);
    cargando = signal(true);

    constructor() {
        this.menuService.obtenerMenu().subscribe({
            next: (opciones) => {
                this.menu.set(opciones);
                this.cargando.set(false);
            },
            error: () => {
                this.menu.set([]);
                this.cargando.set(false);
                // Si falla, dejo el menú vacío en vez de romper la pantalla.
                // El 401 por token vencido ya lo maneja el interceptor.
            },
        });
    }
    // Pido el menú en el constructor. Hay quien prefiere ngOnInit
    // (el equivalente del useEffect de React); con inject() y signals
    // el constructor es igual de válido y más corto.

    salir(): void {
        this.auth.logout();
        this.router.navigate(['/login']);
    }
}