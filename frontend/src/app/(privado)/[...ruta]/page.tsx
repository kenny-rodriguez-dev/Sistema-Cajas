'use client';

import { usePathname } from 'next/navigation';

export default function EnConstruccionPage() {
    const pathname = usePathname();

    return (
        <main className="p-8">
            <h1 className="text-2xl font-bold mb-2">En construcción</h1>
            <p className="text-gray-600">
                La pantalla <code className="bg-gray-200 px-2 py-1 rounded">{pathname}</code> se
                construye en una rebanada siguiente.
            </p>
        </main>
    );
}
// [...ruta] es una "ruta comodín": atrapa cualquier dirección que no
// coincida con una página real. Next.js siempre prefiere la coincidencia
// exacta, así que /clientes/nuevo y /bienvenida siguen funcionando.
//
// Así puedo tener el menú completo desde ya, con las pantallas
// pendientes claramente marcadas en vez de dando error 404.