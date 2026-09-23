import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, forkJoin } from 'rxjs';
import { TurnosService } from '../../services/turnos.service';
import { UsuariosService } from '../../services/usuarios.service';
import { CajaDetalle, CajeroAsignado, UsuarioLista } from '../../models/modelos';

@Component({
  selector: 'app-cajas',
  templateUrl: './cajas.html',
})
export class Cajas {
  private turnosService = inject(TurnosService);
  private usuariosService = inject(UsuariosService);

  cajas = signal<CajaDetalle[]>([]);
  cajeros = signal<UsuarioLista[]>([]);
  seleccion = signal<Record<number, number>>({});
  // Un desplegable por caja: guardo la elección de cada uno
  // indexada por el id de la caja.

  mensaje = signal<string | null>(null);
  error = signal<string | null>(null);
  cargando = signal(true);
  ocupado = signal(false);

  constructor() {
    this.cargar();
  }

  cargar(): void {
    forkJoin({
      cajas: this.turnosService.listarCajas(),
      usuarios: this.usuariosService.listar(),
    }).subscribe({
      next: (r) => {
        this.cajas.set(r.cajas);
        this.cajeros.set(
          r.usuarios.filter((u) => u.rol === 'Cajero' && u.activo && u.aprobado)
        );
        // Solo cajeros activos y aprobados.
        this.cargando.set(false);
      },
      error: (e: HttpErrorResponse) => {
        this.cargando.set(false);
        this.error.set(
          e.status === 403 ? 'Solo un gestor puede asignar cajas' : 'No se pudieron cargar los datos'
        );
      },
    });
  }

  disponiblesPara(caja: CajaDetalle): UsuarioLista[] {
    const ids = new Set(caja.cajerosAsignados.map((c) => c.id));
    return this.cajeros().filter((c) => !ids.has(c.id));
    // En el desplegable solo los que NO están ya en esta caja.
  }

  elegir(cajaId: number, evento: Event): void {
    const valor = Number((evento.target as HTMLSelectElement).value);
    this.seleccion.update((actual) => ({ ...actual, [cajaId]: valor }));
    // ═══ .update() EN VEZ DE .set() ═══
    // update recibe el valor ANTERIOR y devuelve el nuevo.
    // Lo necesito porque solo cambio UNA caja del objeto y quiero
    // conservar las demás. Es el setState((prev) => ...) de React.
  }

  private ejecutar(accion: Observable<{ mensaje: string }>): void {
    this.ocupado.set(true);
    this.error.set(null);
    this.mensaje.set(null);

    accion.subscribe({
      next: (r) => {
        this.mensaje.set(r.mensaje);
        this.seleccion.set({});
        this.ocupado.set(false);
        this.cargar();
      },
      error: (e: HttpErrorResponse) => {
        this.ocupado.set(false);
        this.error.set(
          e.status === 403 ? 'No tienes permiso para esta acción'
            : (e.error?.mensaje ?? 'No se pudo completar')
        );
        // Aquí llegan las reglas del backend: "máximo 2 cajeros",
        // "no se puede quitar: tiene la caja abierta".
      },
    });
  }

  asignar(cajaId: number): void {
    const usuarioId = this.seleccion()[cajaId];
    if (!usuarioId) {
      this.error.set('Elige un cajero primero');
      return;
    }
    this.ejecutar(this.turnosService.asignarCajero(cajaId, usuarioId));
  }

  quitar(cajaId: number, cajero: CajeroAsignado): void {
    if (!window.confirm(`¿Quitar a ${cajero.username} de esta caja?`)) return;
    this.ejecutar(this.turnosService.quitarCajero(cajaId, cajero.id));
  }
}