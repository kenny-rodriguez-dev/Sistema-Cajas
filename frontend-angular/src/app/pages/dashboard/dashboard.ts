import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors,
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardDto } from '../../models/modelos';

// ═══════════════════════════════════════════════════════════════
//   VALIDADOR DE GRUPO: una regla que mira DOS campos
// ═══════════════════════════════════════════════════════════════
function rangoValido(grupo: AbstractControl): ValidationErrors | null {
  const desde = grupo.get('desde')?.value;
  const hasta = grupo.get('hasta')?.value;

  if (!desde || !hasta) return null;
  // Si falta alguno, esta regla no opina: de eso se encarga "required".

  return desde > hasta ? { rangoInvalido: true } : null;
  // Devuelve null si todo está bien, o un objeto con el error.
  // Esa es la forma de TODOS los validadores de Angular.
  //
  // Comparar fechas como texto funciona porque el formato AAAA-MM-DD
  // ordena igual alfabéticamente que cronológicamente.
}
// ¿Por qué no un validador normal en el campo "hasta"?
// Porque un validador de campo solo ve SU valor. Para comparar
// "desde" con "hasta" necesito ver los dos a la vez, y eso solo lo
// puede hacer un validador puesto sobre el grupo.
//
// En React hiciste esto a mano dentro de la función cargar().
// Aquí es parte del formulario: su estado de "válido" ya lo incluye.

function hoy(): string {
  return new Date().toISOString().slice(0, 10);
}

@Component({
  selector: 'app-dashboard',
  imports: [ReactiveFormsModule],
  templateUrl: './dashboard.html',
})
export class Dashboard {
  private fb = inject(FormBuilder);
  private servicio = inject(DashboardService);

  datos = signal<DashboardDto | null>(null);
  error = signal<string | null>(null);
  cargando = signal(false);

  filtro = this.fb.nonNullable.group(
    { desde: [hoy()], hasta: [hoy()] },
    { validators: rangoValido }
    // ← el segundo argumento aplica el validador al GRUPO entero.
  );

  constructor() {
    this.cargar();
    // Carga UNA vez al abrir. Después solo con el botón.
    //
    // Te acuerdas del bug de React: recargar en cada tecla mandaba
    // peticiones con fechas a medio escribir y el backend respondía
    // 400. Aquí esa trampa no existe desde el principio.
  }

  cargar(): void {
    if (this.filtro.invalid) {
      this.error.set('La fecha inicial no puede ser mayor que la final');
      return;
      // El grupo es inválido si rangoValido devolvió un error.
      // No se manda nada al servidor. Capa 1.
    }

    this.cargando.set(true);
    this.error.set(null);

    const { desde, hasta } = this.filtro.getRawValue();

    this.servicio.indicadores(desde, hasta).subscribe({
      next: (d) => { this.datos.set(d); this.cargando.set(false); },
      error: (e: HttpErrorResponse) => {
        this.cargando.set(false);
        this.error.set(
          e.status === 403
            ? 'Solo los administradores pueden ver el dashboard'
            : (e.error?.mensaje ?? 'No se pudieron cargar los indicadores')
        );
        // El backend valida el rango otra vez. Si alguien se saltara
        // el validador de grupo, el mensaje del 400 aparecería aquí.
        // Capa 2.
      },
    });
  }
}