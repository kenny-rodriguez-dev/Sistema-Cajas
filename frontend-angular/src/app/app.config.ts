import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),

    provideHttpClient(withInterceptors([authInterceptor])),
    // ↑ Aquí se ENGANCHA el interceptor. Sin esta línea el archivo
    //   existe pero nunca se ejecuta.
    //
    // Compáralo con tu Program.cs: builder.Services.AddCors(...) define
    // la regla y app.UseCors(...) la activa. Aquí es lo mismo en una
    // línea: se registra el HttpClient y se le engancha el interceptor.
    // El mismo patrón de registrar-y-activar, en otro framework.
  ],
};