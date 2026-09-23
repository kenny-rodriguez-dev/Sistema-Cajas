import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, forkJoin } from 'rxjs';
import { ClientesService } from '../../services/clientes.service';
import { ContratosService } from '../../services/contratos.service';
import { PagosService } from '../../services/pagos.service';
import { TurnosService } from '../../services/turnos.service';
import { Cliente, ContratoDto, FormaPagoDto, PagoDto, ServicioDto } from '../../models/modelos';

@Component({
    selector: 'app-atencion',
    imports: [RouterLink, DatePipe, CurrencyPipe],
    // CurrencyPipe: el "| currency" de la plantilla. Formatea dinero
    // con símbolo y dos decimales sin que lo escribas a mano.
    templateUrl: './atencion.html',
})
export class Atencion {
    private clientesService = inject(ClientesService);
    private contratosService = inject(ContratosService);
    private pagosService = inject(PagosService);
    private turnosService = inject(TurnosService);

    servicios = signal<ServicioDto[]>([]);
    formasPago = signal<FormaPagoDto[]>([]);
    cajaAbierta = signal(false);

    cedula = signal('');
    cliente = signal<Cliente | null>(null);
    noExiste = signal(false);
    contratos = signal<ContratoDto[]>([]);
    pagos = signal<PagoDto[]>([]);

    // ═══ SIGNALS EN VEZ DE REACTIVE FORMS ═══
    // Son seis campos sueltos, cada uno en un panel distinto, y casi sin
    // reglas: la validación de verdad la hace el backend. Un FormGroup por
    // cada panel sería más estructura que ayuda.
    // Reactive Forms rinde cuando hay muchas reglas sobre un mismo
    // formulario, como en Nuevo cliente. Aquí un signal alcanza.
    nuevoServicio = signal(0);
    nuevaForma = signal(0);
    meses = signal(12);
    periodo = signal(new Date().toISOString().slice(0, 7));
    cambioServicio = signal(0);
    cambioForma = signal(0);

    mensaje = signal<string | null>(null);
    error = signal<string | null>(null);
    ocupado = signal(false);

    // ═══ TRES computed ENCADENADOS, UNO DE ELLOS CON LA ENTRADA DEL USUARIO ═══
    contratoActivo = computed(() =>
        this.contratos().find((c) => c.estado === 'VIG' || c.estado === 'REN') ?? null
    );

    periodosPagados = computed(() => new Set(this.pagos().map((p) => p.periodo)));

    periodoYaPagado = computed(() => this.periodosPagados().has(this.periodo()));
    // Este depende de lo que el usuario elige en el selector de mes.
    // Cambia el mes y el aviso de "ya pagado" aparece o desaparece solo,
    // sin un solo manejador de eventos. Aquí se ve por qué periodo
    // tenía que ser un signal: un control de formulario no avisaría.

    servicioElegido = computed(() =>
        this.servicios().find((s) => s.id === this.nuevoServicio()) ?? null
    );

    constructor() {
        forkJoin({
            servicios: this.contratosService.servicios(),
            formas: this.contratosService.formasPago(),
            misCajas: this.turnosService.misCajas(),
        }).subscribe({
            next: (r) => {
                this.servicios.set(r.servicios);
                this.formasPago.set(r.formas);
                this.cajaAbierta.set(r.misCajas.miSesionCajaId !== null);
            },
            error: () => this.error.set('No se pudieron cargar los catálogos'),
        });
    }

    // Dos ayudantes para leer lo que escribe el usuario en la plantilla.
    texto(e: Event): string {
        return (e.target as HTMLInputElement).value;
    }
    numero(e: Event): number {
        return Number((e.target as HTMLSelectElement).value);
    }

    private limpiar(): void {
        this.mensaje.set(null);
        this.error.set(null);
    }

    private cargarCliente(id: number): void {
        forkJoin({
            contratos: this.contratosService.historial(id),
            pagos: this.pagosService.historial(id),
        }).subscribe({
            next: (r) => {
                this.contratos.set(r.contratos);
                this.pagos.set(r.pagos);
            },
        });
    }

    buscar(): void {
        this.limpiar();
        this.cliente.set(null);
        this.noExiste.set(false);
        this.contratos.set([]);
        this.pagos.set([]);

        const valor = this.cedula().trim();
        if (!/^[0-9]{10,13}$/.test(valor)) {
            this.error.set('La identificación debe tener entre 10 y 13 dígitos');
            return;
        }

        this.clientesService.buscar(valor).subscribe({
            next: (c) => {
                this.cliente.set(c);
                this.cargarCliente(c.id);
            },
            error: (e: HttpErrorResponse) => {
                if (e.status === 404) { this.noExiste.set(true); return; }
                this.error.set('No se pudo buscar');
            },
        });
    }

    private ejecutar(accion: Observable<{ mensaje: string }>): void {
        const c = this.cliente();
        if (!c) return;
        this.ocupado.set(true);
        this.limpiar();

        accion.subscribe({
            next: (r) => {
                this.mensaje.set(r.mensaje);
                this.nuevoServicio.set(0);
                this.nuevaForma.set(0);
                this.cambioServicio.set(0);
                this.cambioForma.set(0);
                this.ocupado.set(false);
                this.cargarCliente(c.id);
            },
            error: (e: HttpErrorResponse) => {
                this.ocupado.set(false);
                const errores = e.error?.errors;
                this.error.set(
                    errores
                        ? Object.values(errores as Record<string, string[]>).flat().join(' ')
                        : (e.error?.mensaje ?? 'No se pudo completar')
                );
            },
        });
    }

    contratar(): void {
        const c = this.cliente();
        if (!c || !this.nuevoServicio() || !this.nuevaForma()) {
            this.error.set('Elige un servicio y una forma de pago');
            return;
        }
        this.ejecutar(this.contratosService.contratar(c.id, this.nuevoServicio(), this.nuevaForma(), this.meses()));
    }

    pagar(): void {
        const contrato = this.contratoActivo();
        if (!contrato) return;
        this.ejecutar(this.pagosService.registrar(contrato.id, this.periodo()));
    }

    cambiarServicio(): void {
        const contrato = this.contratoActivo();
        if (!contrato || !this.cambioServicio()) { this.error.set('Elige el nuevo plan'); return; }
        this.ejecutar(this.contratosService.cambiarServicio(contrato.id, this.cambioServicio()));
    }

    cambiarForma(): void {
        const contrato = this.contratoActivo();
        if (!contrato || !this.cambioForma()) { this.error.set('Elige la nueva forma de pago'); return; }
        this.ejecutar(this.contratosService.cambiarFormaPago(contrato.id, this.cambioForma()));
    }

    cancelar(): void {
        const contrato = this.contratoActivo();
        if (!contrato) return;
        if (!window.confirm('¿Cancelar el contrato? Su fecha de fin pasará a ser hoy.')) return;
        this.ejecutar(this.contratosService.cancelar(contrato.id));
    }

    claseEstado(estado: string): string {
        return {
            VIG: 'bg-green-100 text-green-800',
            REN: 'bg-blue-100 text-blue-800',
            SUS: 'bg-gray-100 text-gray-600',
            CAN: 'bg-red-100 text-red-700',
        }[estado] ?? 'bg-gray-100';
    }

    textoEstado(estado: string): string {
        return { VIG: 'Vigente', REN: 'Renovado', SUS: 'Sustituido', CAN: 'Cancelado' }[estado] ?? estado;
    }
}