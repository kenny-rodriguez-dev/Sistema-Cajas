'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { leerSesion } from '@/lib/sesion';

export default function Home() {
  const router = useRouter();

  useEffect(() => {
    const sesion = leerSesion();
    router.replace(sesion ? '/bienvenida' : '/login');
    // Si ya entró, a bienvenida. Si no, al login.
    // Es un operador ternario: condición ? esto : lo otro.
  }, [router]);

  return (
    <main className="min-h-screen flex items-center justify-center text-gray-500">
      Cargando...
    </main>
  );
}