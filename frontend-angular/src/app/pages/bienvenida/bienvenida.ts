import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DashboardService } from '../../services/dashboard.service';
import { BienvenidaDto } from '../../models/modelos';
// Ahora el import y la clase tienen nombres distintos: no chocan.

@Component({
  selector: 'app-bienvenida',
  imports: [DatePipe],
  // DatePipe es el "|date" de la plantilla. Como el componente es
  // standalone, hay que importarlo aquí explícitamente.
  templateUrl: './bienvenida.html',
})
export class Bienvenida {
  private servicio = inject(DashboardService);

  datos = signal<BienvenidaDto | null>(null);
  error = signal<string | null>(null);
  cargando = signal(true);

  constructor() {
    this.servicio.bienvenida().subscribe({
      next: (d) => { this.datos.set(d); this.cargando.set(false); },
      error: () => { this.error.set('No se pudieron cargar tus datos'); this.cargando.set(false); },
    });
  }
}