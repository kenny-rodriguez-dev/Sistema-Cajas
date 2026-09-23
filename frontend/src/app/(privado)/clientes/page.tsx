'use client';

import { useState, useEffect, useCallback } from 'react';
import Link from 'next/link';
import { apiFetch } from '@/lib/api';

type Cliente = {
    id: number;
    nombres: string;
    apellidos: string;
    identificacion: string;
    correo: string;
    telefono: string;
    direccion: string;
    referenciaDireccion: string;
    activo: boolean;
};

type Edicion = Omit<Cliente, 'id' | 'identificacion' | 'activo'>;
// ═══ Omit: UN TIPO A PARTIR DE OTRO ═══
// "Todo lo de Cliente MENOS estos tres campos". Si mañana se agrega
// un campo a Cliente, Edicion lo hereda solo, sin reescribirlo.
// Y quitar identificacion aquí refleja lo mismo que el backend:
// la cédula no se edita.

const CAMPOS: { clave: keyof Edicion; etiqueta: string }[] = [
    { clave: 'nombres', etiqueta: 'Nombres' },
    { clave: 'apellidos', etiqueta: 'Apellidos' },
    { clave: 'correo', etiqueta: 'Correo' },
    { clave: 'telefono', etiqueta: 'Teléfono' },
    { clave: 'direccion', etiqueta: 'Dirección' },
    { clave: 'referenciaDireccion', etiqueta: 'Referencia de la dirección' },
];
// Una lista que DESCRIBE el formulario. Lo dibujo con un .map en vez
// de escribir seis bloques casi idénticos.

export default function ClientesPage() {
    const [clientes, setClientes] = useState<Cliente[]>([]);
    const [incluirInactivos, setIncluirInactivos] = useState(false);

    const [cedula, setCedula] = useState('');
    const [encontrado, setEncontrado] = useState<Cliente | null>(null);
    const [noExiste, setNoExiste] = useState(false);

    const [editando, setEditando] = useState<Cliente | null>(null);
    const [datos, setDatos] = useState<Edicion | null>(null);
    const [errores, setErrores] = useState<Record<string, string[]>>({});

    const [mensaje, setMensaje] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [cargando, setCargando] = useState(true);
    const [ocupado, setOcupado] = useState(false);

    const cargar = useCallback(async (inactivos: boolean) => {
        try {
            const r = await apiFetch(`/api/clientes?incluirInactivos=${inactivos}`);
            if (r.status === 403) { setError('Solo los cajeros gestionan clientes'); return; }
            if (r.ok) setClientes(await r.json());
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setCargando(false);
        }
    }, []);

    useEffect(() => { cargar(incluirInactivos); }, [cargar, incluirInactivos]);
    // Recarga al marcar la casilla. Aquí sí está bien: es un clic,
    // no una tecla, así que no hay estados intermedios como con las fechas.

    function limpiar() {
        setMensaje(null); setError(null); setErrores({});
    }

    async function buscar() {
        limpiar();
        setEncontrado(null);
        setNoExiste(false);

        const valor = cedula.trim();
        if (!/^[0-9]{10,13}$/.test(valor)) {
            setError('La identificación debe tener entre 10 y 13 dígitos');
            return;
        }

        try {
            const r = await apiFetch(`/api/clientes/buscar?identificacion=${encodeURIComponent(valor)}`);
            // encodeURIComponent escapa el valor para la URL. Aquí son solo
            // dígitos, pero es el hábito correcto con cualquier dato que
            // viaje en una URL: un espacio o un & romperían la dirección.

            if (r.status === 404) { setNoExiste(true); return; }
            if (!r.ok) { setError('No se pudo realizar la búsqueda'); return; }
            setEncontrado(await r.json());
        } catch {
            setError('No se pudo conectar con el servidor');
        }
    }

    function empezarEdicion(c: Cliente) {
        limpiar();
        setEditando(c);
        setDatos({
            nombres: c.nombres,
            apellidos: c.apellidos,
            correo: c.correo,
            telefono: c.telefono,
            direccion: c.direccion,
            referenciaDireccion: c.referenciaDireccion,
        });
        window.scrollTo({ top: 0, behavior: 'smooth' });
        // El panel de edición aparece arriba: subo la página para que se vea.
    }

    function cancelarEdicion() {
        setEditando(null);
        setDatos(null);
        setErrores({});
    }

    function erroresDe(campo: string): string[] {
        const clave = Object.keys(errores).find((k) => k.toLowerCase() === campo.toLowerCase());
        return clave ? errores[clave] : [];
    }

    async function guardar() {
        if (!editando || !datos) return;
        setOcupado(true);
        limpiar();

        try {
            const r = await apiFetch(`/api/clientes/${editando.id}`, {
                method: 'PUT',
                body: JSON.stringify(datos),
            });

            if (r.status === 403) { setError('No tienes permiso para editar clientes'); return; }

            const d = await r.json();

            if (r.status === 400) {
                setErrores(d.errors ?? {});
                setError('Revisa los campos marcados en rojo');
                return;
            }
            if (!r.ok) { setError(d.mensaje ?? 'No se pudo guardar'); return; }

            setMensaje(d.mensaje);
            cancelarEdicion();
            setEncontrado(null);
            await cargar(incluirInactivos);
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setOcupado(false);
        }
    }

    async function cambiarEstado(c: Cliente) {
        const accion = c.activo ? 'desactivar' : 'activar';
        if (!window.confirm(`¿Seguro que quieres ${accion} a ${c.nombres} ${c.apellidos}?`)) return;

        setOcupado(true);
        limpiar();

        try {
            const r = await apiFetch(`/api/clientes/${c.id}/estado`, { method: 'POST' });
            if (r.status === 403) { setError('No tienes permiso para esta acción'); return; }

            const d = await r.json();
            if (!r.ok) { setError(d.mensaje ?? 'No se pudo cambiar el estado'); return; }
            // Aquí llega la salvaguarda: "tiene un contrato vigente".

            setMensaje(d.mensaje);
            setEncontrado(null);
            await cargar(incluirInactivos);
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setOcupado(false);
        }
    }

    if (cargando) return <main className="p-8 text-gray-500">Cargando...</main>;

    return (
        <main className="p-8 max-w-5xl">
            <div className="flex justify-between items-center mb-6">
                <h1 className="text-2xl font-bold">Clientes</h1>
                <div className="flex gap-2">
                    <Link href="/clientes/carga" className="border rounded px-4 py-2 text-sm">
                        Carga masiva
                    </Link>
                    <Link href="/clientes/nuevo" className="bg-blue-600 text-white rounded px-4 py-2 text-sm font-medium">
                        + Nuevo cliente
                    </Link>
                </div>
            </div>

            {mensaje && <div className="mb-4 p-3 rounded bg-green-100 text-green-800">{mensaje}</div>}
            {error && <div className="mb-4 p-3 rounded bg-red-100 text-red-800">{error}</div>}

            {/* ─── BUSCAR POR IDENTIFICACIÓN ─── */}
            <div className="bg-white border rounded-lg p-5 mb-6">
                <h2 className="font-semibold mb-3">Buscar por identificación</h2>
                <div className="flex gap-2">
                    <input
                        value={cedula}
                        onChange={(e) => setCedula(e.target.value)}
                        onKeyDown={(e) => e.key === 'Enter' && buscar()}
                        inputMode="numeric"
                        placeholder="0912345678"
                        className="flex-1 border rounded px-3 py-2 text-sm"
                    />
                    {/* onKeyDown con Enter: buscar sin tener que ir al botón. */}
                    <button onClick={buscar} className="bg-blue-600 text-white rounded px-5 py-2 text-sm font-medium">
                        Buscar
                    </button>
                </div>

                {encontrado && (
                    <div className="mt-4 p-4 bg-gray-50 rounded flex justify-between items-center">
                        <div>
                            <p className="font-medium">
                                {encontrado.nombres} {encontrado.apellidos}
                                {!encontrado.activo && (
                                    <span className="ml-2 text-xs bg-gray-200 text-gray-600 px-2 py-0.5 rounded">Inactivo</span>
                                )}
                            </p>
                            <p className="text-sm text-gray-500">{encontrado.correo} · {encontrado.telefono}</p>
                        </div>
                        <button onClick={() => empezarEdicion(encontrado)} className="text-sm text-blue-600 underline">
                            Editar
                        </button>
                    </div>
                )}

                {noExiste && (
                    <div className="mt-4 p-4 bg-amber-50 border border-amber-200 rounded text-sm">
                        No hay ningún cliente con esa identificación.{' '}
                        <Link href="/clientes/nuevo" className="text-blue-600 underline font-medium">
                            Registrarlo como nuevo
                        </Link>
                    </div>
                )}
                {/* El flujo del enunciado: se busca primero. Si existe, se usa.
            Si no, se ofrece registrarlo. */}
            </div>

            {/* ─── PANEL DE EDICIÓN ─── */}
            {editando && datos && (
                <div className="bg-white border-2 border-blue-200 rounded-lg p-5 mb-6">
                    <h2 className="font-semibold mb-1">Editar cliente</h2>
                    <p className="text-sm text-gray-500 mb-4">
                        Identificación: <span className="font-mono">{editando.identificacion}</span> (no se puede modificar)
                    </p>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        {CAMPOS.map(({ clave, etiqueta }) => (
                            <div key={clave}>
                                <label className="block text-xs text-gray-500 mb-1">{etiqueta}</label>
                                <input
                                    value={datos[clave]}
                                    onChange={(e) => setDatos({ ...datos, [clave]: e.target.value })}
                                    className="w-full border rounded px-3 py-2 text-sm"
                                />
                                {erroresDe(clave).map((m) => (
                                    <p key={m} className="text-red-600 text-xs mt-1">{m}</p>
                                ))}
                            </div>
                        ))}
                    </div>

                    <div className="flex gap-2 mt-4">
                        <button onClick={guardar} disabled={ocupado}
                            className="bg-blue-600 text-white rounded px-5 py-2 text-sm font-medium disabled:bg-gray-400">
                            {ocupado ? 'Guardando...' : 'Guardar cambios'}
                        </button>
                        <button onClick={cancelarEdicion} className="border rounded px-5 py-2 text-sm">
                            Cancelar
                        </button>
                    </div>
                </div>
            )}

            {/* ─── LISTA ─── */}
            <div className="flex justify-between items-center mb-3">
                <h2 className="text-lg font-semibold">Todos ({clientes.length})</h2>
                <label className="flex items-center gap-2 text-sm text-gray-600">
                    <input
                        type="checkbox"
                        checked={incluirInactivos}
                        onChange={(e) => setIncluirInactivos(e.target.checked)}
                    />
                    Mostrar inactivos
                </label>
            </div>

            <table className="w-full bg-white border rounded-lg overflow-hidden text-sm">
                <thead className="bg-gray-100">
                    <tr>
                        <th className="text-left px-4 py-2">Cliente</th>
                        <th className="text-left px-4 py-2">Identificación</th>
                        <th className="text-left px-4 py-2">Teléfono</th>
                        <th className="text-left px-4 py-2">Estado</th>
                        <th className="text-left px-4 py-2">Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    {clientes.length === 0 && (
                        <tr><td colSpan={5} className="px-4 py-6 text-center text-gray-400">No hay clientes</td></tr>
                    )}
                    {clientes.map((c) => (
                        <tr key={c.id} className={`border-t ${!c.activo ? 'opacity-60' : ''}`}>
                            <td className="px-4 py-2 font-medium">{c.apellidos} {c.nombres}</td>
                            <td className="px-4 py-2 font-mono">{c.identificacion}</td>
                            <td className="px-4 py-2">{c.telefono}</td>
                            <td className="px-4 py-2">
                                <span className={`px-2 py-1 rounded text-xs ${c.activo ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-500'
                                    }`}>
                                    {c.activo ? 'Activo' : 'Inactivo'}
                                </span>
                            </td>
                            <td className="px-4 py-2">
                                <div className="flex gap-3">
                                    <button onClick={() => empezarEdicion(c)} className="text-xs text-blue-600 hover:underline">
                                        Editar
                                    </button>
                                    <button onClick={() => cambiarEstado(c)} disabled={ocupado}
                                        className="text-xs text-gray-600 hover:underline disabled:text-gray-300">
                                        {c.activo ? 'Desactivar' : 'Activar'}
                                    </button>
                                </div>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </main>
    );
}