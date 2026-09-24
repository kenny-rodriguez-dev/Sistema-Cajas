import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { TurnosService } from '../../services/turnos.service';
import { CajaLista, TipoAtencion, Turno } from '../../models/modelos';

@Component({
  selector: 'app-turnos',
  imports: [ReactiveFormsModule],
  templateUrl: './turnos.html',
})
export class Turnos {
  private fb = inject(FormBuilder);
  private servicio = inject(TurnosService);

  cajas = signal<CajaLista[]>([]);
  tipos = signal<TipoAtencion[]>([]);
  turnos = signal<Turno[]>([]);

  ultimoCodigo = signal<string | null>(null);
  error = signal<string | null>(null);
  cargando = signal(true);
  generando = signal(false);

  formulario = this.fb.nonNullable.group({
    cajaId: [0, [Validators.required, Validators.min(1)]],
    tipoAtencionId: [0, [Validators.required, Validators.min(1)]],
    // Arrancan en 0 = "nada elegido". El min(1) lo rechaza,
    // igual que el [Range(1, int.MaxValue)] del DTO de C#.
  });

  // Traduzco los códigos de tres letras a algo legible.
  private etiquetas: Record<string, { texto: string; clase: string }> = {
    PEN: { texto: 'Pendiente', clase: 'bg-amber-100 text-amber-800' },
    ATE: { texto: 'Atendido', clase: 'bg-green-100 text-green-800' },
    CAN: { texto: 'Cancelado', clase: 'bg-gray-100 text-gray-600' },
  };

  etiqueta(estado: string) {
    return this.etiquetas[estado] ?? { texto: estado, clase: 'bg-gray-100' };
  }

  constructor() {
    forkJoin({
      cajas: this.servicio.listarCajas(),
      tipos: this.servicio.listarTipos(),
      turnos: this.servicio.listarDelDia(),
    }).subscribe({
      // ═══ forkJoin ES EL Promise.all DE RxJS ═══
      //
      // Lanza las tres peticiones A LA VEZ y espera a que las tres
      // terminen. Tres viajes en paralelo tardan lo que el más lento,
      // no la suma de los tres.
      //
      // Al pasarle un OBJETO, el resultado llega como un objeto con
      // las mismas claves. Eso es más legible que un array donde
      // tendrías que acordarte de qué posición es cada cosa.
      //
      // Ojo con una trampa: si UNA de las tres falla, forkJoin falla
      // entero y ninguna respuesta llega. Para peticiones que se
      // necesitan todas, como aquí, eso es lo correcto.

      next: (r) => {
        this.cajas.set(r.cajas);
        this.tipos.set(r.tipos);
        this.turnos.set(r.turnos);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar los datos');
        this.cargando.set(false);
      },
    });
  }

  recargarTurnos(): void {
    this.servicio.listarDelDia().subscribe({
      next: (t) => this.turnos.set(t),
    });
  }

  generar(): void {
    this.error.set(null);
    this.ultimoCodigo.set(null);

    if (this.formulario.invalid) {
      this.error.set('Elige una caja y un tipo de atención');
      return;
    }

    this.generando.set(true);
    const { cajaId, tipoAtencionId } = this.formulario.getRawValue();

    this.servicio.crear(cajaId, tipoAtencionId).subscribe({
      next: (r) => {
        this.ultimoCodigo.set(r.codigo);
        this.generando.set(false);
        this.recargarTurnos();
        // Recargo la lista en vez de agregar el turno a mano.
        // Así lo que ves es siempre lo que dice la base, no una
        // copia local que podría desincronizarse.
      },
      error: (e: HttpErrorResponse) => {
        this.generando.set(false);
        this.error.set(
          e.status === 403 ? 'Solo un gestor puede generar turnos'
            : (e.error?.mensaje ?? 'No se pudo generar el turno')
        );
      },
    });
  }

  cancelar(t: Turno): void {
    if (!window.confirm(`¿Cancelar el turno ${t.codigo}?`)) return;
    this.error.set(null);

    this.servicio.cancelarTurno(t.id).subscribe({
      next: () => this.recargarTurnos(),
      error: (e: HttpErrorResponse) =>
        this.error.set(
          e.status === 403 ? 'Solo un gestor puede cancelar turnos'
            : (e.error?.mensaje ?? 'No se pudo cancelar')
        ),
    });
  }
}