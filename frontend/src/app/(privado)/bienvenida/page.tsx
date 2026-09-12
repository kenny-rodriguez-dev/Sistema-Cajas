'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

type Bienvenida = {
    username: string;
    rol: string;
    correo: string;
    miembroDesde: string;
    turnosAtendidosHoy: number;
    descripcionTurnos: string;
    usuariosPendientesAprobacion: number | null;
};
// Fíjate que miembroDesde es "string", no Date.
// El JSON no tiene tipo fecha: todo viaja como texto.
// Lo convierto a Date solo cuando lo voy a mostrar.

function Tarjeta({
    etiqueta,
    valor,
    detalle,
}: {
    etiqueta: string;
    valor: string | number;
    detalle?: string;
}) {
    return (
        <div className="bg-white p-5 rounded-lg border">
            <p className="text-sm text-gray-500">{etiqueta}</p>
            <p className="text-3xl font-bold mt-1">{valor}</p>
            {detalle && <p className="text-xs text-gray-400 mt-2">{detalle}</p>}
        </div>
    );
    // El "?" en detalle lo hace opcional. Si no lo paso, no se dibuja.
}

export default function BienvenidaPage() {
    const [datos, setDatos] = useState<Bienvenida | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [cargando, setCargando] = useState(true);

    useEffect(() => {
        apiFetch('/api/dashboard/bienvenida')
            .then(async (respuesta) => {
                if (!respuesta.ok) throw new Error(`El servidor respondió ${respuesta.status}`);
                setDatos(await respuesta.json());
            })
            .catch((e) => setError(e.message))
            .finally(() => setCargando(false));
    }, []);
    // Ya no leo la sesión de localStorage para mostrar el nombre.
    // Ahora los datos vienen del backend, que los saca del token.
    //
    // ¿Por qué el cambio? Porque localStorage lo puede editar cualquiera
    // desde las herramientas del navegador. Si mañana muestro algo que
    // dependa del rol, quiero que ese rol venga verificado, no del disco
    // del usuario.

    if (cargando) return <main className="p-8 text-gray-500">Cargando...</main>;

    if (error || !datos) {
        return (
            <main className="p-8">
                <div className="p-3 rounded bg-red-100 text-red-800">
                    No se pudieron cargar tus datos. {error}
                </div>
            </main>
        );
    }

    return (
        <main className="p-8">
            <h1 className="text-2xl font-bold mb-1">Bienvenido, {datos.username}</h1>
            <p className="text-gray-600 mb-8">
                {datos.rol} · {datos.correo} · Miembro desde{' '}
                {new Date(datos.miembroDesde).toLocaleDateString('es-EC')}
            </p>
            {/* toLocaleDateString con 'es-EC' formatea a la ecuatoriana:
          11/9/2026 y no 9/11/2026 como sería en Estados Unidos.
          Y aquí es donde importa la "Z" que descubriste: sin ella,
          esta línea mostraría la fecha corrida cinco horas. */}

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 max-w-4xl">
                <Tarjeta
                    etiqueta="Turnos atendidos hoy"
                    valor={datos.turnosAtendidosHoy}
                    detalle={datos.descripcionTurnos}
                />
                {/* La descripción viene del backend, no está escrita aquí.
            Por eso el mismo componente sirve para los tres roles
            sin un solo "if" sobre el rol. */}

                {datos.usuariosPendientesAprobacion !== null && (
                    <Tarjeta
                        etiqueta="Usuarios pendientes"
                        valor={datos.usuariosPendientesAprobacion}
                        detalle="Esperan aprobación de un administrador"
                    />
                )}
                {/* Comparo con !== null a propósito, y no con un simple
            "if (valor)". Si el conteo fuera 0, JavaScript lo trata
            como falso y la tarjeta desaparecería... justo cuando la
            respuesta correcta es mostrar un cero.
            Ese error tiene nombre: falsy values. Muerde seguido. */}
            </div>
        </main>
    );
}