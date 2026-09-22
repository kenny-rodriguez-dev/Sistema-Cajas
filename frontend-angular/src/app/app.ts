import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App { }
// Quité el signal "title" que venía de ejemplo. No lo usamos.
// Este componente es solo el contenedor raíz: su único trabajo
// es tener el hueco donde el router pinta cada pantalla.