'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { guardarSesion } from '@/lib/sesion';
import Link from 'next/link';
// El "@/" es un atajo que apunta a la carpeta src.
// Evita rutas horribles como '../../../lib/sesion'.

export default function LoginPage() {
    const router = useRouter();
    // Para navegar por código. Es el equivalente de Router en Angular.

    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [enviando, setEnviando] = useState(false);

    async function manejarEnvio(evento: React.FormEvent) {
        evento.preventDefault();

        // ═══ CAPA 1: validar ANTES de gastar un viaje al servidor ═══
        // "Validar que los datos sean ingresados antes de consumir el servicio",
        // literalmente lo que pide el enunciado.
        if (!username.trim() || !password.trim()) {
            setError('Ingresa tu usuario y tu contraseña');
            return;
            // .trim() quita espacios de los extremos: así "   " no cuenta
            // como texto válido.
        }

        setEnviando(true);
        setError(null);

        try {
            const respuesta = await fetch(
                `${process.env.NEXT_PUBLIC_API_URL}/api/auth/login`,
                {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username, password }),
                }
            );
            // OJO: aquí uso fetch normal, NO apiFetch.
            // Tiene sentido: todavía no tengo token. Es la única petición
            // del sistema que no lo necesita.

            if (respuesta.status === 401) {
                const problema = await respuesta.json();
                setError(problema.mensaje);
                return;
            }

            if (!respuesta.ok) {
                setError('No se pudo iniciar sesión. Intenta de nuevo.');
                return;
            }

            const datos = await respuesta.json();

            guardarSesion({
                token: datos.token,
                username: datos.username,
                rol: datos.rol,
            });

            router.push('/bienvenida');

        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setEnviando(false);
        }
    }

    return (
        <main className="min-h-screen flex items-center justify-center bg-gray-50">
            <div className="w-full max-w-sm bg-white p-8 rounded-lg shadow">
                <h1 className="text-2xl font-bold mb-6 text-center">Iniciar sesión</h1>

                {error && (
                    <div className="mb-4 p-3 rounded bg-red-100 text-red-800 text-sm">
                        {error}
                    </div>
                )}

                <form onSubmit={manejarEnvio}>
                    <div className="mb-4">
                        <label className="block text-sm font-medium mb-1">Usuario</label>
                        <input
                            className="w-full border border-gray-300 rounded px-3 py-2"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            autoComplete="username"
                        />
                    </div>

                    <div className="mb-6">
                        <label className="block text-sm font-medium mb-1">Contraseña</label>
                        <input
                            type="password"
                            className="w-full border border-gray-300 rounded px-3 py-2"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            autoComplete="current-password"
                        />
                        {/* type="password" hace que se vean puntitos. Es solo visual:
                el valor viaja igual. La seguridad real la da HTTPS. */}
                    </div>

                    <button
                        type="submit"
                        disabled={enviando}
                        className="w-full bg-blue-600 text-white rounded py-2 font-medium
                       disabled:bg-gray-400 disabled:cursor-not-allowed"
                    >
                        {enviando ? 'Entrando...' : 'Entrar'}
                    </button>
                    <Link href="/recuperar" className="block mt-4 text-center text-sm text-blue-600 underline">
                        ¿Olvidaste tu contraseña?
                    </Link>
                </form>

                <p className="mt-4 text-xs text-gray-500 text-center">
                    admin0001 / Admin1234
                </p>
                {/* Recordatorio de desarrollo. Bórralo antes de mostrarle
            esto a cualquiera. */}
            </div>
        </main>
    );
}
