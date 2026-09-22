import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UsuariosService } from '../../services/usuarios.service';
import { SesionService } from '../../services/sesion.service';
import { ErrorFila, UsuarioLista } from '../../models/modelos';

@Component({
  selector: 'app-usuarios',
  imports: [ReactiveFormsModule],
  templateUrl: './usuarios.html',
})
export class Usuarios {
  private fb = inject(FormBuilder);
  private servicio = inject(UsuariosService);
  private sesion = inject(SesionService);

  usuarios = signal<UsuarioLista[]>([]);
  mensaje = signal<string | null>(null);
  mensajeError = signal<string | null>(null);
  cargando = signal(true);
  ocupado = signal(false);
  archivo = signal<File | null>(null);
  erroresCarga = signal<ErrorFila[] | null>(null);

  // ═══════════════════════════════════════════════════════════
  //   computed(): ESTADO DERIVADO
  // ═══════════════════════════════════════════════════════════

  pendientes = computed(() => this.usuarios().filter((u) => !u.aprobado));
  // Un signal que se CALCULA a partir de otro.
  //
  // Angular detecta solo que depende de usuarios(), y lo recalcula
  // cada vez que usuarios cambia. Nunca tienes que acordarte de
  // actualizarlo a mano.
  //
  // En React, esto lo calculaste dentro del render en cada repintado,
  // o tendrías que haber usado useMemo con su lista de dependencias
  // escrita a mano. Aquí las dependencias se detectan solas.
  //
  // Y se lee igual que cualquier signal: this.pendientes()

  soyAdmin = computed(() => this.sesion.sesion()?.rol === 'Administrador');
  // Solo decide QUÉ BOTONES DIBUJAR. La seguridad real es el
  // [Authorize(Roles = "Administrador")] de cada endpoint.

  formulario = this.fb.nonNullable.group({
    username: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.maxLength(20),
      Validators.pattern(/^(?=.*[0-9])(?=.*[a-zA-Z])[a-zA-Z0-9]+$/),
    ]],
    // Las cuatro reglas del enunciado para el nombre de usuario:
    // 8 a 20 caracteres, letras Y números, sin caracteres especiales.
    // La misma expresión que en tu CrearUsuarioDto de C#.

    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.maxLength(30),
      Validators.pattern(/^(?=.*[0-9])(?=.*[A-Z]).+$/),
    ]],

    correo: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],

    rolId: [3, [Validators.required, Validators.min(2), Validators.max(3)]],
    // Arranca en 3 (Cajero). min y max impiden cualquier otro valor,
    // igual que el [Range(2, 3)] del DTO.
  });

  private mensajesPatron: Record<string, string> = {
    username: 'Solo letras y números, con al menos uno de cada uno',
    password: 'Debe tener al menos una mayúscula y un número',
  };

  constructor() {
    this.cargar();
  }

  cargar(): void {
    this.servicio.listar().subscribe({
      next: (u) => { this.usuarios.set(u); this.cargando.set(false); },
      error: (e: HttpErrorResponse) => {
        this.cargando.set(false);
        this.mensajeError.set(
          e.status === 403 ? 'No tienes permiso para ver usuarios' : 'No se pudieron cargar los usuarios'
        );
      },
    });
  }

  private limpiar(): void {
    this.mensaje.set(null);
    this.mensajeError.set(null);
    this.erroresCarga.set(null);
  }

  mensajeDe(campo: string): string | null {
    const control = this.formulario.get(campo);
    if (!control || !control.touched || !control.errors) return null;
    const e = control.errors;
    if (e['servidor']) return e['servidor'];
    if (e['required']) return 'Este campo es obligatorio';
    if (e['minlength']) return `Debe tener al menos ${e['minlength'].requiredLength} caracteres`;
    if (e['maxlength']) return `Debe tener como máximo ${e['maxlength'].requiredLength} caracteres`;
    if (e['email']) return 'El correo no tiene un formato válido';
    if (e['pattern']) return this.mensajesPatron[campo] ?? 'Formato inválido';
    if (e['min'] || e['max']) return 'Rol no permitido';
    return null;
  }

  crear(): void {
    this.limpiar();

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.mensajeError.set('Revisa los campos marcados');
      return;
    }

    this.ocupado.set(true);

    this.servicio.crear(this.formulario.getRawValue()).subscribe({
      next: (r) => {
        this.mensaje.set(r.mensaje);
        // El mensaje cambia según quién creó: si fue el gestor, dice
        // que debe ser aprobado. Esa diferencia la decide el backend.

        this.formulario.reset({ username: '', password: '', correo: '', rolId: 3 });
        // reset() con valores: vuelve a dejar Cajero seleccionado.
        // Sin ese objeto, rolId quedaría en su valor inicial igual,
        // pero así queda explícito y a prueba de cambios.

        this.ocupado.set(false);
        this.cargar();
      },
      error: (e: HttpErrorResponse) => {
        this.ocupado.set(false);

        if (e.status === 400 && e.error?.errors) {
          for (const [clave, mensajes] of Object.entries(e.error.errors as Record<string, string[]>)) {
            const campo = clave.charAt(0).toLowerCase() + clave.slice(1);
            const control = this.formulario.get(campo);
            if (control) {
              control.setErrors({ servidor: mensajes[0] });
              control.markAsTouched();
            }
          }
          this.mensajeError.set('El servidor rechazó algunos campos');
          return;
        }

        this.mensajeError.set(
          e.status === 403 ? 'No tienes permiso para crear usuarios' : (e.error?.mensaje ?? 'No se pudo crear')
        );
        // 409 (usuario repetido) cae aquí con su propio mensaje.
      },
    });
  }

  private ejecutar(accion: Observable<{ mensaje: string }>): void {
    // ═══ UN MÉTODO PARA APROBAR Y CAMBIAR ESTADO ═══
    // Los dos son un POST sin datos que después recarga la tabla.
    // Recibe el Observable ya armado y se encarga del resto.
    //
    // Fíjate en el tipo: Observable<{ mensaje: string }>. Le paso
    // la petición SIN ejecutar. Recién aquí adentro, con el
    // .subscribe(), sale de verdad.
    this.limpiar();
    this.ocupado.set(true);

    accion.subscribe({
      next: (r) => {
        this.mensaje.set(r.mensaje);
        this.ocupado.set(false);
        this.cargar();
      },
      error: (e: HttpErrorResponse) => {
        this.ocupado.set(false);
        this.mensajeError.set(
          e.status === 403 ? 'No tienes permiso para esta acción' : (e.error?.mensaje ?? 'No se pudo completar')
        );
        // Aquí llegan las salvaguardas del backend: "no puedes
        // desactivarte a ti mismo", "no puedes desactivar al único
        // administrador". Las reglas viven en el servidor; la pantalla
        // solo muestra lo que el servidor dice.
      },
    });
  }

  aprobar(id: number): void {
    this.ejecutar(this.servicio.aprobar(id));
  }

  cambiarEstado(id: number): void {
    this.ejecutar(this.servicio.cambiarEstado(id));
  }

  seleccionar(evento: Event): void {
    this.archivo.set((evento.target as HTMLInputElement).files?.[0] ?? null);
    this.limpiar();
  }

  subir(): void {
    const archivo = this.archivo();
    if (!archivo) return;

    this.limpiar();
    this.ocupado.set(true);

    this.servicio.cargaMasiva(archivo).subscribe({
      next: (r) => {
        this.mensaje.set(r.mensaje);
        this.archivo.set(null);
        this.ocupado.set(false);
        this.cargar();
      },
      error: (e: HttpErrorResponse) => {
        this.ocupado.set(false);
        if (e.status === 403) {
          this.mensajeError.set('Solo un administrador puede importar usuarios');
          return;
        }
        if (e.error?.errores) {
          this.erroresCarga.set(e.error.errores);
          this.mensajeError.set(e.error.mensaje);
          return;
        }
        this.mensajeError.set(e.error?.mensaje ?? 'No se pudo procesar el archivo');
      },
    });
  }
}