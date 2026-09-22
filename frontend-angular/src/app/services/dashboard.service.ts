import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { BienvenidaDto } from '../models/modelos';

@Injectable({ providedIn: 'root' })
export class DashboardService {
    private http = inject(HttpClient);

    bienvenida() {
        return this.http.get<BienvenidaDto>(`${environment.apiUrl}/api/dashboard/bienvenida`);
    }

    indicadores(desde: string, hasta: string) {
        return this.http.get<any>(`${environment.apiUrl}/api/dashboard`, {
            params: { desde, hasta },
            // "params" arma la URL con ?desde=...&hasta=... y ESCAPA los
            // valores por ti. Concatenar a mano es cómo se cuelan errores
            // cuando un valor trae espacios o signos raros.
        });
    }
}