import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import {
  AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, Validators,
} from '@angular/forms';
// Dos importaciones nuevas: AbstractControl y FormGroup.
// Son los tipos "generales" que comparten todos los controles y
// todos los formularios.
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UsuariosService } from '../../services/usuarios.service';
import { SesionService } from '../../services/sesion.service';
import { ErrorFila, UsuarioLista } from '../../models/modelos';

@Component({
  selector: 'app-usuarios',
  imports: [ReactiveFormsModule, DatePipe],
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
  editando = signal<UsuarioLista | null>(null);

  pendientes = computed(() => this.usuarios().filter((u) => !u.aprobado));
  bloqueados = computed(() => this.usuarios().filter((u) => u.bloqueadoHasta !== null));
  soyAdmin = computed(() => this.sesion.sesion()?.rol === 'Administrador');

  formulario = this.fb.nonNullable.group({
    username: ['', [
      Validators.required, Validators.minLength(8), Validators.maxLength(20),
      Validators.pattern(/^(?=.*[0-9])(?=.*[a-zA-Z])[a-zA-Z0-9]+$/),
    ]],
    password: ['', [
      Validators.required, Validators.minLength(8), Validators.maxLength(30),
      Validators.pattern(/^(?=.*[0-9])(?=.*[A-Z]).+$/),
    ]],
    correo: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],
    rolId: [3, [Validators.required, Validators.min(2), Validators.max(3)]],
  });

  edicion = this.fb.nonNullable.group({
    correo: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],
    rolId: [3, [Validators.required, Validators.min(2), Validators.max(3)]],
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

  private textoError(control: AbstractControl | null, campo: string): string | null {
    // ═══ AbstractControl: EL TIPO QUE COMPARTEN TODOS LOS CONTROLES ═══
    // Un campo de cualquier formulario ES un AbstractControl.
    // Pidiendo este tipo general, la función sirve para el formulario
    // de crear y para el de editar, sin que TypeScript tenga que
    // adivinar de cuál viene.
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

  mensajeDe(campo: string): string | null {
    return this.textoError(this.formulario.get(campo), campo);
  }

  mensajeEdicion(campo: string): string | null {
    return this.textoError(this.edicion.get(campo), campo);
  }

  private pegarErrores(grupo: FormGroup, errores: Record<string, string[]>): void {
    // ═══ ANTES: grupo: typeof this.formulario | typeof this.edicion ═══
    // Eso le decía a TypeScript "puede ser uno u otro formulario", y
    // como cada uno tiene su propio .get() con sus propios nombres de
    // campo, no podía decidir cuál usar.
    //
    // AHORA: grupo: FormGroup, el tipo general de cualquier formulario.
    // Su .get() acepta cualquier nombre. Los dos formularios entran
    // aquí sin problema, porque los dos SON un FormGroup.
    for (const [clave, mensajes] of Object.entries(errores)) {
      const control = grupo.get(clave.charAt(0).toLowerCase() + clave.slice(1));
      if (control) {
        control.setErrors({ servidor: mensajes[0] });
        control.markAsTouched();
      }
    }
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
        this.formulario.reset({ username: '', password: '', correo: '', rolId: 3 });
        this.ocupado.set(false);
        this.cargar();
      },
      error: (e: HttpErrorResponse) => {
        this.ocupado.set(false);
        if (e.status === 400 && e.error?.errors) {
          this.pegarErrores(this.formulario, e.error.errors);
          this.mensajeError.set('El servidor rechazó algunos campos');
          return;
        }
        this.mensajeError.set(
          e.status === 403 ? 'No tienes permiso para crear usuarios' : (e.error?.mensaje ?? 'No se pudo crear')
        );
      },
    });
  }

  empezarEdicion(u: UsuarioLista): void {
    this.limpiar();
    this.editando.set(u);
    this.edicion.reset({ correo: u.correo, rolId: u.rolId });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  cancelarEdicion(): void {
    this.editando.set(null);
  }

  guardarEdicion(): void {
    const u = this.editando();
    if (!u) return;
    this.limpiar();

    if (this.edicion.invalid) {
      this.edicion.markAllAsTouched();
      this.mensajeError.set('Revisa los campos marcados');
      return;
    }
    this.ocupado.set(true);

    this.servicio.actualizar(u.id, { ...this.edicion.getRawValue(), activo: u.activo }).subscribe({
      next: (r) => {
        this.mensaje.set(r.mensaje);
        this.editando.set(null);
        this.ocupado.set(false);
        this.cargar();
      },
      error: (e: HttpErrorResponse) => {
        this.ocupado.set(false);
        if (e.status === 400 && e.error?.errors) {
          this.pegarErrores(this.edicion, e.error.errors);
          this.mensajeError.set('El servidor rechazó algunos campos');
          return;
        }
        this.mensajeError.set(
          e.status === 403 ? 'Solo un administrador puede editar usuarios' : (e.error?.mensaje ?? 'No se pudo guardar')
        );
      },
    });
  }

  private ejecutar(accion: Observable<{ mensaje: string }>): void {
    this.limpiar();
    this.ocupado.set(true);
    accion.subscribe({
      next: (r) => { this.mensaje.set(r.mensaje); this.ocupado.set(false); this.cargar(); },
      error: (e: HttpErrorResponse) => {
        this.ocupado.set(false);
        this.mensajeError.set(
          e.status === 403 ? 'No tienes permiso para esta acción' : (e.error?.mensaje ?? 'No se pudo completar')
        );
      },
    });
  }

  aprobar(id: number): void { this.ejecutar(this.servicio.aprobar(id)); }
  cambiarEstado(id: number): void { this.ejecutar(this.servicio.cambiarEstado(id)); }
  desbloquear(id: number): void { this.ejecutar(this.servicio.desbloquear(id)); }

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
        if (e.status === 403) { this.mensajeError.set('Solo un administrador puede importar usuarios'); return; }
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