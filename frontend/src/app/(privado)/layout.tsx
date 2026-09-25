'use client';

import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import Link from 'next/link';
import { apiFetch } from '@/lib/api';
import { leerSesion, borrarSesion } from '@/lib/sesion';

type OpcionMenu = {
    etiqueta: string;
    ruta: string;
};

export default function LayoutPrivado({
    children,
}: {
    children: React.ReactNode;
}) {
    // "children" son las páginas que este layout envuelve.
    // Yo dibujo el sidebar y donde ponga {children} se pinta la página
    // que corresponda a la URL actual.

    const router = useRouter();
    const pathname = usePathname();
    // usePathname me da la URL actual. La uso para resaltar la opción activa.

    const [cargando, setCargando] = useState(true);
    const [menu, setMenu] = useState<OpcionMenu[]>([]);
    const [usuario, setUsuario] = useState('');
    const [rol, setRol] = useState('');

    useEffect(() => {
        const sesion = leerSesion();

        if (!sesion) {
            router.replace('/login');
            return;
        }
        // GUARDIA ÚNICO. Antes esto estaba dentro de la página de clientes.
        // Ahora vive aquí y cubre TODAS las pantallas privadas.
        // Una pantalla nueva queda protegida solo por estar en esta carpeta.

        setUsuario(sesion.username);
        setRol(sesion.rol);

        // Pido el menú al backend.
        apiFetch('/api/menu')
            .then((respuesta) => {
                if (!respuesta.ok) throw new Error('No se pudo cargar el menú');
                return respuesta.json();
            })
            .then((opciones: OpcionMenu[]) => setMenu(opciones))
            .catch(() => setMenu([]))
            // Si falla, dejo el menú vacío en vez de reventar la aplicación.
            // El 401 por token vencido ya lo maneja apiFetch por su cuenta.
            .finally(() => setCargando(false));
    }, [router]);

    async function cerrarSesion() {
        try {
            await apiFetch('/api/auth/logout', { method: 'POST' });
        } catch {
            // Si no hay red, igual cerramos la sesión en este navegador.
            // El usuario pidió salir: no lo dejamos atrapado por un fallo de conexión.
        }
        borrarSesion();
        router.replace('/login');
    }
    // ═══ EL ORDEN ES TODO ═══
    // Primero se avisa al backend, DESPUÉS se borra el token local.
    //
    // Al revés no funciona: si borrara primero, apiFetch ya no encontraría
    // el token para pegarlo en la cabecera, y la petición de logout llegaría
    // sin identificar. El backend respondería 401 y la sesión seguiría viva
    // en el servidor, que es justo lo que queremos evitar.

    if (cargando) {
        return (
            <div className="min-h-screen flex items-center justify-center text-gray-500">
                Cargando...
            </div>
        );
        // El "loading" que pide el enunciado. Y evita que la pantalla
        // parpadee mostrando un sidebar vacío antes de tener los datos.
    }

    return (
        <div className="min-h-screen flex">

            {/* ─── SIDEBAR ─── */}
            <aside className="w-64 bg-slate-800 text-slate-100 flex flex-col">
                <div className="p-5 border-b border-slate-700">
                    <h2 className="font-bold text-lg">Gestión de Caja</h2>
                    <p className="text-sm text-slate-400 mt-1">{usuario}</p>
                    <span className="inline-block mt-2 text-xs bg-slate-700 px-2 py-1 rounded">
                        {rol}
                    </span>
                </div>

                <nav className="flex-1 p-3">
                    {menu.map((opcion) => {
                        const activa = pathname === opcion.ruta;

                        return (
                            <Link
                                key={opcion.ruta}
                                href={opcion.ruta}
                                className={`block px-3 py-2 rounded mb-1 text-sm ${activa
                                    ? 'bg-blue-600 text-white'
                                    : 'text-slate-300 hover:bg-slate-700'
                                    }`}
                            >
                                {opcion.etiqueta}
                            </Link>
                        );
                        // <Link> en vez de <a>: navega SIN recargar la página entera.
                        // Con un <a> normal se perdería todo el estado y la aplicación
                        // se sentiría lenta.
                        //
                        // ═══ Y AQUÍ ESTÁ EL PUNTO DE TODA LA REBANADA ═══
                        // Este .map no sabe NADA de roles. No hay un solo "if" que
                        // pregunte si eres administrador. Solo pinta lo que el backend
                        // le mandó, y el backend ya filtró según el token.
                    })}

                    {menu.length === 0 && (
                        <p className="text-slate-500 text-sm px-3">Sin opciones</p>
                    )}
                </nav>

                <div className="p-3 border-t border-slate-700">
                    <button
                        onClick={cerrarSesion}
                        className="w-full text-left px-3 py-2 rounded text-sm
                       text-slate-300 hover:bg-slate-700"
                    >
                        Cerrar sesión
                    </button>
                </div>
            </aside>

            {/* ─── CONTENIDO ─── */}
            <div className="flex-1 bg-gray-50">
                {children}
            </div>
        </div>
    );
}