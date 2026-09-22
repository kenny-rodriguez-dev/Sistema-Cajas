import { Component } from '@angular/core';

@Component({
  selector: 'app-en-construccion',
  standalone: true,
  template: `
    <main class="p-8">
      <h1 class="text-2xl font-bold mb-2">En construcción</h1>
      <p class="text-gray-600">Esta pantalla se construye en la siguiente sesión.</p>
    </main>
  `,
})
export class EnConstruccion { }