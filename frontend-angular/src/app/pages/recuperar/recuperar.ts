import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-recuperar',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './recuperar.html',
})
export class Recuperar {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);

  enviado = signal(false);
  error = signal<string | null>(null);
  enviando = signal(false);

  formulario = this.fb.nonNullable.group({
    correo: ['', [Validators.required, Validators.email]],
  });

  enviar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.error.set('Ingresa un correo válido');
      return;
    }

    this.enviando.set(true);
    this.error.set(null);

    this.auth.recuperar(this.formulario.getRawValue().correo).subscribe({
      next: () => {
        this.enviado.set(true);
        this.enviando.set(false);
      },
      // ═══ MUESTRO ÉXITO SIEMPRE QUE LA PETICIÓN FUNCIONE ═══
      // Exista o no el correo. El backend responde 200 en los dos
      // casos a propósito, y la pantalla no puede contradecirlo.
      //
      // Si aquí dijera "ese correo no está registrado", tiraría por
      // la borda toda la protección contra enumeración de usuarios
      // que construiste en el servidor.

      error: () => {
        this.error.set('No se pudo procesar la solicitud');
        this.enviando.set(false);
      },
    });
  }
}