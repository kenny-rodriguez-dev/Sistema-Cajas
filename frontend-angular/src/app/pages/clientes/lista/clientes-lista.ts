import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ClientesService } from '../../../services/clientes.service';
import { Cliente } from '../../../models/modelos';

@Component({
    selector: 'app-clientes-lista',
    imports: [ReactiveFormsModule, RouterLink],
    templateUrl: './clientes-lista.html',
})
export class ClientesLista {
    private fb = inject(FormBuilder);
    private servicio = inject(ClientesService);

    clientes = signal<Cliente[]>([]);
    incluirInactivos = signal(false);
    encontrado = signal<Cliente | null>(null);
    noExiste = signal(false);
    editando = signal<Cliente | null>(null);

    mensaje = signal<string | null>(null);
    error = signal<string | null>(null);
    cargando = signal(true);
    ocupado = signal(false);

    busqueda = this.fb.nonNullable.control('', [
        Validators.required,
        Validators.pattern(/^[0-9]{10,13}$/),
    ]);
    // ═══ UN FormControl SUELTO, SIN GRUPO ═══
    // Para un solo campo no hace falta armar un FormGroup.
    // En la plantilla se enlaza con [formControl] en vez de formControlName.

    edicion = this.fb.nonNullable.group({
        nombres: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
        apellidos: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
        correo: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],
        telefono: ['', [
            Validators.required, Validators.minLength(10), Validators.maxLength(13),
            Validators.pattern(/^09[0-9]+$/),
        ]],
        direccion: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(100)]],
        referenciaDireccion: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(100)]],
        // Sin identificación: igual que el DTO del backend.
    });

    campos = [
        { clave: 'nombres', etiqueta: 'Nombres' },
        { clave: 'apellidos', etiqueta: 'Apellidos' },
        { clave: 'correo', etiqueta: 'Correo' },
        { clave: 'telefono', etiqueta: 'Teléfono' },
        { clave: 'direccion', etiqueta: 'Dirección' },
        { clave: 'referenciaDireccion', etiqueta: 'Referencia de la dirección' },
    ];

    constructor() {
        this.cargar();
    }

    private limpiar(): void {
        this.mensaje.set(null);
        this.error.set(null);
    }

    cargar(): void {
        this.servicio.listar(this.incluirInactivos()).subscribe({
            next: (c) => { this.clientes.set(c); this.cargando.set(false); },
            error: (e: HttpErrorResponse) => {
                this.cargando.set(false);
                this.error.set(e.status === 403 ? 'Solo los cajeros gestionan clientes' : 'No se pudieron cargar los clientes');
            },
        });
    }

    alternarInactivos(): void {
        this.incluirInactivos.update((v) => !v);
        this.cargar();
    }

    buscar(): void {
        this.limpiar();
        this.encontrado.set(null);
        this.noExiste.set(false);

        if (this.busqueda.invalid) {
            this.error.set('La identificación debe tener entre 10 y 13 dígitos');
            return;
        }

        this.servicio.buscar(this.busqueda.value.trim()).subscribe({
            next: (c) => this.encontrado.set(c),
            error: (e: HttpErrorResponse) => {
                if (e.status === 404) { this.noExiste.set(true); return; }
                this.error.set(e.error?.mensaje ?? 'No se pudo realizar la búsqueda');
                // En Angular, un 404 cae en "error". Aquí no es un fallo:
                // es la respuesta "no existe", así que la trato aparte.
            },
        });
    }

    empezarEdicion(c: Cliente): void {
        this.limpiar();
        this.editando.set(c);
        this.edicion.reset({
            nombres: c.nombres,
            apellidos: c.apellidos,
            correo: c.correo,
            telefono: c.telefono,
            direccion: c.direccion,
            referenciaDireccion: c.referenciaDireccion,
        });
        // reset() con valores: carga los datos Y limpia el estado "tocado".
        // Así el formulario no aparece con errores rojos de antes.
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    cancelarEdicion(): void {
        this.editando.set(null);
    }

    mensajeDe(campo: string): string | null {
        const control = this.edicion.get(campo);
        if (!control || !control.touched || !control.errors) return null;
        const e = control.errors;
        if (e['servidor']) return e['servidor'];
        if (e['required']) return 'Este campo es obligatorio';
        if (e['minlength']) return `Debe tener al menos ${e['minlength'].requiredLength} caracteres`;
        if (e['maxlength']) return `Debe tener como máximo ${e['maxlength'].requiredLength} caracteres`;
        if (e['email']) return 'El correo no tiene un formato válido';
        if (e['pattern']) return 'Debe empezar con 09 y contener solo números';
        return null;
    }

    guardar(): void {
        const cliente = this.editando();
        if (!cliente) return;
        this.limpiar();

        if (this.edicion.invalid) {
            this.edicion.markAllAsTouched();
            this.error.set('Revisa los campos marcados en rojo');
            return;
        }

        this.ocupado.set(true);

        this.servicio.actualizar(cliente.id, this.edicion.getRawValue()).subscribe({
            next: (r) => {
                this.mensaje.set(r.mensaje);
                this.editando.set(null);
                this.encontrado.set(null);
                this.ocupado.set(false);
                this.cargar();
            },
            error: (e: HttpErrorResponse) => {
                this.ocupado.set(false);
                if (e.status === 400 && e.error?.errors) {
                    for (const [clave, mensajes] of Object.entries(e.error.errors as Record<string, string[]>)) {
                        const campo = clave.charAt(0).toLowerCase() + clave.slice(1);
                        const control = this.edicion.get(campo);
                        if (control) {
                            control.setErrors({ servidor: mensajes[0] });
                            control.markAsTouched();
                        }
                    }
                    this.error.set('El servidor rechazó algunos campos');
                    return;
                }
                this.error.set(e.error?.mensaje ?? 'No se pudo guardar');
            },
        });
    }

    cambiarEstado(c: Cliente): void {
        const accion = c.activo ? 'desactivar' : 'activar';
        if (!window.confirm(`¿Seguro que quieres ${accion} a ${c.nombres} ${c.apellidos}?`)) return;

        this.limpiar();
        this.ocupado.set(true);

        this.servicio.cambiarEstado(c.id).subscribe({
            next: (r) => {
                this.mensaje.set(r.mensaje);
                this.encontrado.set(null);
                this.ocupado.set(false);
                this.cargar();
            },
            error: (e: HttpErrorResponse) => {
                this.ocupado.set(false);
                this.error.set(e.error?.mensaje ?? 'No se pudo cambiar el estado');
                // Aquí llega "tiene un contrato vigente".
            },
        });
    }
}