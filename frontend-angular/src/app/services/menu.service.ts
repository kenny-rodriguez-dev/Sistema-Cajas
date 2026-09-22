import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { OpcionMenu } from '../models/modelos';

@Injectable({ providedIn: 'root' })
export class MenuService {
    private http = inject(HttpClient);

    obtenerMenu() {
        return this.http.get<OpcionMenu[]>(`${environment.apiUrl}/api/menu`);
        // El rol NO se manda como parámetro: el backend lo saca del token.
        // El interceptor ya puso la cabecera Authorization, así que este
        // servicio no sabe nada de tokens. Cada pieza hace una sola cosa.
    }
}