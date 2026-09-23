'use client';

import { useState, useEffect, useCallback } from 'react';
import { apiFetch } from '@/lib/api';

type CajeroAsignado = { id: number; username: string };

type Caja = {
    id: number;
    nombre: string;
    cajerosAsignados: CajeroAsignado[];
    ocupadaPor: string | null;
};

type Usuario = {
    id: number;
    username: string;
    rol: string;
    activo: boolean;
    aprobado: boolean;
};

export default function CajasPage() {
    const [cajas, setCajas] = useState<Caja[]>([]);
    const [cajeros, setCajeros] = useState<Usuario[]>([]);

    const [seleccion, setSeleccion] = useState<Record<number, number | ''>>({});
    // Hay UN desplegable por caja, así que guardo la elección de cada
    // uno por separado, indexada por el id de la caja:
    //   { 1: 4, 2: '' }  ->  en la caja 1 eligió al usuario 4,
    //                        en la caja 2 no eligió nada

    const [mensaje, setMensaje] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [cargando, setCargando] = useState(true);
    const [ocupado, setOcupado] = useState(false);

    const cargar = useCallback(async () => {
        try {
            const [rCajas, rUsuarios] = await Promise.all([
                apiFetch('/api/cajas'),
                apiFetch('/api/usuarios'),
            ]);
            // Reutilizo el endpoint de usuarios que ya existe: el gestor
            // tiene permiso para leerlo. No hizo falta crear uno nuevo.

            if (rCajas.status === 403) {
                setError('Solo un gestor puede asignar cajas');
                return;
            }

            if (rCajas.ok) setCajas(await rCajas.json());

            if (rUsuarios.ok) {
                const todos: Usuario[] = await rUsuarios.json();
                setCajeros(todos.filter((u) => u.rol === 'Cajero' && u.activo && u.aprobado));
                // Solo cajeros activos y aprobados. Asignar a uno inactivo o
                // pendiente sería darle una caja a quien no puede entrar.
            }
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setCargando(false);
        }
    }, []);

    useEffect(() => { cargar(); }, [cargar]);

    async function ejecutar(ruta: string, metodo: string, cuerpo?: object) {
        setOcupado(true);
        setError(null);
        setMensaje(null);

        try {
            const r = await apiFetch(ruta, {
                method: metodo,
                body: cuerpo ? JSON.stringify(cuerpo) : undefined,
            });

            if (r.status === 403) { setError('No tienes permiso para esta acción'); return; }

            const datos = await r.json();
            if (!r.ok) { setError(datos.mensaje ?? 'No se pudo completar'); return; }

            setMensaje(datos.mensaje);
            setSeleccion({});
            await cargar();
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setOcupado(false);
        }
    }

    function asignar(cajaId: number) {
        const usuarioId = seleccion[cajaId];
        if (!usuarioId) { setError('Elige un cajero primero'); return; }
        ejecutar(`/api/cajas/${cajaId}/asignaciones`, 'POST', { usuarioId });
    }

    function quitar(cajaId: number, cajero: CajeroAsignado) {
        if (!window.confirm(`¿Quitar a ${cajero.username} de esta caja?`)) return;
        // Confirmación antes de una acción que quita permisos. Un clic
        // por error dejaría a un cajero sin poder trabajar.
        ejecutar(`/api/cajas/${cajaId}/asignaciones/${cajero.id}`, 'DELETE');
    }

    if (cargando) return <main className="p-8 text-gray-500">Cargando...</main>;

    return (
        <main className="p-8 max-w-4xl">
            <h1 className="text-2xl font-bold mb-2">Asignación de cajas</h1>
            <p className="text-sm text-gray-600 mb-6">
                Cada caja admite un máximo de 2 cajeros, y solo uno puede operarla a la vez.
            </p>

            {mensaje && <div className="mb-4 p-3 rounded bg-green-100 text-green-800">{mensaje}</div>}
            {error && <div className="mb-4 p-3 rounded bg-red-100 text-red-800">{error}</div>}

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {cajas.map((caja) => {
                    const idsAsignados = new Set(caja.cajerosAsignados.map((c) => c.id));
                    const disponibles = cajeros.filter((c) => !idsAsignados.has(c.id));
                    const llena = caja.cajerosAsignados.length >= 2;
                    // Un Set para buscar rápido si un id ya está asignado.
                    // En el desplegable solo aparecen los que NO están en esta caja.

                    return (
                        <div key={caja.id} className="bg-white border rounded-lg p-5">
                            <div className="flex justify-between items-start mb-4">
                                <div>
                                    <p className="font-semibold text-lg">{caja.nombre}</p>
                                    <p className="text-xs text-gray-500">
                                        {caja.cajerosAsignados.length} / 2 cajeros
                                    </p>
                                </div>
                                {caja.ocupadaPor ? (
                                    <span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded">
                                        Operando: {caja.ocupadaPor}
                                    </span>
                                ) : (
                                    <span className="text-xs bg-green-100 text-green-800 px-2 py-1 rounded">
                                        Libre
                                    </span>
                                )}
                            </div>

                            <div className="space-y-2 mb-4">
                                {caja.cajerosAsignados.length === 0 && (
                                    <p className="text-sm text-gray-400">Sin cajeros asignados</p>
                                )}
                                {caja.cajerosAsignados.map((c) => (
                                    <div key={c.id} className="flex justify-between items-center bg-gray-50 rounded px-3 py-2">
                                        <span className="text-sm">{c.username}</span>
                                        <button
                                            onClick={() => quitar(caja.id, c)}
                                            disabled={ocupado}
                                            className="text-xs text-red-600 hover:underline disabled:text-gray-400"
                                        >
                                            Quitar
                                        </button>
                                    </div>
                                ))}
                            </div>

                            {llena ? (
                                <p className="text-xs text-gray-500 text-center py-2">
                                    Caja completa. Quita a un cajero para asignar otro.
                                </p>
                            ) : (
                                <div className="flex gap-2">
                                    <select
                                        value={seleccion[caja.id] ?? ''}
                                        onChange={(e) =>
                                            setSeleccion({
                                                ...seleccion,
                                                [caja.id]: e.target.value === '' ? '' : Number(e.target.value),
                                            })
                                        }
                                        className="flex-1 border rounded px-3 py-2 text-sm"
                                    >
                                        <option value="">Elige un cajero...</option>
                                        {disponibles.map((c) => (
                                            <option key={c.id} value={c.id}>{c.username}</option>
                                        ))}
                                    </select>
                                    <button
                                        onClick={() => asignar(caja.id)}
                                        disabled={ocupado}
                                        className="bg-blue-600 text-white rounded px-4 py-2 text-sm font-medium disabled:bg-gray-400"
                                    >
                                        Asignar
                                    </button>
                                </div>
                            )}
                            {/* El formulario desaparece con la caja llena. Es comodidad:
                  el backend igual rechaza la tercera asignación con 409. */}
                        </div>
                    );
                })}
            </div>
        </main>
    );
}