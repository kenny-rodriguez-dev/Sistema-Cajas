'use client';

import { useEffect, useState } from 'react';
import { leerSesion } from '@/lib/sesion';

export default function BienvenidaPage() {
    const [nombre, setNombre] = useState('');
    const [rol, setRol] = useState('');

    useEffect(() => {
        const sesion = leerSesion();
        if (sesion) {
            setNombre(sesion.username);
            setRol(sesion.rol);
        }
        // No hace falta guardia aquí: el layout ya la protegió.
    }, []);

    return (
        <main className="p-8">
            <h1 className="text-2xl font-bold mb-2">Bienvenido, {nombre}</h1>
            <p className="text-gray-600 mb-8">Tu rol en el sistema es: {rol}</p>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 max-w-3xl">
                <div className="bg-white p-5 rounded-lg border">
                    <p className="text-sm text-gray-500">Turnos atendidos hoy</p>
                    <p className="text-3xl font-bold mt-1 text-gray-300">—</p>
                </div>
                <div className="bg-white p-5 rounded-lg border">
                    <p className="text-sm text-gray-500">Usuarios pendientes</p>
                    <p className="text-3xl font-bold mt-1 text-gray-300">—</p>
                </div>
                <div className="bg-white p-5 rounded-lg border">
                    <p className="text-sm text-gray-500">Cajas activas</p>
                    <p className="text-3xl font-bold mt-1 text-gray-300">—</p>
                </div>
            </div>

            <p className="text-sm text-gray-400 mt-6">
                Los indicadores se conectan en la rebanada 7.
            </p>
            {/* Dejo los huecos visibles a propósito: el enunciado pide estos
          indicadores y así el pendiente queda a la vista, no olvidado. */}
        </main>
    );
}