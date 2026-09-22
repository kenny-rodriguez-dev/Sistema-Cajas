import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { CrearUsuarioDto, ResultadoCarga, UsuarioLista } from '../models/modelos';

type Mensaje = { mensaje: string };
// Un alias de tipo: evita repetir { mensaje: string } cinco veces.

@Injectable({ providedIn: 'root' })
export class UsuariosService {
    private http = inject(HttpClient);
    private url = `${environment.apiUrl}/api/usuarios`;

    listar() {
        return this.http.get<UsuarioLista[]>(this.url);
    }

    crear(dto: CrearUsuarioDto) {
        return this.http.post<Mensaje & { id: number }>(this.url, dto);
        // "Mensaje & { id: number }" = un objeto que tiene las dos cosas.
        // El & combina tipos.
    }

    aprobar(id: number) {
        return this.http.post<Mensaje>(`${this.url}/${id}/aprobar`, {});
        // POST sin datos: mando un objeto vacío como cuerpo.
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