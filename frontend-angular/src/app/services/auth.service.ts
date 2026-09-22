import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { RespuestaLogin } from '../models/modelos';
import { SesionService } from './sesion.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private http = inject(HttpClient);
    private sesionService = inject(SesionService);

    login(username: string, password: string) {
        return this.http
            .post<RespuestaLogin>(`${environment.apiUrl}/api/auth/login`, { username, password })
            // ═══ ESTO DEVUELVE UN OBSERVABLE, NO UNA PROMESA ═══
            //
            // Es la diferencia más grande con React, y la única de verdad
            // nueva. Un Observable es un FLUJO de valores en el tiempo;
            // una promesa es UN valor futuro.
            //
            // Lo más importante en la práctica: un Observable NO SE EJECUTA
            // hasta que alguien se suscribe. Si escribes esta línea y no
            // llamas a .subscribe(), la petición jamás sale.
            //
            // Es el error número uno de quien viene de fetch.
            .pipe(
                tap((respuesta) => {
                    this.sesionService.guardar({
                        token: respuesta.token,
                        username: respuesta.username,
                        rol: respuesta.rol,
                    });
                })
                // tap() = "haz esto con el valor que pasa, sin modificarlo".
                // Guardo la sesión aquí para que el componente no tenga que
                // acordarse de hacerlo.
            );
    }

    recuperar(correo: string) {
        return this.http.post<{ mensaje: string }>(
            `${environment.apiUrl}/api/auth/recuperar`, { correo }
        );
    }

    restablecer(token: string, nuevaPassword: string) {
        return this.http.post<{ mensaje: string }>(
            `${environment.apiUrl}/api/auth/restablecer`, { token, nuevaPassword }
        );
    }

    logout(): void {
        this.sesionService.borrar();
    }
}