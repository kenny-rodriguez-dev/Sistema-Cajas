import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { PagoDto } from '../models/modelos';

@Injectable({ providedIn: 'root' })
export class PagosService {
    private http = inject(HttpClient);
    private url = `${environment.apiUrl}/api/pagos`;
    // Un servicio por controlador del backend: el mapa es uno a uno.

    registrar(contratoId: number, periodo: string) {
        return this.http.post<{ mensaje: string }>(this.url, { contratoId, periodo });
        // Sin monto. El servidor lo pone.
    }

    historial(clienteId: number) {
        return this.http.get<PagoDto[]>(`${this.url}/cliente/${clienteId}`);
    }
}