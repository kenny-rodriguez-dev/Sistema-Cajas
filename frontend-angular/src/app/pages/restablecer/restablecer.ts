import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';

function clavesIguales(grupo: AbstractControl): ValidationErrors | null {
  const a = grupo.get('password')?.value;
  const b = grupo.get('confirmacion')?.value;
  if (!a || !b) return null;
  return a === b ? null : { noCoinciden: true };
}
// Otro validador de grupo, como el del dashboard: compara DOS campos.
// Esta regla existe SOLO en el frontend, y está bien: no es una regla
// de negocio, es evitar que el usuario se equivoque al teclear.
// Al backend solo le llega una contraseña.

@Component({
  selector: 'app-restablecer',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './restablecer.html',
})
export class Restablecer {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private ruta = inject(ActivatedRoute);

  token = this.ruta.snapshot.queryParamMap.get('token') ?? '';
  // Lee ?token=... de la URL.
  //
  // "snapshot" = el valor AHORA, una sola vez. Angular también ofrece
  // un observable para cuando los parámetros cambian sin recargar,
  // pero aquí el token se lee al entrar y no cambia.
  //
  // Fíjate que Angular no necesitó nada como el <Suspense> que puso
  // Next.js: aquí leer la URL es una operación normal.

  listo = signal(false);
  error = signal<string | null>(null);
  enviando = signal(false);

  formulario = this.fb.nonNullable.group(
    {
      password: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(30),
        Validators.pattern(/^(?=.*[0-9])(?=.*[A-Z]).+$/),
      ]],
      // Las MISMAS reglas que al crear un usuario. Si aquí fueran más
      // flojas, la recuperación sería la puerta trasera para poner
      // contraseñas débiles.
      confirmacion: ['', [Validators.required]],
    },
    { validators: clavesIguales }
  );

  constructor() {
    if (!this.token) {
      this.error.set('El enlace está incompleto. Solicita uno nuevo.');
    }
  }

  enviar(): void {
    if (!this.token) return;

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.error.set(
        this.formulario.errors?.['noCoinciden']
          ? 'Las contraseñas no coinciden'
          : 'Revisa los requisitos de la contraseña'
      );
      return;
    }

    this.enviando.set(true);
    this.error.set(null);

    this.auth.restablecer(this.token, this.formulario.getRawValue().password).subscribe({
      next: () => {
        this.listo.set(true);
        this.enviando.set(false);
        setTimeout(() => this.router.navigate(['/login']), 3000);
        // Tres segundos para leer el mensaje, y al login.
      },
      error: (e: HttpErrorResponse) => {
        this.enviando.set(false);

        const errores = e.error?.errors;
        this.error.set(
          errores
            ? Object.values(errores as Record<string, string[]>).flat().join(' ')
            : (e.error?.mensaje ?? 'No se pudo restablecer la contraseña')
        );
        // El 400 viene de dos formas: errores de validación del DTO
        // (con "errors") o el mensaje del token inválido. .flat()
        // aplana el array de arrays a uno solo.
      },
    });
  }
}