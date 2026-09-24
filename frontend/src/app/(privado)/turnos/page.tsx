'use client';

import { useState, useEffect, useCallback } from 'react';
import { apiFetch } from '@/lib/api';

type Caja = { id: number; nombre: string };
type TipoAtencion = { id: number; codigo: string; descripcion: string };
type Turno = {
    id: number;
    codigo: string;
    tipoAtencion: string;
    caja: string;
    estado: string;
    fechaHoraEmision: string;
    atendidoPor: string | null;
};

const ETIQUETAS: Record<string, { texto: string; clase: string }> = {
    PEN: { texto: 'Pendiente', clase: 'bg-amber-100 text-amber-800' },
    ATE: { texto: 'Atendido', clase: 'bg-green-100 text-green-800' },
    CAN: { texto: 'Cancelado', clase: 'bg-gray-100 text-gray-600' },
};
// Traduzco los códigos de 3 letras a algo legible.
// Una tabla al inicio del archivo es más limpia que un switch repetido
// dentro del JSX.

export default function TurnosPage() {
    const [cajas, setCajas] = useState<Caja[]>([]);
    const [tipos, setTipos] = useState<TipoAtencion[]>([]);
    const [turnos, setTurnos] = useState<Turno[]>([]);

    const [cajaId, setCajaId] = useState<number | ''>('');
    const [tipoId, setTipoId] = useState<number | ''>('');
    // El '' inicial representa "nada elegido todavía". Un <select> vacío
    // devuelve texto, no número, por eso el tipo acepta las dos cosas.

    const [ultimoCodigo, setUltimoCodigo] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [cargando, setCargando] = useState(true);
    const [generando, setGenerando] = useState(false);

    const cargarTurnos = useCallback(async () => {
        const r = await apiFetch('/api/turnos');
        if (r.ok) setTurnos(await r.json());
    }, []);

    useEffect(() => {
        // Promise.all lanza las tres peticiones A LA VEZ, no una tras otra.
        // Tres viajes en paralelo tardan lo que el más lento, no la suma.
        Promise.all([
            apiFetch('/api/cajas'),
            apiFetch('/api/turnos/tipos'),
            apiFetch('/api/turnos'),
        ])
            .then(async ([rCajas, rTipos, rTurnos]) => {
                if (rCajas.ok) setCajas(await rCajas.json());
                if (rTipos.ok) setTipos(await rTipos.json());
                if (rTurnos.ok) setTurnos(await rTurnos.json());
            })
            .catch(() => setError('No se pudieron cargar los datos'))
            .finally(() => setCargando(false));
    }, []);

    async function generar() {
        if (cajaId === '' || tipoId === '') {
            setError('Elige una caja y un tipo de atención');
            return;
        }

        setGenerando(true);
        setError(null);
        setUltimoCodigo(null);

        try {
            const r = await apiFetch('/api/turnos', {
                method: 'POST',
                body: JSON.stringify({ cajaId, tipoAtencionId: tipoId }),
            });

            if (r.status === 403) { setError('Solo un gestor puede generar turnos'); return; }

            const datos = await r.json();
            if (!r.ok) { setError(datos.mensaje ?? 'No se pudo generar el turno'); return; }

            setUltimoCodigo(datos.codigo);
            await cargarTurnos();
            // Recargo la lista en vez de agregarle el turno nuevo a mano.
            // Así lo que ves en pantalla es siempre lo que dice la base,
            // no una copia que podría desincronizarse.
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setGenerando(false);
        }
    }

    async function cancelar(t: Turno) {
        if (!window.confirm(`¿Cancelar el turno ${t.codigo}?`)) return;
        setError(null);

        try {
            const r = await apiFetch(`/api/turnos/${t.id}/cancelar`, { method: 'POST' });
            if (r.status === 403) { setError('Solo un gestor puede cancelar turnos'); return; }

            const d = await r.json();
            if (!r.ok) { setError(d.mensaje ?? 'No se pudo cancelar'); return; }

            await cargarTurnos();
            // No hace falta mensaje de éxito: la etiqueta del turno pasa
            // a "Cancelado" en la tabla, y esa es la confirmación visual.
        } catch {
            setError('No se pudo conectar con el servidor');
        }
    }

    if (cargando) return <main className="p-8 text-gray-500">Cargando...</main>;

    return (
        <main className="p-8 max-w-4xl">
            <h1 className="text-2xl font-bold mb-6">Asignación de turnos</h1>

            <div className="bg-white border rounded-lg p-5 mb-6">
                <div className="flex flex-wrap gap-4 items-end">
                    <div>
                        <label className="block text-xs text-gray-500 mb-1">Caja</label>
                        <select
                            value={cajaId}
                            onChange={(e) => setCajaId(e.target.value === '' ? '' : Number(e.target.value))}
                            className="border rounded px-3 py-2 text-sm min-w-40"
                        >
                            <option value="">Selecciona...</option>
                            {cajas.map((c) => (
                                <option key={c.id} value={c.id}>{c.nombre}</option>
                            ))}
                        </select>
                        {/* Un <select> siempre devuelve TEXTO, aunque el value sea un
                número. Por eso el Number(): sin él mandaría "1" en vez de 1
                y el backend lo rechazaría. */}
                    </div>

                    <div>
                        <label className="block text-xs text-gray-500 mb-1">Tipo de atención</label>
                        <select
                            value={tipoId}
                            onChange={(e) => setTipoId(e.target.value === '' ? '' : Number(e.target.value))}
                            className="border rounded px-3 py-2 text-sm min-w-56"
                        >
                            <option value="">Selecciona...</option>
                            {tipos.map((t) => (
                                <option key={t.id} value={t.id}>{t.codigo} — {t.descripcion}</option>
                            ))}
                        </select>
                    </div>

                    <button
                        onClick={generar}
                        disabled={generando}
                        className="bg-blue-600 text-white rounded px-5 py-2 text-sm font-medium disabled:bg-gray-400"
                    >
                        {generando ? 'Generando...' : 'Generar turno'}
                    </button>
                </div>

                {ultimoCodigo && (
                    <div className="mt-5 p-4 bg-green-50 border border-green-200 rounded text-center">
                        <p className="text-sm text-green-800">Turno generado</p>
                        <p className="text-4xl font-bold font-mono text-green-900 mt-1">{ultimoCodigo}</p>
                    </div>
                )}
                {/* Grande y en monoespaciada: es el número que el cliente tiene
            que leer en pantalla. font-mono hace que todos los dígitos
            ocupen lo mismo y el código no "baile" al cambiar. */}
            </div>

            {error && <div className="mb-4 p-3 rounded bg-red-100 text-red-800">{error}</div>}

            <div className="flex justify-between items-center mb-3">
                <h2 className="text-lg font-semibold">Turnos de hoy ({turnos.length})</h2>
                <button onClick={cargarTurnos} className="text-sm text-blue-600 underline">
                    Actualizar
                </button>
            </div>

            <table className="w-full bg-white border rounded-lg overflow-hidden text-sm">
                <thead className="bg-gray-100">
                    <tr>
                        <th className="text-left px-4 py-2">Código</th>
                        <th className="text-left px-4 py-2">Tipo</th>
                        <th className="text-left px-4 py-2">Caja</th>
                        <th className="text-left px-4 py-2">Estado</th>
                        <th className="text-left px-4 py-2">Atendido por</th>
                        <th className="text-left px-4 py-2">Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    {turnos.length === 0 && (
                        <tr><td colSpan={6} className="px-4 py-6 text-center text-gray-400">
                            Todavía no hay turnos hoy
                        </td></tr>
                    )}
                    {turnos.map((t) => {
                        const e = ETIQUETAS[t.estado] ?? { texto: t.estado, clase: 'bg-gray-100' };
                        return (
                            <tr key={t.id} className="border-t">
                                <td className="px-4 py-2 font-mono font-medium">{t.codigo}</td>
                                <td className="px-4 py-2">{t.tipoAtencion}</td>
                                <td className="px-4 py-2">{t.caja}</td>
                                <td className="px-4 py-2">
                                    <span className={`px-2 py-1 rounded text-xs ${e.clase}`}>{e.texto}</span>
                                </td>
                                <td className="px-4 py-2 text-gray-600">{t.atendidoPor ?? '—'}</td>
                                <td className="px-4 py-2">
                                    {t.estado === 'PEN' && (
                                        <button onClick={() => cancelar(t)} className="text-xs text-red-600 hover:underline">
                                            Cancelar
                                        </button>
                                    )}
                                </td>
                            </tr>
                        );
                    })}
                </tbody>
            </table>
        </main>
    );
}