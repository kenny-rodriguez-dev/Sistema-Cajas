import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { CrearClienteDto, ResultadoCarga } from '../models/modelos';

@Injectable({ providedIn: 'root' })
export class ClientesService {
    private http = inject(HttpClient);
    private url = `${environment.apiUrl}/api/clientes`;
    // La URL base una sola vez. Los métodos solo agregan el final.

    crear(datos: CrearClienteDto) {
        return this.http.post<{ id: number; mensaje: string }>(this.url, datos);
        // HttpClient convierte el objeto a JSON y pone el Content-Type
        // solo. No hay JSON.stringify como en fetch.
    }

    cargaMasiva(archivo: File) {
        const formulario = new FormData();
        formulario.append('archivo', archivo);
        // 'archivo' tiene que coincidir con el parámetro IFormFile de C#.
        // Mismo nombre que usaste en React y en Postman.

        return this.http.post<ResultadoCarga>(`${this.url}/carga-masiva`, formulario);
        // ═══ AQUÍ NO HAY BUG ═══
        // En React tuviste que agregar un "if (!esFormData)" al interceptor
        // para no pisar el Content-Type. Aquí no hace falta:
        //   1. Tu interceptor de Angular solo toca Authorization
        //   2. HttpClient detecta que el cuerpo es FormData y deja que el
        //      navegador arme el Content-Type con su boundary
        // El framework resolvió lo que en React resolviste tú a mano.
    }
}