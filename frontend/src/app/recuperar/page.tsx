'use client';

import { useState } from 'react';
import Link from 'next/link';

export default function RecuperarPage() {
    const [correo, setCorreo] = useState('');
    const [enviado, setEnviado] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [enviando, setEnviando] = useState(false);

    async function enviar(evento: React.FormEvent) {
        evento.preventDefault();

        if (!correo.trim()) { setError('Ingresa tu correo'); return; }

        setEnviando(true);
        setError(null);

        try {
            const r = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/auth/recuperar`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ correo }),
            });
            // fetch normal y no apiFetch: no hay token que adjuntar.
            // Igual que en el login.

            if (!r.ok) { setError('No se pudo procesar la solicitud'); return; }

            setEnviado(true);
            // Muestro el mensaje de éxito SIEMPRE que la petición funcione,
            // exista o no el correo. Es la misma decisión del backend,
            // reflejada en la interfaz: si aquí dijera "correo no encontrado",
            // habría tirado por la borda toda la protección del servidor.
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setEnviando(false);
        }
    }

    return (
        <main className="min-h-screen flex items-center justify-center bg-gray-50">
            <div className="w-full max-w-sm bg-white p-8 rounded-lg shadow">
                <h1 className="text-2xl font-bold mb-2 text-center">Recuperar contraseña</h1>

                {enviado ? (
                    <>
                        <div className="mt-4 p-4 rounded bg-green-50 border border-green-200 text-sm text-green-900">
                            Si el correo está registrado, recibirás instrucciones para
                            restablecer tu contraseña. El enlace expira en 15 minutos.
                        </div>
                        <p className="mt-4 text-xs text-gray-500 text-center">
                            En desarrollo, el enlace se imprime en la consola del backend.
                        </p>
                        <Link href="/login" className="block mt-4 text-center text-sm text-blue-600 underline">
                            Volver al inicio de sesión
                        </Link>
                    </>
                ) : (
                    <>
                        <p className="text-sm text-gray-600 mb-6 text-center">
                            Ingresa tu correo y te enviaremos un enlace para crear una nueva contraseña.
                        </p>

                        {error && (
                            <div className="mb-4 p-3 rounded bg-red-100 text-red-800 text-sm">{error}</div>
                        )}

                        <form onSubmit={enviar}>
                            <label className="block text-sm font-medium mb-1">Correo</label>
                            <input
                                type="email"
                                value={correo}
                                onChange={(e) => setCorreo(e.target.value)}
                                className="w-full border border-gray-300 rounded px-3 py-2 mb-4"
                                autoComplete="email"
                            />

                            <button
                                type="submit"
                                disabled={enviando}
                                className="w-full bg-blue-600 text-white rounded py-2 font-medium disabled:bg-gray-400"
                            >
                                {enviando ? 'Enviando...' : 'Enviar enlace'}
                            </button>
                        </form>

                        <Link href="/login" className="block mt-4 text-center text-sm text-blue-600 underline">
                            Volver al inicio de sesión
                        </Link>
                    </>
                )}
            </div>
        </main>
    );
}
