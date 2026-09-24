'use client';

import { useState, useCallback, useEffect } from 'react';
import { apiFetch } from '@/lib/api';

type IndicadoresUsuarios = {
    total: number;
    activos: number;
    inactivos: number;
    pendientesAprobacion: number;
    conSesionAbierta: number;
    gestores: number;
    cajeros: number;
    bloqueados: number;
};

type IndicadorCaja = {
    caja: string;
    cajerosAsignados: number;
    turnosAtendidos: number;
    ocupadaPor: string | null;
};

type Dashboard = {
    usuarios: IndicadoresUsuarios;
    cajas: IndicadorCaja[];
    turnosEmitidos: number;
    turnosAtendidos: number;
    turnosPendientes: number;
    contratosActivos: number;
};

function hoyTexto(): string {
    return new Date().toISOString().slice(0, 10);
    // toISOString da "2026-09-11T21:13:23.000Z".
    // slice(0,10) se queda con "2026-09-11", que es el formato
    // que espera un <input type="date">.
}

function Indicador({
    etiqueta,
    valor,
    color = 'text-gray-900',
}: {
    etiqueta: string;
    valor: number;
    color?: string;
}) {
    return (
        <div className="bg-white p-4 rounded-lg border">
            <p className="text-xs text-gray-500 uppercase tracking-wide">{etiqueta}</p>
            <p className={`text-2xl font-bold mt-1 ${color}`}>{valor}</p>
        </div>
    );
}

export default function DashboardPage() {
    const [desde, setDesde] = useState(hoyTexto());
    const [hasta, setHasta] = useState(hoyTexto());
    const [datos, setDatos] = useState<Dashboard | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [cargando, setCargando] = useState(false);

    const cargar = useCallback(async () => {
        setCargando(true);
        setError(null);

        const formatoValido = /^\d{4}-\d{2}-\d{2}$/;
        // 4 dígitos, guion, 2 dígitos, guion, 2 dígitos. Nada más.

        if (!formatoValido.test(desde) || !formatoValido.test(hasta)) {
            setError('Selecciona fechas válidas');
            setCargando(false);
            return;
        }

        if (desde > hasta) {
            setError('La fecha inicial no puede ser mayor que la final');
            setCargando(false);
            return;
        }
        // Comparar las fechas como TEXTO funciona porque el formato
        // AAAA-MM-DD ordena igual alfabéticamente que cronológicamente.
        // Es una de las razones por las que ese formato es el estándar.

        try {
            const respuesta = await apiFetch(
                `/api/dashboard?desde=${desde}&hasta=${hasta}`
            );

            if (respuesta.status === 403) {
                setError('Solo los administradores pueden ver el dashboard');
                return;
            }
            // El status ANTES de leer el cuerpo. La lección del bug
            // de la carga masiva, aplicada desde el principio esta vez.

            if (!respuesta.ok) {
                setError(`El servidor respondió ${respuesta.status}`);
                return;
            }

            setDatos(await respuesta.json());
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setCargando(false);
        }
    }, [desde, hasta]);
    // useCallback memoriza la función y solo la vuelve a crear si
    // cambian "desde" o "hasta".
    //
    // ¿Por qué hace falta? Porque la uso dentro de un useEffect. Si la
    // función se recreara en cada repintado, el useEffect la vería como
    // "nueva" cada vez, se ejecutaría otra vez, provocaría un repintado...
    // y entraría en bucle infinito.

    useEffect(() => {
        cargar();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    // El [] vacío = corre UNA sola vez, al abrir la pantalla.
    //
    // Antes tenía [cargar], y como "cargar" se recrea cuando cambian
    // las fechas, se disparaba en cada tecla. Ahora el usuario cambia
    // las fechas con calma y decide cuándo consultar, con el botón.
    //
    // El comentario de eslint silencia la advertencia del linter, que
    // querría que pusiera [cargar]. Aquí lo estoy omitiendo a propósito.

    return (
        <main className="p-8">
            <h1 className="text-2xl font-bold mb-6">Dashboard</h1>

            {/* ─── FILTRO DE FECHAS ─── */}
            <div className="bg-white border rounded-lg p-4 mb-6 flex flex-wrap gap-4 items-end">
                <div>
                    <label className="block text-xs text-gray-500 mb-1">Desde</label>
                    <input
                        type="date"
                        value={desde}
                        onChange={(e) => setDesde(e.target.value)}
                        className="border rounded px-3 py-2 text-sm"
                    />
                </div>
                <div>
                    <label className="block text-xs text-gray-500 mb-1">Hasta</label>
                    <input
                        type="date"
                        value={hasta}
                        onChange={(e) => setHasta(e.target.value)}
                        className="border rounded px-3 py-2 text-sm"
                    />
                </div>
                <button
                    onClick={cargar}
                    disabled={cargando}
                    className="bg-blue-600 text-white rounded px-5 py-2 text-sm font-medium
                     disabled:bg-gray-400"
                >
                    {cargando ? 'Cargando...' : 'Actualizar'}
                </button>
                {/* type="date" es un input nativo del navegador: trae su propio
            calendario, sin librerías. Devuelve siempre "AAAA-MM-DD",
            que es justo lo que el backend sabe leer. */}
            </div>

            {error && (
                <div className="mb-6 p-3 rounded bg-red-100 text-red-800">{error}</div>
            )}

            {datos && (
                <>
                    {/* ─── USUARIOS ─── */}
                    <h2 className="text-lg font-semibold mb-3">Usuarios</h2>
                    <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-8">
                        <Indicador etiqueta="Total" valor={datos.usuarios.total} />
                        <Indicador etiqueta="Activos" valor={datos.usuarios.activos} color="text-green-600" />
                        <Indicador etiqueta="Inactivos" valor={datos.usuarios.inactivos} color="text-gray-400" />
                        <Indicador etiqueta="Sin aprobar" valor={datos.usuarios.pendientesAprobacion} color="text-amber-600" />
                        <Indicador etiqueta="Bloqueados" valor={datos.usuarios.bloqueados} color="text-red-600" />
                        <Indicador etiqueta="Gestores" valor={datos.usuarios.gestores} />
                        <Indicador etiqueta="Cajeros" valor={datos.usuarios.cajeros} />
                        <Indicador etiqueta="Sesión abierta" valor={datos.usuarios.conSesionAbierta} color="text-blue-600" />
                    </div>

                    {/* ─── PERIODO ─── */}
                    <h2 className="text-lg font-semibold mb-3">
                        Actividad del periodo
                    </h2>
                    <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-8">
                        <Indicador etiqueta="Turnos emitidos" valor={datos.turnosEmitidos} />
                        <Indicador etiqueta="Atendidos" valor={datos.turnosAtendidos} color="text-green-600" />
                        <Indicador etiqueta="Pendientes" valor={datos.turnosPendientes} color="text-amber-600" />
                        <Indicador etiqueta="Contratos activos" valor={datos.contratosActivos} />
                    </div>

                    {/* ─── CAJAS ─── */}
                    <h2 className="text-lg font-semibold mb-3">Cajas</h2>
                    <table className="w-full bg-white border rounded-lg overflow-hidden text-sm">
                        <thead className="bg-gray-100">
                            <tr>
                                <th className="text-left px-4 py-2">Caja</th>
                                <th className="text-left px-4 py-2">Cajeros asignados</th>
                                <th className="text-left px-4 py-2">Turnos atendidos</th>
                                <th className="text-left px-4 py-2">Estado</th>
                            </tr>
                        </thead>
                        <tbody>
                            {datos.cajas.map((c) => (
                                <tr key={c.caja} className="border-t">
                                    <td className="px-4 py-2 font-medium">{c.caja}</td>
                                    <td className="px-4 py-2">{c.cajerosAsignados} / 2</td>
                                    <td className="px-4 py-2">{c.turnosAtendidos}</td>
                                    <td className="px-4 py-2">
                                        {c.ocupadaPor ? (
                                            <span className="text-blue-700">
                                                Operando: {c.ocupadaPor}
                                            </span>
                                        ) : (
                                            <span className="text-gray-400">Libre</span>
                                        )}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                    {/* El "/ 2" no es decorativo: le recuerda al administrador
              cuál es el máximo, así ve de un vistazo qué caja tiene cupo. */}
                </>
            )}
            {/* Ese <> </> es un "Fragment": agrupa varios elementos sin
          meter un <div> extra en el HTML. Un componente de React
          tiene que devolver UNA sola cosa; el fragmento la envuelve
          sin ensuciar la estructura. */}
        </main>
    );
}