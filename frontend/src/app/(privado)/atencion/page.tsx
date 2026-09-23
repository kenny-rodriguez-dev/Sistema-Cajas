'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { apiFetch } from '@/lib/api';

// ─── TIPOS ───────────────────────────────────────────────────
type Cliente = {
    id: number; nombres: string; apellidos: string; identificacion: string;
    correo: string; telefono: string; activo: boolean;
};

type Contrato = {
    id: number; servicio: string; velocidadMbps: number; precio: number;
    formaPago: string; estado: string; fechaInicio: string; fechaFin: string;
    contratoAnteriorId: number | null;
};

type Pago = {
    id: number; periodo: string; monto: number; formaPago: string;
    servicio: string; fechaPago: string; registradoPor: string;
};

type Servicio = {
    id: number; nombre: string; descripcion: string;
    velocidadMbps: number; precio: number; equipos: string[];
};

type FormaPago = { id: number; descripcion: string };

const ESTADOS: Record<string, { texto: string; clase: string }> = {
    VIG: { texto: 'Vigente', clase: 'bg-green-100 text-green-800' },
    REN: { texto: 'Renovado', clase: 'bg-blue-100 text-blue-800' },
    SUS: { texto: 'Sustituido', clase: 'bg-gray-100 text-gray-600' },
    CAN: { texto: 'Cancelado', clase: 'bg-red-100 text-red-700' },
};

function mesActual(): string {
    return new Date().toISOString().slice(0, 7);
    // "2026-09-23T..." -> "2026-09". El formato exacto que espera el
    // backend y el que devuelve un <input type="month">.
}

function fecha(iso: string): string {
    return new Date(iso).toLocaleDateString('es-EC');
    // Aquí trabaja la "Z" que descubriste: sin ella, las fechas
    // saldrían corridas cinco horas.
}

export default function AtencionPage() {
    // ─── Catálogos: se cargan una vez al abrir ───
    const [servicios, setServicios] = useState<Servicio[]>([]);
    const [formasPago, setFormasPago] = useState<FormaPago[]>([]);
    const [cajaAbierta, setCajaAbierta] = useState(false);

    // ─── Búsqueda ───
    const [cedula, setCedula] = useState('');
    const [cliente, setCliente] = useState<Cliente | null>(null);
    const [noExiste, setNoExiste] = useState(false);

    // ─── Datos del cliente encontrado ───
    const [contratos, setContratos] = useState<Contrato[]>([]);
    const [pagos, setPagos] = useState<Pago[]>([]);

    // ─── Campos de cada formulario ───
    const [nuevoServicio, setNuevoServicio] = useState<number | ''>('');
    const [nuevaForma, setNuevaForma] = useState<number | ''>('');
    const [meses, setMeses] = useState(12);
    const [periodo, setPeriodo] = useState(mesActual());
    const [cambioServicio, setCambioServicio] = useState<number | ''>('');
    const [cambioForma, setCambioForma] = useState<number | ''>('');

    const [mensaje, setMensaje] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [ocupado, setOcupado] = useState(false);

    useEffect(() => {
        Promise.all([
            apiFetch('/api/contratos/servicios'),
            apiFetch('/api/contratos/formas-pago'),
            apiFetch('/api/cajas/mis-cajas'),
        ])
            .then(async ([rS, rF, rC]) => {
                if (rS.ok) setServicios(await rS.json());
                if (rF.ok) setFormasPago(await rF.json());
                if (rC.ok) {
                    const d = await rC.json();
                    setCajaAbierta(d.miSesionCajaId !== null);
                    // Solo para AVISAR y apagar el botón de cobrar.
                    // Quien de verdad lo impide es el backend, con su 409.
                }
            })
            .catch(() => setError('No se pudieron cargar los catálogos'));
    }, []);

    // ═══ VALORES DERIVADOS ═══
    // No se guardan en un estado: se calculan en cada repintado a partir
    // de los datos. Así nunca se desincronizan con la lista real.

    const contratoActivo = contratos.find((c) => c.estado === 'VIG' || c.estado === 'REN') ?? null;
    // Un cliente tiene como máximo UN contrato activo: lo garantiza el
    // índice único filtrado que pusiste en la rebanada 5.

    const periodosPagados = new Set(pagos.map((p) => p.periodo));
    const periodoYaPagado = periodosPagados.has(periodo);

    function limpiar() {
        setMensaje(null);
        setError(null);
    }

    async function cargarCliente(id: number) {
        const [rC, rP] = await Promise.all([
            apiFetch(`/api/contratos/cliente/${id}`),
            apiFetch(`/api/pagos/cliente/${id}`),
        ]);
        if (rC.ok) setContratos(await rC.json());
        if (rP.ok) setPagos(await rP.json());
    }

    async function buscar() {
        limpiar();
        setCliente(null);
        setNoExiste(false);
        setContratos([]);
        setPagos([]);

        const valor = cedula.trim();
        if (!/^[0-9]{10,13}$/.test(valor)) {
            setError('La identificación debe tener entre 10 y 13 dígitos');
            return;
        }

        try {
            const r = await apiFetch(`/api/clientes/buscar?identificacion=${encodeURIComponent(valor)}`);
            if (r.status === 404) { setNoExiste(true); return; }
            if (!r.ok) { setError('No se pudo buscar'); return; }

            const encontrado: Cliente = await r.json();
            setCliente(encontrado);
            await cargarCliente(encontrado.id);
        } catch {
            setError('No se pudo conectar con el servidor');
        }
    }

    async function ejecutar(ruta: string, cuerpo: object) {
        // Los cinco procesos son un POST que después recarga los datos
        // del cliente. Una función para todos.
        if (!cliente) return;
        setOcupado(true);
        limpiar();

        try {
            const r = await apiFetch(ruta, { method: 'POST', body: JSON.stringify(cuerpo) });
            if (r.status === 403) { setError('No tienes permiso para esta acción'); return; }

            const d = await r.json();
            if (!r.ok) {
                setError(d.errors ? Object.values(d.errors).flat().join(' ') : (d.mensaje ?? 'No se pudo completar'));
                return;
                // Aquí llegan TODAS las reglas del backend con su mensaje:
                // "ya tiene un contrato activo", "el periodo ya está pagado",
                // "debes abrir una sesión de caja"...
            }

            setMensaje(d.mensaje);
            setNuevoServicio(''); setNuevaForma('');
            setCambioServicio(''); setCambioForma('');
            await cargarCliente(cliente.id);
            // Recargo desde el servidor en vez de actualizar a mano:
            // un cambio de plan crea un contrato NUEVO y marca el viejo como
            // sustituido. Dejar que el servidor diga cómo quedó es lo seguro.
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setOcupado(false);
        }
    }

    // ─── LOS CINCO PROCESOS ───

    function contratar() {
        if (!nuevoServicio || !nuevaForma) { setError('Elige un servicio y una forma de pago'); return; }
        ejecutar('/api/contratos', {
            clienteId: cliente!.id,
            // El "!" le dice a TypeScript "cliente no es null aquí, confía".
            // Es seguro porque este botón solo aparece con un cliente cargado.
            servicioId: nuevoServicio,
            formaPagoId: nuevaForma,
            mesesDuracion: meses,
        });
    }

    function pagar() {
        if (!contratoActivo) return;
        ejecutar('/api/pagos', { contratoId: contratoActivo.id, periodo });
        // Solo mando contrato y periodo. EL MONTO NO VIAJA: lo pone el servidor.
    }

    function cambiarServicio() {
        if (!contratoActivo || !cambioServicio) { setError('Elige el nuevo servicio'); return; }
        ejecutar(`/api/contratos/${contratoActivo.id}/cambiar-servicio`, { nuevoServicioId: cambioServicio });
    }

    function cambiarForma() {
        if (!contratoActivo || !cambioForma) { setError('Elige la nueva forma de pago'); return; }
        ejecutar(`/api/contratos/${contratoActivo.id}/cambiar-forma-pago`, { nuevaFormaPagoId: cambioForma });
    }

    function cancelar() {
        if (!contratoActivo) return;
        if (!window.confirm('¿Cancelar el contrato? Su fecha de fin pasará a ser hoy.')) return;
        ejecutar(`/api/contratos/${contratoActivo.id}/cancelar`, {});
    }

    const numero = (v: string): number | '' => (v === '' ? '' : Number(v));
    // Un <select> siempre devuelve texto. Esto lo pasa a número,
    // o deja '' si no hay nada elegido.

    return (
        <main className="p-8 max-w-6xl">
            <h1 className="text-2xl font-bold mb-6">Atención al cliente</h1>

            {mensaje && <div className="mb-4 p-3 rounded bg-green-100 text-green-800">{mensaje}</div>}
            {error && <div className="mb-4 p-3 rounded bg-red-100 text-red-800">{error}</div>}

            {!cajaAbierta && (
                <div className="mb-4 p-3 rounded bg-amber-50 border border-amber-200 text-sm text-amber-900">
                    No tienes una caja abierta. Puedes consultar y gestionar contratos, pero para
                    <strong> cobrar</strong> debes abrir una en{' '}
                    <Link href="/caja" className="underline font-medium">Procesos de caja</Link>.
                </div>
            )}

            {/* ═══ 1. BUSCAR ═══ */}
            <div className="bg-white border rounded-lg p-5 mb-6">
                <label className="block text-sm font-medium mb-2">Identificación del cliente</label>
                <div className="flex gap-2">
                    <input
                        value={cedula}
                        onChange={(e) => setCedula(e.target.value)}
                        onKeyDown={(e) => e.key === 'Enter' && buscar()}
                        inputMode="numeric"
                        placeholder="0912345678"
                        className="flex-1 border rounded px-3 py-2"
                    />
                    <button onClick={buscar} className="bg-blue-600 text-white rounded px-6 py-2 font-medium">
                        Buscar
                    </button>
                </div>

                {noExiste && (
                    <div className="mt-4 p-4 bg-amber-50 border border-amber-200 rounded text-sm">
                        No hay ningún cliente con esa identificación.{' '}
                        <Link href="/clientes/nuevo" className="text-blue-600 underline font-medium">
                            Registrarlo como nuevo
                        </Link>
                    </div>
                )}
            </div>

            {cliente && (
                <>
                    {/* ═══ 2. EL CLIENTE ═══ */}
                    <div className="bg-white border rounded-lg p-5 mb-6">
                        <p className="text-lg font-semibold">
                            {cliente.nombres} {cliente.apellidos}
                        </p>
                        <p className="text-sm text-gray-500">
                            <span className="font-mono">{cliente.identificacion}</span> · {cliente.correo} · {cliente.telefono}
                        </p>
                        {!cliente.activo && (
                            <p className="mt-3 p-3 rounded bg-red-50 text-red-800 text-sm">
                                Este cliente está inactivo. Reactívalo en{' '}
                                <Link href="/clientes" className="underline font-medium">Clientes</Link> para poder atenderlo.
                            </p>
                        )}
                    </div>

                    {cliente.activo && (
                        contratoActivo ? (
                            /* ═══ 3A. TIENE CONTRATO ACTIVO: cobrar, cambiar o cancelar ═══ */
                            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-6">

                                <div className="bg-white border rounded-lg p-5">
                                    <div className="flex justify-between items-start mb-3">
                                        <h2 className="font-semibold">Contrato actual</h2>
                                        <span className={`px-2 py-1 rounded text-xs ${ESTADOS[contratoActivo.estado]?.clase}`}>
                                            {ESTADOS[contratoActivo.estado]?.texto}
                                        </span>
                                    </div>
                                    <p className="text-2xl font-bold">{contratoActivo.servicio}</p>
                                    <p className="text-gray-600">
                                        {contratoActivo.velocidadMbps} Mbps · ${contratoActivo.precio.toFixed(2)} al mes
                                    </p>
                                    <p className="text-sm text-gray-500 mt-2">
                                        Forma de pago: {contratoActivo.formaPago}<br />
                                        Vigencia: {fecha(contratoActivo.fechaInicio)} al {fecha(contratoActivo.fechaFin)}
                                    </p>
                                    <button onClick={cancelar} disabled={ocupado}
                                        className="mt-4 text-sm text-red-600 hover:underline disabled:text-gray-400">
                                        Cancelar contrato
                                    </button>
                                </div>

                                {/* ─── COBRAR ─── */}
                                <div className="bg-white border rounded-lg p-5">
                                    <h2 className="font-semibold mb-3">Registrar pago</h2>
                                    <label className="block text-xs text-gray-500 mb-1">Mes a pagar</label>
                                    <input
                                        type="month"
                                        value={periodo}
                                        onChange={(e) => setPeriodo(e.target.value)}
                                        className="w-full border rounded px-3 py-2 mb-3"
                                    />
                                    {/* type="month" es un selector nativo del navegador:
                      devuelve "AAAA-MM", justo lo que valida el backend. */}

                                    <p className="text-sm text-gray-600 mb-3">
                                        Monto: <strong>${contratoActivo.precio.toFixed(2)}</strong>
                                        <span className="text-xs text-gray-400"> (lo calcula el sistema)</span>
                                    </p>
                                    {/* Se MUESTRA el precio, pero no se envía: el backend
                      pone el monto por su cuenta. Lo que ves aquí es
                      informativo. */}

                                    {periodoYaPagado && (
                                        <p className="text-sm text-amber-700 mb-3">Ese mes ya está pagado.</p>
                                    )}

                                    <button
                                        onClick={pagar}
                                        disabled={ocupado || !cajaAbierta || periodoYaPagado}
                                        className="w-full bg-green-600 text-white rounded py-2 font-medium
                               disabled:bg-gray-400 disabled:cursor-not-allowed"
                                    >
                                        {!cajaAbierta ? 'Abre tu caja para cobrar' : 'Cobrar'}
                                    </button>
                                    {/* Tres razones para apagar el botón, todas capa 1.
                      El backend repite las tres verificaciones. */}
                                </div>

                                {/* ─── CAMBIAR SERVICIO ─── */}
                                <div className="bg-white border rounded-lg p-5">
                                    <h2 className="font-semibold mb-1">Cambiar de plan</h2>
                                    <p className="text-xs text-gray-500 mb-3">
                                        Se crea un contrato nuevo. La fecha de fin se conserva.
                                    </p>
                                    <div className="flex gap-2">
                                        <select
                                            value={cambioServicio}
                                            onChange={(e) => setCambioServicio(numero(e.target.value))}
                                            className="flex-1 border rounded px-3 py-2 text-sm"
                                        >
                                            <option value="">Elige el nuevo plan...</option>
                                            {servicios.map((s) => (
                                                <option key={s.id} value={s.id} disabled={s.nombre === contratoActivo.servicio}>
                                                    {s.nombre} · {s.velocidadMbps} Mbps · ${s.precio.toFixed(2)}
                                                    {s.nombre === contratoActivo.servicio ? ' (actual)' : ''}
                                                </option>
                                            ))}
                                        </select>
                                        <button onClick={cambiarServicio} disabled={ocupado}
                                            className="bg-blue-600 text-white rounded px-4 py-2 text-sm disabled:bg-gray-400">
                                            Cambiar
                                        </button>
                                    </div>
                                </div>

                                {/* ─── CAMBIAR FORMA DE PAGO ─── */}
                                <div className="bg-white border rounded-lg p-5">
                                    <h2 className="font-semibold mb-1">Cambiar forma de pago</h2>
                                    <p className="text-xs text-gray-500 mb-3">
                                        Se actualiza el mismo contrato: no cambia su valor.
                                    </p>
                                    <div className="flex gap-2">
                                        <select
                                            value={cambioForma}
                                            onChange={(e) => setCambioForma(numero(e.target.value))}
                                            className="flex-1 border rounded px-3 py-2 text-sm"
                                        >
                                            <option value="">Elige la nueva forma...</option>
                                            {formasPago.map((f) => (
                                                <option key={f.id} value={f.id} disabled={f.descripcion === contratoActivo.formaPago}>
                                                    {f.descripcion}{f.descripcion === contratoActivo.formaPago ? ' (actual)' : ''}
                                                </option>
                                            ))}
                                        </select>
                                        <button onClick={cambiarForma} disabled={ocupado}
                                            className="bg-blue-600 text-white rounded px-4 py-2 text-sm disabled:bg-gray-400">
                                            Cambiar
                                        </button>
                                    </div>
                                </div>
                            </div>
                        ) : (
                            /* ═══ 3B. SIN CONTRATO ACTIVO: contratar ═══ */
                            <div className="bg-white border rounded-lg p-5 mb-6">
                                <h2 className="font-semibold mb-4">Contratar un servicio</h2>
                                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                    <div>
                                        <label className="block text-xs text-gray-500 mb-1">Servicio</label>
                                        <select
                                            value={nuevoServicio}
                                            onChange={(e) => setNuevoServicio(numero(e.target.value))}
                                            className="w-full border rounded px-3 py-2 text-sm"
                                        >
                                            <option value="">Elige...</option>
                                            {servicios.map((s) => (
                                                <option key={s.id} value={s.id}>
                                                    {s.nombre} · {s.velocidadMbps} Mbps · ${s.precio.toFixed(2)}
                                                </option>
                                            ))}
                                        </select>
                                    </div>
                                    <div>
                                        <label className="block text-xs text-gray-500 mb-1">Forma de pago</label>
                                        <select
                                            value={nuevaForma}
                                            onChange={(e) => setNuevaForma(numero(e.target.value))}
                                            className="w-full border rounded px-3 py-2 text-sm"
                                        >
                                            <option value="">Elige...</option>
                                            {formasPago.map((f) => (
                                                <option key={f.id} value={f.id}>{f.descripcion}</option>
                                            ))}
                                        </select>
                                    </div>
                                    <div>
                                        <label className="block text-xs text-gray-500 mb-1">Duración (meses)</label>
                                        <input
                                            type="number" min={1} max={36}
                                            value={meses}
                                            onChange={(e) => setMeses(Number(e.target.value))}
                                            className="w-full border rounded px-3 py-2 text-sm"
                                        />
                                    </div>
                                </div>

                                {nuevoServicio !== '' && (() => {
                                    const s = servicios.find((x) => x.id === nuevoServicio);
                                    return s ? (
                                        <p className="text-sm text-gray-600 mt-3">
                                            {s.descripcion}. Incluye: {s.equipos.join(', ')}.
                                        </p>
                                    ) : null;
                                })()}
                                {/* Una función que se ejecuta en el acto dentro del JSX:
                    así puedo buscar el servicio y dibujarlo en un solo
                    bloque. Muestra los equipos que pide el enunciado. */}

                                <button onClick={contratar} disabled={ocupado}
                                    className="mt-4 bg-blue-600 text-white rounded px-6 py-2 font-medium disabled:bg-gray-400">
                                    {ocupado ? 'Procesando...' : 'Contratar'}
                                </button>
                            </div>
                        )
                    )}

                    {/* ═══ 4. HISTORIAL DE CONTRATOS ═══ */}
                    <h2 className="text-lg font-semibold mb-3">Historial de contratos</h2>
                    <table className="w-full bg-white border rounded-lg overflow-hidden text-sm mb-6">
                        <thead className="bg-gray-100">
                            <tr>
                                <th className="text-left px-4 py-2">#</th>
                                <th className="text-left px-4 py-2">Servicio</th>
                                <th className="text-left px-4 py-2">Precio</th>
                                <th className="text-left px-4 py-2">Vigencia</th>
                                <th className="text-left px-4 py-2">Estado</th>
                            </tr>
                        </thead>
                        <tbody>
                            {contratos.length === 0 && (
                                <tr><td colSpan={5} className="px-4 py-6 text-center text-gray-400">Sin contratos</td></tr>
                            )}
                            {contratos.map((c) => (
                                <tr key={c.id} className="border-t">
                                    <td className="px-4 py-2 font-mono">
                                        {c.id}
                                        {c.contratoAnteriorId && (
                                            <span className="text-xs text-gray-400"> ← {c.contratoAnteriorId}</span>
                                        )}
                                    </td>
                                    {/* La flecha muestra la cadena de sustitución:
                      "el contrato 5 reemplazó al 3". Es el campo
                      ContratoAnteriorId que agregaste en la rebanada 5. */}
                                    <td className="px-4 py-2">{c.servicio} ({c.velocidadMbps} Mbps)</td>
                                    <td className="px-4 py-2">${c.precio.toFixed(2)}</td>
                                    <td className="px-4 py-2">{fecha(c.fechaInicio)} al {fecha(c.fechaFin)}</td>
                                    <td className="px-4 py-2">
                                        <span className={`px-2 py-1 rounded text-xs ${ESTADOS[c.estado]?.clase}`}>
                                            {ESTADOS[c.estado]?.texto ?? c.estado}
                                        </span>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>

                    {/* ═══ 5. HISTORIAL DE PAGOS ═══ */}
                    <h2 className="text-lg font-semibold mb-3">Historial de pagos</h2>
                    <table className="w-full bg-white border rounded-lg overflow-hidden text-sm">
                        <thead className="bg-gray-100">
                            <tr>
                                <th className="text-left px-4 py-2">Mes</th>
                                <th className="text-left px-4 py-2">Servicio</th>
                                <th className="text-left px-4 py-2">Monto</th>
                                <th className="text-left px-4 py-2">Forma de pago</th>
                                <th className="text-left px-4 py-2">Cobrado por</th>
                            </tr>
                        </thead>
                        <tbody>
                            {pagos.length === 0 && (
                                <tr><td colSpan={5} className="px-4 py-6 text-center text-gray-400">Sin pagos</td></tr>
                            )}
                            {pagos.map((p) => (
                                <tr key={p.id} className="border-t">
                                    <td className="px-4 py-2 font-mono">{p.periodo}</td>
                                    <td className="px-4 py-2">{p.servicio}</td>
                                    <td className="px-4 py-2">${p.monto.toFixed(2)}</td>
                                    <td className="px-4 py-2">{p.formaPago}</td>
                                    <td className="px-4 py-2 text-gray-600">{p.registradoPor}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                    {/* Fíjate en "Servicio" y "Forma de pago": muestran lo que
              valían EL DÍA DEL PAGO. Si el cliente cambió de plan después,
              los pagos viejos siguen diciendo el plan viejo. Es la copia
              que decidiste guardar en vez de un enlace al valor actual. */}
                </>
            )}
        </main>
    );
}