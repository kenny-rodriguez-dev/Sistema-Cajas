'use client';

import { useState, useEffect, Suspense } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import Link from 'next/link';

function Formulario() {
    const parametros = useSearchParams();
    const router = useRouter();

    const token = parametros.get('token') ?? '';
    // Lee ?token=... de la URL. Es el que llegó en el enlace.

    const [password, setPassword] = useState('');
    const [confirmacion, setConfirmacion] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [listo, setListo] = useState(false);
    const [enviando, setEnviando] = useState(false);

    useEffect(() => {
        if (!token) setError('El enlace está incompleto. Solicita uno nuevo.');
    }, [token]);

    async function enviar(evento: React.FormEvent) {
        evento.preventDefault();

        if (password !== confirmacion) {
            setError('Las contraseñas no coinciden');
            return;
        }
        // Esta comprobación SOLO existe en el frontend, y está bien:
        // no es una regla de negocio, es evitar que el usuario se
        // equivoque al teclear. El backend solo recibe una contraseña.

        setEnviando(true);
        setError(null);

        try {
            const r = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/auth/restablecer`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ token, nuevaPassword: password }),
            });

            const datos = await r.json();

            if (r.status === 400) {
                setError(datos.errors
                    ? Object.values(datos.errors).flat().join(' ')
                    : datos.mensaje);
                return;
                // El 400 viene en dos formas: errores de validación del DTO
                // (con "errors") o el mensaje del token inválido. Los aplano
                // a un solo texto para mostrarlos juntos.
            }

            if (!r.ok) { setError(datos.mensaje ?? 'No se pudo restablecer'); return; }

            setListo(true);
            setTimeout(() => router.push('/login'), 3000);
            // Tres segundos para leer el mensaje y luego al login.
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setEnviando(false);
        }
    }

    if (listo) {
        return (
            <div className="w-full max-w-sm bg-white p-8 rounded-lg shadow text-center">
                <h1 className="text-xl font-bold mb-3">Contraseña actualizada</h1>
                <p className="text-sm text-gray-600">Te llevamos al inicio de sesión...</p>
            </div>
        );
    }

    return (
        <div className="w-full max-w-sm bg-white p-8 rounded-lg shadow">
            <h1 className="text-2xl font-bold mb-2 text-center">Nueva contraseña</h1>
            <p className="text-xs text-gray-500 mb-6 text-center">
                Al menos 8 caracteres, con una mayúscula y un número.
            </p>

            {error && (
                <div className="mb-4 p-3 rounded bg-red-100 text-red-800 text-sm">{error}</div>
            )}

            <form onSubmit={enviar}>
                <label className="block text-sm font-medium mb-1">Nueva contraseña</label>
                <input
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full border border-gray-300 rounded px-3 py-2 mb-4"
                    autoComplete="new-password"
                />

                <label className="block text-sm font-medium mb-1">Confirmar</label>
                <input
                    type="password"
                    value={confirmacion}
                    onChange={(e) => setConfirmacion(e.target.value)}
                    className="w-full border border-gray-300 rounded px-3 py-2 mb-4"
                    autoComplete="new-password"
                />

                <button
                    type="submit"
                    disabled={enviando || !token}
                    className="w-full bg-blue-600 text-white rounded py-2 font-medium disabled:bg-gray-400"
                >
                    {enviando ? 'Guardando...' : 'Cambiar contraseña'}
                </button>
            </form>

            <Link href="/login" className="block mt-4 text-center text-sm text-blue-600 underline">
                Volver al inicio de sesión
            </Link>
        </div>
    );
}

export default function RestablecerPage() {
    return (
        <main className="min-h-screen flex items-center justify-center bg-gray-50">
            <Suspense fallback={<div className="text-gray-500">Cargando...</div>}>
                <Formulario />
            </Suspense>
        </main>
    );
}
// ═══ POR QUÉ ESE <Suspense> ═══
//
// useSearchParams obliga a Next.js a esperar a conocer la URL, y eso
// solo pasa en el navegador. Sin envolverlo en Suspense, el build de
// producción FALLA con un error sobre prerenderizado.
//
// Suspense le dice: "mientras no sepas la URL, muestra esto".
// Es un error que solo aparece al compilar para producción, nunca
// con npm run dev. Bueno saberlo antes de que te muerda en Docker.