import { Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, forkJoin } from 'rxjs';
import { TurnosService } from '../../services/turnos.service';
import { MiCaja, Turno } from '../../models/modelos';

@Component({
  selector: 'app-caja',
  templateUrl: './caja.html',
})
export class Caja {
  private servicio = inject(TurnosService);

  cajas = signal<MiCaja[]>([]);
  miSesionCajaId = signal<number | null>(null);
  turnos = signal<Turno[]>([]);

  error = signal<string | null>(null);
  exito = signal<string | null>(null);
  cargando = signal(true);
  ocupado = signal(false);

  // ═══ DOS computed ENCADENADOS ═══
  miCaja = computed(() =>
    this.cajas().find((c) => c.id === this.miSesionCajaId()) ?? null
  );
  // Depende de DOS signals. Angular detecta las dos dependencias solo.

  turnosDeMiCaja = computed(() => {
    const caja = this.miCaja();
    if (!caja) return [];
    return this.turnos().filter((t) => t.estado === 'PEN' && t.caja === caja.nombre);
  });
  // Este depende del anterior. Si cambia cualquiera de los tres signals
  // de origen, la cascada se recalcula sola.
  //
  // Solo los pendientes de LA caja que tengo abierta. El backend igual
  // lo verifica: si mando un turno de otra caja, responde 403.

  constructor() {
    this.cargar();
  }

  cargar(): void {
    forkJoin({
      misCajas: this.servicio.misCajas(),
      turnos: this.servicio.listarDelDia(),
    }).subscribe({
      next: (r) => {
        this.cajas.set(r.misCajas.cajas);
        this.miSesionCajaId.set(r.misCajas.miSesionCajaId);
        this.turnos.set(r.turnos);
        this.cargando.set(false);
      },
      error: (e: HttpErrorResponse) => {
        this.cargando.set(false);
        this.error.set(
          e.status === 403 ? 'Esta pantalla es solo para cajeros'
            : 'No se pudieron cargar los datos'
        );
      },
    });
  }

  private ejecutar(accion: Observable<{ mensaje: string }>, mensajeExito: string): void {
    // Abrir, cerrar y atender son lo mismo: un POST sin datos que
    // después recarga. Un método para los tres.
    this.error.set(null);
    this.exito.set(null);
    this.ocupado.set(true);

    accion.subscribe({
      next: () => {
        this.exito.set(mensajeExito);
        this.ocupado.set(false);
        this.cargar();
      },
      error: (e: HttpErrorResponse) => {
        this.ocupado.set(false);
        this.error.set(
          e.status === 403 ? 'No tienes permiso para esta acción'
            : (e.error?.mensaje ?? 'No se pudo completar la acción')
        );
        // Aquí llegan las reglas del backend: "otro cajero está
        // operando esta caja" (409), "debes abrir una sesión antes
        // de atender turnos" (409). El servidor manda.
      },
    });
  }

  abrir(cajaId: number): void {
    this.ejecutar(this.servicio.abrirCaja(cajaId), 'Sesión abierta');
  }

  cerrar(): void {
    this.ejecutar(this.servicio.cerrarCaja(), 'Sesión cerrada');
  }

  atender(turno: Turno): void {
    this.ejecutar(this.servicio.atender(turno.id), `Turno ${turno.codigo} atendido`);
  }
}