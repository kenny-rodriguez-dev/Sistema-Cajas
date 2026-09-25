import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import {
    ActualizarUsuarioDto, CrearUsuarioDto, ResultadoCarga, UsuarioLista,
} from '../models/modelos';

type Mensaje = { mensaje: string };

@Injectable({ providedIn: 'root' })
export class UsuariosService {
    private http = inject(HttpClient);
    private url = `${environment.apiUrl}/api/usuarios`;

    listar() {
        return this.http.get<UsuarioLista[]>(this.url);
    }

    crear(dto: CrearUsuarioDto) {
        return this.http.post<Mensaje & { id: number }>(this.url, dto);
    }

    actualizar(id: number, dto: ActualizarUsuarioDto) {
        return this.http.put<Mensaje>(`${this.url}/${id}`, dto);
    }

    aprobar(id: number) {
        return this.http.post<Mensaje>(`${this.url}/${id}/aprobar`, {});
    }

    cambiarEstado(id: number) {
        return this.http.post<Mensaje>(`${this.url}/${id}/estado`, {});
    }

    desbloquear(id: number) {
        return this.http.post<Mensaje>(`${this.url}/${id}/desbloquear`, {});
    }

    cargaMasiva(archivo: File) {
        const formulario = new FormData();
        formulario.append('archivo', archivo);
        return this.http.post<ResultadoCarga>(`${this.url}/carga-masiva`, formulario);
    }

    cerrarSesiones(id: number) {
        return this.http.post<Mensaje>(`${this.url}/${id}/cerrar-sesiones`, {});
    }
}