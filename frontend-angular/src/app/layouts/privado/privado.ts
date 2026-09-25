import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MenuService } from '../../services/menu.service';
import { SesionService } from '../../services/sesion.service';
import { AuthService } from '../../services/auth.service';
import { OpcionMenu } from '../../models/modelos';

@Component({
    selector: 'app-privado',
    imports: [RouterOutlet, RouterLink, RouterLinkActive],
    templateUrl: './privado.html',
})
export class Privado {
    private menuService = inject(MenuService);
    private auth = inject(AuthService);
    private router = inject(Router);

    sesion = inject(SesionService);

    menu = signal<OpcionMenu[]>([]);
    cargando = signal(true);
    saliendo = signal(false);

    constructor() {
        this.menuService.obtenerMenu().subscribe({
            next: (opciones) => {
                this.menu.set(opciones);
                this.cargando.set(false);
            },
            error: () => {
                this.menu.set([]);
                this.cargando.set(false);
            },
        });
    }

    salir(): void {
        if (this.saliendo()) return;
        // Evita que un doble clic mande dos peticiones de logout.
        this.saliendo.set(true);

        this.auth.logout().subscribe(() => this.router.navigate(['/login']));
        // ↑ El .subscribe() es lo que dispara todo. Sin él, nada.
    }
}