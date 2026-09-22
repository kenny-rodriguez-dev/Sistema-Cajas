import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-login',
    standalone: true,
    // standalone = sin NgModule. Es el default en Angular moderno.

    imports: [ReactiveFormsModule, RouterLink],
    // ═══ ESTO NO EXISTE EN REACT ═══
    // Un componente standalone declara qué necesita para funcionar.
    // Si uso [formGroup] en la plantilla, tengo que importar
    // ReactiveFormsModule aquí. Si olvido uno, el error dice
    // "no es una propiedad conocida de..." y significa esto.

    templateUrl: './login.html',
})
export class Login {
    private fb = inject(FormBuilder);
    private auth = inject(AuthService);
    private router = inject(Router);

    error = signal<string | null>(null);
    enviando = signal(false);

    formulario = this.fb.nonNullable.group({
        username: ['', [Validators.required]],
        password: ['', [Validators.required]],
    });
    // ═══ REACTIVE FORMS: LA MEJOR PIEZA DE ANGULAR ═══
    //
    // Esto reemplaza todos tus useState + onChange de React.
    // El formulario es un objeto que sabe sus valores, sus reglas,
    // si es válido y si el usuario ya tocó cada campo.
    //
    // El array es [valorInicial, [validadores]].
    //
    // nonNullable = los campos nunca serán null, solo texto vacío.
    // Evita tener que comprobar null por todos lados.
    //
    // En el login solo pido "requerido": validar el formato de la
    // contraseña aquí le regalaría las reglas a un atacante sin
    // necesidad de tener cuenta. Esa decisión ya la tomaste en el DTO.

    entrar(): void {
        if (this.formulario.invalid) {
            this.formulario.markAllAsTouched();
            // Marca todos los campos como "tocados" para que se muestren
            // los errores, aunque el usuario no haya escrito en ellos.
            this.error.set('Ingresa tu usuario y tu contraseña');
            return;
        }

        this.enviando.set(true);
        this.error.set(null);

        const { username, password } = this.formulario.getRawValue();

        this.auth.login(username, password).subscribe({
            // ═══ AQUÍ SE DISPARA LA PETICIÓN ═══
            // Sin este .subscribe(), nada sale. El observable estaba
            // preparado pero dormido.

            next: () => {
                this.router.navigate(['/bienvenida']);
                // next = llegó la respuesta y todo salió bien.
            },

            error: (e) => {
                this.error.set(
                    e.status === 401
                        ? (e.error?.mensaje ?? 'Usuario o contraseña incorrectos')
                        : 'No se pudo conectar con el servidor'
                );
                this.enviando.set(false);
                // error = falló. Aquí entran tanto los errores HTTP como
                // los fallos de red. A diferencia de fetch, que trata un 401
                // como "éxito" porque la respuesta llegó, Angular manda todos
                // los códigos de error aquí. Es más cómodo.
            },
        });
    }
}