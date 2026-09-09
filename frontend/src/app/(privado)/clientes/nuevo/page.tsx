'use client';

import { useState } from 'react';
import { apiFetch } from '@/lib/api';

type FormularioCliente = {
    nombres: string;
    apellidos: string;
    identificacion: string;
    correo: string;
    telefono: string;
    direccion: string;
    referenciaDireccion: string;
};

const VALORES_INICIALES: FormularioCliente = {
    nombres: '',
    apellidos: '',
    identificacion: '',
    correo: '',
    telefono: '',
    direccion: '',
    referenciaDireccion: '',
};

function Campo({
    etiqueta,
    valor,
    alCambiar,
    errores,
}: {
    etiqueta: string;
    valor: string;
    alCambiar: (nuevoValor: string) => void;
    errores: string[];
}) {
    return (
        <div className="mb-4">
            <label className="block text-sm font-medium mb-1">{etiqueta}</label>
            <input
                className="w-full border border-gray-300 rounded px-3 py-2 bg-white"
                value={valor}
                onChange={(e) => alCambiar(e.target.value)}
            />
            {errores.map((mensaje) => (
                <p key={mensaje} className="text-red-600 text-sm mt-1">
                    {mensaje}
                </p>
            ))}
        </div>
    );
}

export default function NuevoClientePage() {
    const [datos, setDatos] = useState<FormularioCliente>(VALORES_INICIALES);
    const [errores, setErrores] = useState<Record<string, string[]>>({});
    const [mensajeExito, setMensajeExito] = useState<string | null>(null);
    const [mensajeError, setMensajeError] = useState<string | null>(null);
    const [enviando, setEnviando] = useState(false);

    function manejarCambio(campo: keyof FormularioCliente, valor: string) {
        setDatos((anterior) => ({ ...anterior, [campo]: valor }));
    }

    function erroresDe(campo: string): string[] {
        const clave = Object.keys(errores).find(
            (k) => k.toLowerCase() === campo.toLowerCase()
        );
        return clave ? errores[clave] : [];
    }

    async function manejarEnvio(evento: React.FormEvent) {
        evento.preventDefault();

        setEnviando(true);
        setErrores({});
        setMensajeExito(null);
        setMensajeError(null);

        try {
            const respuesta = await apiFetch('/api/clientes', {
                method: 'POST',
                body: JSON.stringify(datos),
            });

            if (respuesta.status === 403) {
                setMensajeError('Tu rol no tiene permiso para registrar clientes');
                return;
                // NUEVO. Antes este caso no existía porque no había roles.
                // Ahora un admin que llegue aquí a la fuerza ve este mensaje,
                // no una pantalla rota.
            }

            if (respuesta.status === 400) {
                const problema = await respuesta.json();
                setErrores(problema.errors ?? {});
                setMensajeError('Revisa los campos marcados en rojo');
                return;
            }

            if (respuesta.status === 409) {
                const problema = await respuesta.json();
                setMensajeError(problema.mensaje);
                return;
            }

            if (!respuesta.ok) {
                setMensajeError(`Error inesperado del servidor (${respuesta.status})`);
                return;
            }

            const creado = await respuesta.json();
            setMensajeExito(`Cliente creado correctamente (id ${creado.id})`);
            setDatos(VALORES_INICIALES);

        } catch {
            setMensajeError('No se pudo conectar con el servidor');
        } finally {
            setEnviando(false);
        }
    }

    return (
        <main className="max-w-xl p-8">
            <h1 className="text-2xl font-bold mb-6">Nuevo cliente</h1>

            {mensajeExito && (
                <div className="mb-4 p-3 rounded bg-green-100 text-green-800">
                    {mensajeExito}
                </div>
            )}
            {mensajeError && (
                <div className="mb-4 p-3 rounded bg-red-100 text-red-800">
                    {mensajeError}
                </div>
            )}

            <form onSubmit={manejarEnvio}>
                <Campo etiqueta="Nombres" valor={datos.nombres}
                    alCambiar={(v) => manejarCambio('nombres', v)}
                    errores={erroresDe('nombres')} />
                <Campo etiqueta="Apellidos" valor={datos.apellidos}
                    alCambiar={(v) => manejarCambio('apellidos', v)}
                    errores={erroresDe('apellidos')} />
                <Campo etiqueta="Identificación" valor={datos.identificacion}
                    alCambiar={(v) => manejarCambio('identificacion', v)}
                    errores={erroresDe('identificacion')} />
                <Campo etiqueta="Correo" valor={datos.correo}
                    alCambiar={(v) => manejarCambio('correo', v)}
                    errores={erroresDe('correo')} />
                <Campo etiqueta="Teléfono" valor={datos.telefono}
                    alCambiar={(v) => manejarCambio('telefono', v)}
                    errores={erroresDe('telefono')} />
                <Campo etiqueta="Dirección" valor={datos.direccion}
                    alCambiar={(v) => manejarCambio('direccion', v)}
                    errores={erroresDe('direccion')} />
                <Campo etiqueta="Referencia de la dirección" valor={datos.referenciaDireccion}
                    alCambiar={(v) => manejarCambio('referenciaDireccion', v)}
                    errores={erroresDe('referenciaDireccion')} />

                <button
                    type="submit"
                    disabled={enviando}
                    className="w-full bg-blue-600 text-white rounded py-2 font-medium
                     disabled:bg-gray-400 disabled:cursor-not-allowed"
                >
                    {enviando ? 'Guardando...' : 'Guardar cliente'}
                </button>
            </form>
        </main>
    );
}