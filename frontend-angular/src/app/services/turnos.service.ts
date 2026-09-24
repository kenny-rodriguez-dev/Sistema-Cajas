import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { CajaDetalle, MisCajasRespuesta, TipoAtencion, Turno } from '../models/modelos';

type Mensaje = { mensaje: string };

@Injectable({ providedIn: 'root' })
export class TurnosService {
    private http = inject(HttpClient);
    private api = environment.apiUrl;

    // ─── TURNOS ───
    listarTipos() {
        return this.http.get<TipoAtencion[]>(`${this.api}/api/turnos/tipos`);
    }

    listarDelDia() {
        return this.http.get<Turno[]>(`${this.api}/api/turnos`);
    }

    crear(cajaId: number, tipoAtencionId: number) {
        return this.http.post<{ id: number; codigo: string; mensaje: string }>(
            `${this.api}/api/turnos`, { cajaId, tipoAtencionId }
        );
    }

    atender(turnoId: number) {
        return this.http.post<Mensaje>(`${this.api}/api/turnos/${turnoId}/atender`, {});
    }

    cancelarTurno(turnoId: number) {
        return this.http.post<Mensaje>(`${this.api}/api/turnos/${turnoId}/cancelar`, {});
    }

    // ─── CAJAS ───
    listarCajas() {
        return this.http.get<CajaDetalle[]>(`${this.api}/api/cajas`);
        // Ahora tipado con la forma completa. La pantalla de turnos lo
        // recibe en un signal de CajaLista[] y funciona igual: un
        // CajaDetalle ES un CajaLista con campos de más.
    }

    asignarCajero(cajaId: number, usuarioId: number) {
        return this.http.post<Mensaje>(`${this.api}/api/cajas/${cajaId}/asignaciones`, { usuarioId });
    }

    quitarCajero(cajaId: number, usuarioId: number) {
        return this.http.delete<Mensaje>(`${this.api}/api/cajas/${cajaId}/asignaciones/${usuarioId}`);
        // http.delete, el verbo exacto. Sin cuerpo.
    }

    misCajas() {
        return this.http.get<MisCajasRespuesta>(`${this.api}/api/cajas/mis-cajas`);
        // Solo Cajero: sus cajas asignadas, no todas.
    }

    abrirCaja(cajaId: number) {
        return this.http.post<Mensaje>(`${this.api}/api/cajas/${cajaId}/abrir`, {});
    }

    cerrarCaja() {
        return this.http.post<Mensaje>(`${this.api}/api/cajas/cerrar`, {});
    }
}