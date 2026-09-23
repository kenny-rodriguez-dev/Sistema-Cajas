import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import {
    ActualizarClienteDto, Cliente, CrearClienteDto, ResultadoCarga,
} from '../models/modelos';

type Mensaje = { mensaje: string };

@Injectable({ providedIn: 'root' })
export class ClientesService {
    private http = inject(HttpClient);
    private url = `${environment.apiUrl}/api/clientes`;

    listar(incluirInactivos: boolean) {
        return this.http.get<Cliente[]>(this.url, {
            params: { incluirInactivos },
            // params acepta booleanos y los convierte a "true"/"false".
        });
    }

    buscar(identificacion: string) {
        return this.http.get<Cliente>(`${this.url}/buscar`, {
            params: { identificacion },
            // params escapa el valor solo: el equivalente al
            // encodeURIComponent que en Next.js escribiste a mano.
        });
    }

    crear(datos: CrearClienteDto) {
        return this.http.post<Mensaje & { id: number }>(this.url, datos);
    }

    actualizar(id: number, datos: ActualizarClienteDto) {
        return this.http.put<Mensaje>(`${this.url}/${id}`, datos);
    }

    cambiarEstado(id: number) {
        return this.http.post<Mensaje>(`${this.url}/${id}/estado`, {});
    }

    cargaMasiva(archivo: File) {
        const formulario = new FormData();
        formulario.append('archivo', archivo);
        return this.http.post<ResultadoCarga>(`${this.url}/carga-masiva`, formulario);
    }
}