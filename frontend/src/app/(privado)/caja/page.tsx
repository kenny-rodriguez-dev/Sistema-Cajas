'use client';

import { useState, useEffect, useCallback } from 'react';
import { apiFetch } from '@/lib/api';

type MiCaja = {
    id: number;
    nombre: string;
    ocupadaPor: string | null;
    esMia: boolean;
};

type Turno = {
    id: number;
    codigo: string;
    tipoAtencion: string;
    caja: string;
    estado: string;
};

export default function CajaPage() {
    const [cajas, setCajas] = useState<MiCaja[]>([]);
    const [miSesionCajaId, setMiSesionCajaId] = useState<number | null>(null);
    const [turnos, setTurnos] = useState<Turno[]>([]);
    const [error, setError] = useState<string | null>(null);
    const [exito, setExito] = useState<string | null>(null);
    const [cargando, setCargando] = useState(true);
    const [ocupado, setOcupado] = useState(false);

    const cargar = useCallback(async () => {
        try {
            const [rCajas, rTurnos] = await Promise.all([
                apiFetch('/api/cajas/mis-cajas'),
                apiFetch('/api/turnos'),
            ]);

            if (rCajas.status === 403) { setError('Esta pantalla es solo para cajeros'); return; }

            if (rCajas.ok) {
                const datos = await rCajas.json();
                setCajas(datos.cajas);
                setMiSesionCajaId(datos.miSesionCajaId);
            }
            if (rTurnos.ok) setTurnos(await rTurnos.json());
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setCargando(false);
        }
    }, []);

    useEffect(() => { cargar(); }, [cargar]);

    async function accion(ruta: string, mensajeExito: string) {
        setOcupado(true);
        setError(null);
        setExito(null);
        try {
            const r = await apiFetch(ruta, { method: 'POST' });

            if (r.status === 403) { setError('No tienes permiso para esta acción'); return; }

            const datos = await r.json();
            if (!r.ok) { setError(datos.mensaje ?? 'No se pudo completar la acción'); return; }

            setExito(mensajeExito);
            await cargar();
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setOcupado(false);
        }
    }
    // Una sola función para abrir, cerrar y atender: las tres son un POST
    // sin cuerpo que después recarga. Escribir tres funciones casi idénticas
    // sería repetir el mismo código tres veces.

    if (cargando) return <main className="p-8 text-gray-500">Cargando...</main>;

    const turnosDeMiCaja = turnos.filter(
        (t) => t.estado === 'PEN' && cajas.find((c) => c.id === miSesionCajaId)?.nombre === t.caja
    );
    // Solo los pendientes de LA caja que tengo abierta.
    // El backend igual lo verifica al atender: si mando el id de un turno
    // de otra caja, responde 403. Capa 1 y capa 2, una vez más.

    return (
        <main className="p-8 max-w-4xl">
            <h1 className="text-2xl font-bold mb-6">Procesos de caja</h1>

            {error && <div className="mb-4 p-3 rounded bg-red-100 text-red-800">{error}</div>}
            {exito && <div className="mb-4 p-3 rounded bg-green-100 text-green-800">{exito}</div>}

            <h2 className="text-lg font-semibold mb-3">Mis cajas asignadas</h2>

            {cajas.length === 0 && (
                <div className="bg-amber-50 border border-amber-200 rounded p-4 text-sm text-amber-900 mb-6">
                    No tienes cajas asignadas. Un gestor debe asignarte una antes de que puedas operar.
                </div>
            )}

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-8">
                {cajas.map((c) => (
                    <div key={c.id} className="bg-white border rounded-lg p-5">
                        <div className="flex justify-between items-start mb-3">
                            <p className="font-semibold text-lg">{c.nombre}</p>
                            {c.esMia ? (
                                <span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded">
                                    La tienes abierta
                                </span>
                            ) : c.ocupadaPor ? (
                                <span className="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded">
                                    Ocupada por {c.ocupadaPor}
                                </span>
                            ) : (
                                <span className="text-xs bg-green-100 text-green-800 px-2 py-1 rounded">
                                    Libre
                                </span>
                            )}
                        </div>

                        {c.esMia ? (
                            <button
                                onClick={() => accion('/api/cajas/cerrar', 'Sesión cerrada')}
                                disabled={ocupado}
                                className="w-full bg-gray-700 text-white rounded py-2 text-sm font-medium disabled:bg-gray-400"
                            >
                                Cerrar sesión de caja
                            </button>
                        ) : (
                            <button
                                onClick={() => accion(`/api/cajas/${c.id}/abrir`, 'Sesión abierta')}
                                disabled={ocupado || c.ocupadaPor !== null || miSesionCajaId !== null}
                                className="w-full bg-blue-600 text-white rounded py-2 text-sm font-medium disabled:bg-gray-400 disabled:cursor-not-allowed"
                            >
                                {c.ocupadaPor ? 'Ocupada' : miSesionCajaId !== null ? 'Ya tienes otra abierta' : 'Abrir caja'}
                            </button>
                        )}
                        {/* El botón se apaga en TRES casos: si la caja está ocupada,
                si ya tengo otra abierta, o si hay una acción en curso.
                Es comodidad: el backend y el índice único de SQL Server
                siguen siendo lo que de verdad impide el doble uso. */}
                    </div>
                ))}
            </div>

            {miSesionCajaId !== null && (
                <>
                    <div className="flex justify-between items-center mb-3">
                        <h2 className="text-lg font-semibold">
                            Turnos pendientes ({turnosDeMiCaja.length})
                        </h2>
                        <button onClick={cargar} className="text-sm text-blue-600 underline">
                            Actualizar
                        </button>
                    </div>

                    {turnosDeMiCaja.length === 0 ? (
                        <div className="bg-white border rounded-lg p-6 text-center text-gray-400">
                            No hay turnos pendientes en tu caja
                        </div>
                    ) : (
                        <div className="space-y-2">
                            {turnosDeMiCaja.map((t) => (
                                <div key={t.id} className="bg-white border rounded-lg p-4 flex justify-between items-center">
                                    <div>
                                        <p className="font-mono font-bold text-xl">{t.codigo}</p>
                                        <p className="text-sm text-gray-500">{t.tipoAtencion}</p>
                                    </div>
                                    <button
                                        onClick={() => accion(`/api/turnos/${t.id}/atender`, `Turno ${t.codigo} atendido`)}
                                        disabled={ocupado}
                                        className="bg-green-600 text-white rounded px-5 py-2 text-sm font-medium disabled:bg-gray-400"
                                    >
                                        Atender
                                    </button>
                                </div>
                            ))}
                        </div>
                    )}
                </>
            )}
            {/* La lista de turnos solo aparece si tengo una caja abierta.
          Coincide con la regla del backend: atender sin sesión da 409. */}
        </main>
    );
}