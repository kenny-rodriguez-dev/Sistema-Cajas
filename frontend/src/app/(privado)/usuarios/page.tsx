'use client';

import { useState, useEffect, useCallback } from 'react';
import { apiFetch } from '@/lib/api';
import { leerSesion } from '@/lib/sesion';

type Usuario = {
    id: number;
    username: string;
    correo: string;
    rol: string;
    rolId: number;
    activo: boolean;
    aprobado: boolean;
    fechaCreacion: string;
    creadoPor: string | null;
    bloqueadoHasta: string | null;
    // null si no está bloqueado, o si el bloqueo ya venció:
    // el backend se encarga de mandarlo así.
};

type ErrorFila = { fila: number; errores: string[] };

const VACIO = { username: '', password: '', correo: '', rolId: 3 };

function hora(iso: string): string {
    return new Date(iso).toLocaleTimeString('es-EC', { hour: '2-digit', minute: '2-digit' });
    // Solo la hora: el bloqueo dura 15 minutos, la fecha sobra.
}

export default function UsuariosPage() {
    const [usuarios, setUsuarios] = useState<Usuario[]>([]);
    const [nuevo, setNuevo] = useState(VACIO);
    const [errores, setErrores] = useState<Record<string, string[]>>({});

    const [editando, setEditando] = useState<Usuario | null>(null);
    const [edicion, setEdicion] = useState({ correo: '', rolId: 3 });
    const [erroresEdicion, setErroresEdicion] = useState<Record<string, string[]>>({});
    // Errores APARTE para la edición. Los dos formularios tienen un campo
    // "correo": si compartieran estado, un error al editar aparecería
    // también debajo del correo del formulario de crear.

    const [mensaje, setMensaje] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [cargando, setCargando] = useState(true);
    const [ocupado, setOcupado] = useState(false);
    const [soyAdmin, setSoyAdmin] = useState(false);

    const [archivo, setArchivo] = useState<File | null>(null);
    const [erroresCarga, setErroresCarga] = useState<ErrorFila[] | null>(null);

    const cargar = useCallback(async () => {
        try {
            const r = await apiFetch('/api/usuarios');
            if (r.status === 403) { setError('No tienes permiso para ver usuarios'); return; }
            if (r.ok) setUsuarios(await r.json());
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setCargando(false);
        }
    }, []);

    useEffect(() => {
        setSoyAdmin(leerSesion()?.rol === 'Administrador');
        cargar();
    }, [cargar]);

    function limpiar() {
        setErrores({}); setErroresEdicion({});
        setMensaje(null); setError(null); setErroresCarga(null);
    }

    function erroresDe(fuente: Record<string, string[]>, campo: string): string[] {
        const clave = Object.keys(fuente).find((k) => k.toLowerCase() === campo.toLowerCase());
        return clave ? fuente[clave] : [];
    }
    // Recibe de qué objeto de errores leer. Así el mismo ayudante sirve
    // para el formulario de crear y para el de editar.

    async function crear() {
        setOcupado(true); limpiar();
        try {
            const r = await apiFetch('/api/usuarios', { method: 'POST', body: JSON.stringify(nuevo) });
            if (r.status === 403) { setError('No tienes permiso para crear usuarios'); return; }

            const d = await r.json();
            if (r.status === 400) { setErrores(d.errors ?? {}); setError('Revisa los campos'); return; }
            if (!r.ok) { setError(d.mensaje ?? 'No se pudo crear'); return; }

            setMensaje(d.mensaje);
            setNuevo(VACIO);
            await cargar();
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setOcupado(false);
        }
    }

    async function accion(ruta: string) {
        // Aprobar, activar/desactivar y desbloquear: los tres son un POST
        // sin datos que después recarga la tabla.
        setOcupado(true); limpiar();
        try {
            const r = await apiFetch(ruta, { method: 'POST' });
            if (r.status === 403) { setError('No tienes permiso para esta acción'); return; }
            const d = await r.json();
            if (!r.ok) { setError(d.mensaje ?? 'No se pudo completar'); return; }
            setMensaje(d.mensaje);
            await cargar();
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setOcupado(false);
        }
    }

    function empezarEdicion(u: Usuario) {
        limpiar();
        setEditando(u);
        setEdicion({ correo: u.correo, rolId: u.rolId });
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    async function guardarEdicion() {
        if (!editando) return;
        setOcupado(true); limpiar();

        try {
            const r = await apiFetch(`/api/usuarios/${editando.id}`, {
                method: 'PUT',
                body: JSON.stringify({ ...edicion, activo: editando.activo }),
                // El endpoint pide también "activo". Mando el valor que ya tenía:
                // activar y desactivar tienen su propio botón, y aquí solo se
                // edita el correo y el rol.
            });
            if (r.status === 403) { setError('Solo un administrador puede editar usuarios'); return; }

            const d = await r.json();
            if (r.status === 400) { setErroresEdicion(d.errors ?? {}); setError('Revisa los campos'); return; }
            if (!r.ok) { setError(d.mensaje ?? 'No se pudo guardar'); return; }

            setMensaje(d.mensaje);
            setEditando(null);
            await cargar();
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setOcupado(false);
        }
    }

    async function subir() {
        if (!archivo) { setError('Selecciona un archivo'); return; }
        setOcupado(true); limpiar();
        try {
            const formulario = new FormData();
            formulario.append('archivo', archivo);

            const r = await apiFetch('/api/usuarios/carga-masiva', { method: 'POST', body: formulario });
            if (r.status === 403) { setError('Solo un administrador puede importar usuarios'); return; }

            let d;
            try { d = await r.json(); }
            catch { setError(`El servidor respondió ${r.status} sin detalles`); return; }

            if (r.ok) { setMensaje(d.mensaje); setArchivo(null); await cargar(); return; }
            if (d.errores) { setErroresCarga(d.errores); setError(d.mensaje); }
            else setError(d.mensaje ?? 'No se pudo procesar el archivo');
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setOcupado(false);
        }
    }

    if (cargando) return <main className="p-8 text-gray-500">Cargando...</main>;

    const pendientes = usuarios.filter((u) => !u.aprobado);
    const bloqueados = usuarios.filter((u) => u.bloqueadoHasta !== null);

    return (
        <main className="p-8 max-w-6xl">
            <h1 className="text-2xl font-bold mb-6">Usuarios</h1>

            {mensaje && <div className="mb-4 p-3 rounded bg-green-100 text-green-800">{mensaje}</div>}
            {error && <div className="mb-4 p-3 rounded bg-red-100 text-red-800">{error}</div>}

            {pendientes.length > 0 && (
                <div className="mb-3 p-4 rounded bg-amber-50 border border-amber-200 text-sm text-amber-900">
                    <strong>{pendientes.length} usuario(s) pendiente(s) de aprobación.</strong>{' '}
                    {soyAdmin ? 'Apruébalos desde la tabla.' : 'Un administrador debe aprobarlos.'}
                </div>
            )}

            {soyAdmin && bloqueados.length > 0 && (
                <div className="mb-6 p-4 rounded bg-red-50 border border-red-200 text-sm text-red-900">
                    <strong>{bloqueados.length} usuario(s) bloqueado(s)</strong> por intentos fallidos.
                    Se desbloquean solos al vencer el plazo, o puedes hacerlo desde la tabla.
                </div>
            )}

            {/* ─── EDICIÓN (solo admin) ─── */}
            {editando && (
                <div className="bg-white border-2 border-blue-200 rounded-lg p-5 mb-6">
                    <h2 className="font-semibold mb-1">Editar usuario</h2>
                    <p className="text-sm text-gray-500 mb-4">
                        Usuario: <span className="font-mono">{editando.username}</span> (no se puede modificar)
                    </p>
                    {/* El username no se edita por la misma razón que la cédula:
              es la identidad que aparece en los registros históricos. */}

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div>
                            <label className="block text-xs text-gray-500 mb-1">Correo</label>
                            <input
                                value={edicion.correo}
                                onChange={(e) => setEdicion({ ...edicion, correo: e.target.value })}
                                className="w-full border rounded px-3 py-2 text-sm"
                            />
                            {erroresDe(erroresEdicion, 'correo').map((m) => (
                                <p key={m} className="text-red-600 text-xs mt-1">{m}</p>
                            ))}
                        </div>
                        <div>
                            <label className="block text-xs text-gray-500 mb-1">Rol</label>
                            <select
                                value={edicion.rolId}
                                onChange={(e) => setEdicion({ ...edicion, rolId: Number(e.target.value) })}
                                className="w-full border rounded px-3 py-2 text-sm"
                            >
                                <option value={3}>Cajero</option>
                                <option value={2}>Gestor</option>
                            </select>
                        </div>
                    </div>

                    <div className="flex gap-2 mt-4">
                        <button onClick={guardarEdicion} disabled={ocupado}
                            className="bg-blue-600 text-white rounded px-5 py-2 text-sm font-medium disabled:bg-gray-400">
                            {ocupado ? 'Guardando...' : 'Guardar cambios'}
                        </button>
                        <button onClick={() => setEditando(null)} className="border rounded px-5 py-2 text-sm">
                            Cancelar
                        </button>
                    </div>
                </div>
            )}

            {/* ─── CREAR ─── */}
            <div className="bg-white border rounded-lg p-5 mb-6">
                <h2 className="font-semibold mb-4">Crear usuario</h2>
                {!soyAdmin && (
                    <p className="text-sm text-amber-700 mb-4">
                        Como gestor puedes crear usuarios, pero un administrador deberá aprobarlos.
                    </p>
                )}

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                        <label className="block text-xs text-gray-500 mb-1">Nombre de usuario</label>
                        <input value={nuevo.username} placeholder="ejemplo: cajero0004"
                            onChange={(e) => setNuevo({ ...nuevo, username: e.target.value })}
                            className="w-full border rounded px-3 py-2 text-sm" />
                        {erroresDe(errores, 'username').map((m) => <p key={m} className="text-red-600 text-xs mt-1">{m}</p>)}
                    </div>
                    <div>
                        <label className="block text-xs text-gray-500 mb-1">Contraseña</label>
                        <input type="password" value={nuevo.password}
                            onChange={(e) => setNuevo({ ...nuevo, password: e.target.value })}
                            className="w-full border rounded px-3 py-2 text-sm" />
                        {erroresDe(errores, 'password').map((m) => <p key={m} className="text-red-600 text-xs mt-1">{m}</p>)}
                    </div>
                    <div>
                        <label className="block text-xs text-gray-500 mb-1">Correo</label>
                        <input value={nuevo.correo}
                            onChange={(e) => setNuevo({ ...nuevo, correo: e.target.value })}
                            className="w-full border rounded px-3 py-2 text-sm" />
                        {erroresDe(errores, 'correo').map((m) => <p key={m} className="text-red-600 text-xs mt-1">{m}</p>)}
                    </div>
                    <div>
                        <label className="block text-xs text-gray-500 mb-1">Rol</label>
                        <select value={nuevo.rolId}
                            onChange={(e) => setNuevo({ ...nuevo, rolId: Number(e.target.value) })}
                            className="w-full border rounded px-3 py-2 text-sm">
                            <option value={3}>Cajero</option>
                            <option value={2}>Gestor</option>
                        </select>
                    </div>
                </div>

                <button onClick={crear} disabled={ocupado}
                    className="mt-4 bg-blue-600 text-white rounded px-5 py-2 text-sm font-medium disabled:bg-gray-400">
                    {ocupado ? 'Procesando...' : 'Crear usuario'}
                </button>
            </div>

            {/* ─── CARGA MASIVA (solo admin) ─── */}
            {soyAdmin && (
                <div className="bg-white border rounded-lg p-5 mb-6">
                    <h2 className="font-semibold mb-2">Carga masiva</h2>
                    <p className="text-xs text-gray-500 mb-3">
                        Columnas en orden: Username · Password · Correo · RolId (2 = Gestor, 3 = Cajero)
                    </p>
                    <input type="file" accept=".xlsx,.csv"
                        onChange={(e) => { setArchivo(e.target.files?.[0] ?? null); limpiar(); }}
                        className="block w-full text-sm mb-3 file:mr-4 file:py-2 file:px-4 file:rounded
                            file:border-0 file:bg-blue-50 file:text-blue-700" />
                    <button onClick={subir} disabled={ocupado || !archivo}
                        className="bg-blue-600 text-white rounded px-5 py-2 text-sm font-medium disabled:bg-gray-400">
                        Importar
                    </button>

                    {erroresCarga && (
                        <table className="w-full mt-4 border rounded text-sm">
                            <thead className="bg-gray-100">
                                <tr>
                                    <th className="text-left px-3 py-2 w-16">Fila</th>
                                    <th className="text-left px-3 py-2">Problemas</th>
                                </tr>
                            </thead>
                            <tbody>
                                {erroresCarga.map((e) => (
                                    <tr key={e.fila} className="border-t">
                                        <td className="px-3 py-2 font-mono">{e.fila}</td>
                                        <td className="px-3 py-2">
                                            <ul className="list-disc list-inside">
                                                {e.errores.map((m) => <li key={m}>{m}</li>)}
                                            </ul>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                </div>
            )}

            {/* ─── TABLA ─── */}
            <h2 className="text-lg font-semibold mb-3">Todos los usuarios ({usuarios.length})</h2>

            <table className="w-full bg-white border rounded-lg overflow-hidden text-sm">
                <thead className="bg-gray-100">
                    <tr>
                        <th className="text-left px-4 py-2">Usuario</th>
                        <th className="text-left px-4 py-2">Correo</th>
                        <th className="text-left px-4 py-2">Rol</th>
                        <th className="text-left px-4 py-2">Estado</th>
                        <th className="text-left px-4 py-2">Creado por</th>
                        {soyAdmin && <th className="text-left px-4 py-2">Acciones</th>}
                    </tr>
                </thead>
                <tbody>
                    {usuarios.map((u) => (
                        <tr key={u.id} className="border-t">
                            <td className="px-4 py-2 font-medium">{u.username}</td>
                            <td className="px-4 py-2 text-gray-600">{u.correo}</td>
                            <td className="px-4 py-2">{u.rol}</td>
                            <td className="px-4 py-2">
                                <div className="flex gap-1 flex-wrap">
                                    <span className={`px-2 py-1 rounded text-xs ${u.activo ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-500'
                                        }`}>
                                        {u.activo ? 'Activo' : 'Inactivo'}
                                    </span>
                                    {!u.aprobado && (
                                        <span className="px-2 py-1 rounded text-xs bg-amber-100 text-amber-800">Sin aprobar</span>
                                    )}
                                    {u.bloqueadoHasta && (
                                        <span className="px-2 py-1 rounded text-xs bg-red-100 text-red-800">
                                            Bloqueado hasta {hora(u.bloqueadoHasta)}
                                        </span>
                                    )}
                                </div>
                            </td>
                            <td className="px-4 py-2 text-gray-500">{u.creadoPor ?? '—'}</td>
                            {soyAdmin && (
                                <td className="px-4 py-2">
                                    <div className="flex gap-2 flex-wrap">
                                        {!u.aprobado && (
                                            <button onClick={() => accion(`/api/usuarios/${u.id}/aprobar`)} disabled={ocupado}
                                                className="text-xs bg-green-600 text-white rounded px-3 py-1 disabled:bg-gray-400">
                                                Aprobar
                                            </button>
                                        )}
                                        {u.bloqueadoHasta && (
                                            <button onClick={() => accion(`/api/usuarios/${u.id}/desbloquear`)} disabled={ocupado}
                                                className="text-xs bg-red-600 text-white rounded px-3 py-1 disabled:bg-gray-400">
                                                Desbloquear
                                            </button>
                                        )}
                                        {u.rolId !== 1 && (
                                            <>
                                                <button onClick={() => empezarEdicion(u)}
                                                    className="text-xs border rounded px-3 py-1">
                                                    Editar
                                                </button>
                                                <button onClick={() => accion(`/api/usuarios/${u.id}/estado`)} disabled={ocupado}
                                                    className="text-xs border rounded px-3 py-1 disabled:text-gray-400">
                                                    {u.activo ? 'Desactivar' : 'Activar'}
                                                </button>
                                            </>
                                        )}
                                        {/* Los administradores no se editan ni se desactivan
                        desde aquí: el backend lo prohíbe igual. */}
                                    </div>
                                </td>
                            )}
                        </tr>
                    ))}
                </tbody>
            </table>
        </main>
    );
}