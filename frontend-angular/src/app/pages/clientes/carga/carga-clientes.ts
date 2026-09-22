import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ClientesService } from '../../../services/clientes.service';
import { ResultadoCarga } from '../../../models/modelos';

@Component({
  selector: 'app-carga-clientes',
  templateUrl: './carga-clientes.html',
})
export class CargaClientes {
  private servicio = inject(ClientesService);

  archivo = signal<File | null>(null);
  resultado = signal<ResultadoCarga | null>(null);
  mensajeError = signal<string | null>(null);
  subiendo = signal(false);

  seleccionar(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    // "as HTMLInputElement": el evento genérico no sabe que viene de
    // un input de archivo. Le digo a TypeScript qué es para poder
    // acceder a .files.

    this.archivo.set(input.files?.[0] ?? null);
    this.resultado.set(null);
    this.mensajeError.set(null);
    // Limpio el reporte anterior: dejarlo junto a un archivo nuevo confunde.
  }

  subir(): void {
    const archivo = this.archivo();
    if (!archivo) {
      this.mensajeError.set('Selecciona un archivo primero');
      return;
    }

    this.subiendo.set(true);
    this.resultado.set(null);
    this.mensajeError.set(null);

    this.servicio.cargaMasiva(archivo).subscribe({
      next: (r) => {
        this.resultado.set(r);
        this.archivo.set(null);
        this.subiendo.set(false);
      },

      error: (e: HttpErrorResponse) => {
        this.subiendo.set(false);

        if (e.status === 403) {
          this.mensajeError.set('Tu rol no tiene permiso para importar clientes');
          return;
          // ═══ AQUÍ NO HAY EL BUG QUE ENCONTRASTE EN REACT ═══
          // En React, el 403 venía sin cuerpo y el .json() reventaba,
          // así que caía en el catch con "no se pudo conectar".
          // HttpClient no intenta parsear un cuerpo vacío: el 403 llega
          // limpio aquí, con e.error en null. Otro caso resuelto por
          // el framework.
        }

        if (e.error?.errores) {
          this.resultado.set(e.error as ResultadoCarga);
          return;
          // Reporte por fila: el archivo se leyó, pero hay filas con
          // problemas. Viene con 400, así que llega a "error", pero
          // trae el mismo formato que el éxito.
        }

        this.mensajeError.set(
          e.status === 0
            ? 'No se pudo conectar con el servidor'
            : (e.error?.mensaje ?? 'No se pudo procesar el archivo')
        );
      },
    });
  }
}