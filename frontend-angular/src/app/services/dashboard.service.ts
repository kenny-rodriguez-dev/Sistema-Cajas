import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { BienvenidaDto, DashboardDto } from '../models/modelos';

@Injectable({ providedIn: 'root' })
export class DashboardService {
    private http = inject(HttpClient);

    bienvenida() {
        return this.http.get<BienvenidaDto>(`${environment.apiUrl}/api/dashboard/bienvenida`);
    }

    indicadores(desde: string, hasta: string) {
        return this.http.get<DashboardDto>(`${environment.apiUrl}/api/dashboard`, {
            params: { desde, hasta },
        });
        // Ahora con su tipo real. Si en la plantilla escribes
        // datos.turnosAtendido (sin la "s"), el compilador te avisa
        // antes de que abras el navegador.
    }
}