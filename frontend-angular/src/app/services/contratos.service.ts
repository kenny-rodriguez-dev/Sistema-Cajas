import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { ContratoDto, FormaPagoDto, ServicioDto } from '../models/modelos';

type Mensaje = { mensaje: string };

@Injectable({ providedIn: 'root' })
export class ContratosService {
    private http = inject(HttpClient);
    private url = `${environment.apiUrl}/api/contratos`;

    servicios() {
        return this.http.get<ServicioDto[]>(`${this.url}/servicios`);
    }

    formasPago() {
        return this.http.get<FormaPagoDto[]>(`${this.url}/formas-pago`);
    }

    historial(clienteId: number) {
        return this.http.get<ContratoDto[]>(`${this.url}/cliente/${clienteId}`);
    }

    contratar(clienteId: number, servicioId: number, formaPagoId: number, mesesDuracion: number) {
        return this.http.post<Mensaje>(this.url, { clienteId, servicioId, formaPagoId, mesesDuracion });
    }

    cambiarServicio(contratoId: number, nuevoServicioId: number) {
        return this.http.post<Mensaje>(`${this.url}/${contratoId}/cambiar-servicio`, { nuevoServicioId });
    }

    cambiarFormaPago(contratoId: number, nuevaFormaPagoId: number) {
        return this.http.post<Mensaje>(`${this.url}/${contratoId}/cambiar-forma-pago`, { nuevaFormaPagoId });
    }

    cancelar(contratoId: number) {
        return this.http.post<Mensaje>(`${this.url}/${contratoId}/cancelar`, {});
    }
}