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
};

type ErrorFila = { fila: number; errores: string[] };

const VACIO = { username: '', password: '', correo: '', rolId: 3 };

export default function UsuariosPage() {
    const [usuarios, setUsuarios] = useState<Usuario[]>([]);
    const [nuevo, setNuevo] = useState(VACIO);
    const [errores, setErrores] = useState<Record<string, string[]>>({});
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
        const sesion = leerSesion();
        setSoyAdmin(sesion?.rol === 'Administrador');
        // Esto SOLO decide qué botones dibujar. La seguridad real es el
        // [Authorize(Roles = "Administrador")] de cada endpoint: si un
        // gestor forzara la petición, el backend responde 403 igual.
        cargar();
    }, [cargar]);

    function limpiar() {
        setErrores({}); setMensaje(null); setError(null); setErroresCarga(null);
    }

    async function crear() {
        setOcupado(true); limpiar();
        try {
            const r = await apiFetch('/api/usuarios', {
                method: 'POST',
                body: JSON.stringify(nuevo),
            });

            if (r.status === 403) { setError('No tienes permiso para crear usuarios'); return; }

            const datos = await r.json();

            if (r.status === 400) { setErrores(datos.errors ?? {}); setError('Revisa los campos'); return; }
            if (!r.ok) { setError(datos.mensaje ?? 'No se pudo crear'); return; }

            setMensaje(datos.mensaje);
            setNuevo(VACIO);
            await cargar();
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setOcupado(false);
        }
    }

    async function accion(ruta: string) {
        setOcupado(true); limpiar();
        try {
            const r = await apiFetch(ruta, { method: 'POST' });
            if (r.status === 403) { setError('No tienes permiso para esta acción'); return; }
            const datos = await r.json();
            if (!r.ok) { setError(datos.mensaje ?? 'No se pudo completar'); return; }
            setMensaje(datos.mensaje);
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

            const r = await apiFetch('/api/usuarios/carga-masiva', {
                method: 'POST', body: formulario,
            });

            if (r.status === 403) { setError('Solo un administrador puede importar usuarios'); return; }

            let datos;
            try { datos = await r.json(); }
            catch { setError(`El servidor respondió ${r.status} sin detalles`); return; }

            if (r.ok) { setMensaje(datos.mensaje); setArchivo(null); await cargar(); return; }
            if (datos.errores) { setErroresCarga(datos.errores); setError(datos.mensaje); }
            else setError(datos.mensaje ?? 'No se pudo procesar el archivo');
        } catch {
            setError('No se pudo conectar con el servidor');
        } finally {
            setOcupado(false);
        }
    }

    function erroresDe(campo: string): string[] {
        const clave = Object.keys(errores).find((k) => k.toLowerCase() === campo.toLowerCase());
        return clave ? errores[clave] : [];
    }

    if (cargando) return <main className="p-8 text-gray-500">Cargando...</main>;

    const pendientes = usuarios.filter((u) => !u.aprobado);

    return (
        <main className="p-8 max-w-5xl">
            <h1 className="text-2xl font-bold mb-6">Usuarios</h1>

            {mensaje && <div className="mb-4 p-3 rounded bg-green-100 text-green-800">{mensaje}</div>}
            {error && <div className="mb-4 p-3 rounded bg-red-100 text-red-800">{error}</div>}

            {pendientes.length > 0 && (
                <div className="mb-6 p-4 rounded bg-amber-50 border border-amber-200 text-sm text-amber-900">
                    <strong>{pendientes.length} usuario(s) pendiente(s) de aprobación.</strong>{' '}
                    {soyAdmin
                        ? 'Apruébalos desde la tabla para que puedan entrar.'
                        : 'Un administrador debe aprobarlos antes de que puedan entrar.'}
                </div>
            )}

            {/* ─── CREAR ─── */}
            <div className="bg-white border rounded-lg p-5 mb-6">
                <h2 className="font-semibold mb-4">Crear usuario</h2>

                {!soyAdmin && (
                    <p className="text-sm text-amber-700 mb-4">
                        Como gestor puedes crear usuarios, pero un administrador
                        deberá aprobarlos antes de que puedan iniciar sesión.
                    </p>
                )}

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                        <label className="block text-xs text-gray-500 mb-1">Nombre de usuario</label>
                        <input
                            value={nuevo.username}
                            onChange={(e) => setNuevo({ ...nuevo, username: e.target.value })}
                            placeholder="ejemplo: cajero0004"
                            className="w-full border rounded px-3 py-2 text-sm"
                        />
                        {erroresDe('username').map((m) => (
                            <p key={m} className="text-red-600 text-xs mt-1">{m}</p>
                        ))}
                    </div>

                    <div>
                        <label className="block text-xs text-gray-500 mb-1">Contraseña</label>
                        <input
                            type="password"
                            value={nuevo.password}
                            onChange={(e) => setNuevo({ ...nuevo, password: e.target.value })}
                            className="w-full border rounded px-3 py-2 text-sm"
                        />
                        {erroresDe('password').map((m) => (
                            <p key={m} className="text-red-600 text-xs mt-1">{m}</p>
                        ))}
                    </div>

                    <div>
                        <label className="block text-xs text-gray-500 mb-1">Correo</label>
                        <input
                            value={nuevo.correo}
                            onChange={(e) => setNuevo({ ...nuevo, correo: e.target.value })}
                            className="w-full border rounded px-3 py-2 text-sm"
                        />
                        {erroresDe('correo').map((m) => (
                            <p key={m} className="text-red-600 text-xs mt-1">{m}</p>
                        ))}
                    </div>

                    <div>
                        <label className="block text-xs text-gray-500 mb-1">Rol</label>
                        <select
                            value={nuevo.rolId}
                            onChange={(e) => setNuevo({ ...nuevo, rolId: Number(e.target.value) })}
                            className="w-full border rounded px-3 py-2 text-sm"
                        >
                            <option value={3}>Cajero</option>
                            <option value={2}>Gestor</option>
                        </select>
                        {/* No hay opción de Administrador, y el backend además lo
                bloquea con [Range(2,3)]. Capa 1 y capa 2 sobre la misma
                regla: el select no lo ofrece, la API no lo acepta. */}
                    </div>
                </div>

                <button
                    onClick={crear}
                    disabled={ocupado}
                    className="mt-4 bg-blue-600 text-white rounded px-5 py-2 text-sm font-medium disabled:bg-gray-400"
                >
                    {ocupado ? 'Creando...' : 'Crear usuario'}
                </button>
            </div>

            {/* ─── CARGA MASIVA ─── */}
            {soyAdmin && (
                <div className="bg-white border rounded-lg p-5 mb-6">
                    <h2 className="font-semibold mb-2">Carga masiva</h2>
                    <p className="text-xs text-gray-500 mb-3">
                        Columnas en orden: Username · Password · Correo · RolId (2 = Gestor, 3 = Cajero)
                    </p>
                    <input
                        type="file"
                        accept=".xlsx,.csv"
                        onChange={(e) => { setArchivo(e.target.files?.[0] ?? null); limpiar(); }}
                        className="block w-full text-sm mb-3 file:mr-4 file:py-2 file:px-4 file:rounded
                       file:border-0 file:bg-blue-50 file:text-blue-700"
                    />
                    <button
                        onClick={subir}
                        disabled={ocupado || !archivo}
                        className="bg-blue-600 text-white rounded px-5 py-2 text-sm font-medium disabled:bg-gray-400"
                    >
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
                                        <span className="px-2 py-1 rounded text-xs bg-amber-100 text-amber-800">
                                            Sin aprobar
                                        </span>
                                    )}
                                </div>
                            </td>
                            <td className="px-4 py-2 text-gray-500">{u.creadoPor ?? '—'}</td>
                            {soyAdmin && (
                                <td className="px-4 py-2">
                                    <div className="flex gap-2">
                                        {!u.aprobado && (
                                            <button
                                                onClick={() => accion(`/api/usuarios/${u.id}/aprobar`)}
                                                disabled={ocupado}
                                                className="text-xs bg-green-600 text-white rounded px-3 py-1 disabled:bg-gray-400"
                                            >
                                                Aprobar
                                            </button>
                                        )}
                                        {u.rolId !== 1 && (
                                            <button
                                                onClick={() => accion(`/api/usuarios/${u.id}/estado`)}
                                                disabled={ocupado}
                                                className="text-xs border rounded px-3 py-1 disabled:text-gray-400"
                                            >
                                                {u.activo ? 'Desactivar' : 'Activar'}
                                            </button>
                                        )}
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