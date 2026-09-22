import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ClientesService } from '../../../services/clientes.service';

@Component({
  selector: 'app-nuevo-cliente',
  imports: [ReactiveFormsModule],
  templateUrl: './nuevo-cliente.html',
})
export class NuevoCliente {
  private fb = inject(FormBuilder);
  private servicio = inject(ClientesService);

  exito = signal<string | null>(null);
  mensajeError = signal<string | null>(null);
  enviando = signal(false);

  formulario = this.fb.nonNullable.group({
    nombres: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
    apellidos: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],

    identificacion: ['', [
      Validators.required,
      Validators.minLength(10),
      Validators.maxLength(13),
      Validators.pattern(/^[0-9]+$/),
    ]],
    // ═══ COMPÁRALO CON TU DTO DE C# ═══
    //
    //   [Required]                              -> Validators.required
    //   [StringLength(13, MinimumLength = 10)]  -> minLength(10) + maxLength(13)
    //   [RegularExpression(@"^[0-9]+$")]        -> Validators.pattern(/^[0-9]+$/)
    //
    // Las MISMAS reglas, escritas en otro idioma. Un validador por
    // regla, igual que decidiste en la rebanada 1B: así cada mensaje
    // dice exactamente qué falló.

    correo: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],

    telefono: ['', [
      Validators.required,
      Validators.minLength(10),
      Validators.maxLength(13),
      Validators.pattern(/^09[0-9]+$/),
    ]],

    direccion: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(100)]],
    referenciaDireccion: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(100)]],
  });

  // ═══ EL PRECIO DE ESTA COMODIDAD ═══
  // Ahora las reglas viven en DOS lugares: aquí y en el DTO de C#.
  // Si mañana alguien cambia una en el backend y se olvida de aquí,
  // quedan desincronizadas.
  //
  // ¿Qué pasa entonces? El frontend deja pasar algo que el backend
  // rechaza con 400. Y ahí entra el método de abajo: pega el error
  // del servidor al campo. La capa 2 sigue siendo la fuente de verdad,
  // y esto es la red de seguridad para cuando las dos se desalinean.

  private mensajesPatron: Record<string, string> = {
    identificacion: 'Solo puede contener números',
    telefono: 'Debe empezar con 09 y contener solo números',
  };
  // Validators.pattern no dice QUÉ patrón falló, solo que falló.
  // Por eso cada campo con patrón tiene su propio mensaje aquí.

  mensajeDe(campo: string): string | null {
    const control = this.formulario.get(campo);
    if (!control || !control.touched || !control.errors) return null;
    // ═══ touched ES LA CLAVE ═══
    // Un campo "tocado" es uno en el que el usuario entró y salió.
    // Sin este chequeo, el formulario aparecería lleno de errores
    // rojos apenas carga la pantalla, antes de escribir nada.
    // En React no controlaste esto: los errores solo venían del servidor.

    const e = control.errors;

    if (e['servidor']) return e['servidor'];
    // Primero el error del backend, si lo hay. Tiene prioridad
    // porque es el que de verdad cuenta.

    if (e['required']) return 'Este campo es obligatorio';
    if (e['minlength']) return `Debe tener al menos ${e['minlength'].requiredLength} caracteres`;
    if (e['maxlength']) return `Debe tener como máximo ${e['maxlength'].requiredLength} caracteres`;
    // minlength viene con información: requiredLength y actualLength.
    // Así el mensaje dice el número exacto sin escribirlo a mano.

    if (e['email']) return 'El correo no tiene un formato válido';
    if (e['pattern']) return this.mensajesPatron[campo] ?? 'Formato inválido';

    return null;
  }

  guardar(): void {
    this.exito.set(null);
    this.mensajeError.set(null);

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      // Marca todos como tocados para que aparezcan TODOS los errores,
      // incluidos los campos en los que el usuario nunca hizo clic.
      this.mensajeError.set('Revisa los campos marcados en rojo');
      return;
      // Capa 1 en acción: no se gasta un viaje al servidor.
    }

    this.enviando.set(true);

    this.servicio.crear(this.formulario.getRawValue()).subscribe({
      // getRawValue() entrega el objeto con los siete campos.
      // Como los nombres coinciden con el DTO, se manda tal cual.

      next: (respuesta) => {
        this.exito.set(`Cliente creado correctamente (id ${respuesta.id})`);
        this.formulario.reset();
        // reset() vacía los campos Y borra el estado "touched".
        // Sin lo segundo, el formulario vacío aparecería lleno de
        // errores de "campo obligatorio".
        this.enviando.set(false);
      },

      error: (e: HttpErrorResponse) => {
        this.enviando.set(false);

        if (e.status === 400 && e.error?.errors) {
          this.pegarErroresDelServidor(e.error.errors);
          this.mensajeError.set('El servidor rechazó algunos campos');
          return;
        }

        if (e.status === 409) {
          this.mensajeError.set(e.error?.mensaje ?? 'Esa identificación ya está registrada');
          return;
          // 409 es la regla de negocio (capa 3): no es un error de un
          // campo concreto, así que va arriba y no bajo un input.
        }

        if (e.status === 403) {
          this.mensajeError.set('Tu rol no tiene permiso para registrar clientes');
          return;
        }

        this.mensajeError.set(
          e.status === 0
            ? 'No se pudo conectar con el servidor'
            : `Error inesperado (${e.status})`
        );
        // ═══ status 0 = ERROR DE RED ═══
        // En Angular, cuando el servidor no responde (apagado, sin red,
        // CORS bloqueado), el status llega en 0. No hay un catch aparte
        // como con fetch: todo cae en este mismo "error".
      },
    });
  }

  private pegarErroresDelServidor(errores: Record<string, string[]>): void {
    for (const [clave, mensajes] of Object.entries(errores)) {
      const campo = clave.charAt(0).toLowerCase() + clave.slice(1);
      // C# manda "ReferenciaDireccion"; el formulario usa
      // "referenciaDireccion". Paso la primera letra a minúscula.
      //
      // En React hiciste esto buscando sin importar mayúsculas.
      // Aquí conviertes la clave una vez y usas el nombre exacto.

      const control = this.formulario.get(campo);
      if (control) {
        control.setErrors({ servidor: mensajes[0] });
        // ═══ ESTA LÍNEA ES LO MEJOR DE REACTIVE FORMS ═══
        // Le pego al campo un error nuevo llamado "servidor".
        // Para el formulario es un error más, igual que un "required".
        //
        // Resultado: la MISMA plantilla que muestra los errores de
        // validación local muestra también los del backend, sin una
        // sola línea extra en el HTML. Las dos capas se ven igual.

        control.markAsTouched();
        // Si no lo marco como tocado, mensajeDe() lo ignoraría.
      }
    }
  }
}