import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, of, tap } from 'rxjs';
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
            .pipe(
                tap((respuesta) => {
                    this.sesionService.guardar({
                        token: respuesta.token,
                        username: respuesta.username,
                        rol: respuesta.rol,
                    });
                })
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

    logout() {
        return this.http.post(`${environment.apiUrl}/api/auth/logout`, {}).pipe(
            catchError(() => of(null)),
            // Si el backend no responde, convierto el error en un valor normal
            // (null). Así el cierre de sesión local sigue adelante igual:
            // el usuario pidió salir y no debe quedar atrapado.

            tap(() => this.sesionService.borrar())
            // Recién después de la petición, borro el token local.
            // Mismo orden que en Next.js: primero avisar, después borrar.
            // El interceptor necesita el token para pegarlo en la cabecera.
        );
        // ═══ OJO: ESTO CAMBIÓ Y ES FÁCIL DE ROMPER ═══
        // Antes logout() borraba la sesión en el acto. Ahora devuelve un
        // Observable. Y un Observable NO HACE NADA hasta que alguien se
        // suscribe: si llamas this.auth.logout() sin .subscribe(), no sale
        // la petición Y TAMPOCO se borra la sesión. El botón parecería no
        // hacer nada.
        //
        // Es la regla de la primera sesión de Angular, aplicada a un lugar
        // donde un error sería silencioso.
    }
}