'use client';

import { useState } from 'react';
import { apiFetch } from '@/lib/api';

type ErrorFila = {
    fila: number;
    errores: string[];
};

type ResultadoCarga = {
    exitoso: boolean;
    filasLeidas: number;
    filasImportadas: number;
    errores: ErrorFila[];
    mensaje: string;
};

export default function CargaMasivaPage() {
    const [archivo, setArchivo] = useState<File | null>(null);
    const [resultado, setResultado] = useState<ResultadoCarga | null>(null);
    const [mensajeError, setMensajeError] = useState<string | null>(null);
    const [subiendo, setSubiendo] = useState(false);

    function manejarSeleccion(evento: React.ChangeEvent<HTMLInputElement>) {
        const seleccionado = evento.target.files?.[0] ?? null;
        // "files" es una lista porque un input puede aceptar varios.
        // Yo solo quiero el primero. El "?." evita reventar si es null.

        setArchivo(seleccionado);
        setResultado(null);
        setMensajeError(null);
        // Limpio los resultados anteriores: si dejo el reporte viejo en
        // pantalla junto a un archivo nuevo, el usuario se confunde.
    }

    async function subir() {
        if (!archivo) {
            setMensajeError('Selecciona un archivo primero');
            return;
        }

        setSubiendo(true);
        setResultado(null);
        setMensajeError(null);

        try {
            const formulario = new FormData();
            formulario.append('archivo', archivo);

            const respuesta = await apiFetch('/api/clientes/carga-masiva', {
                method: 'POST',
                body: formulario,
            });

            // ═══ EL 403 SE REVISA ANTES DE INTENTAR LEER EL CUERPO ═══
            if (respuesta.status === 403) {
                setMensajeError('Tu rol no tiene permiso para importar clientes');
                return;
            }
            // Antes esta comprobación estaba DESPUÉS del .json(), así que
            // nunca se alcanzaba: el .json() ya había reventado.
            //
            // Regla general: revisa el status ANTES de leer el cuerpo.
            // Un 403 o un 401 de middleware no traen cuerpo que leer.

            let datos: unknown;
            try {
                datos = await respuesta.json();
            } catch {
                setMensajeError(`El servidor respondió ${respuesta.status} sin detalles`);
                return;
            }
            // Un try anidado solo para el parseo. Así distingo
            // "el cuerpo no era JSON" de "falló la red".
            // Antes los dos casos daban el mismo mensaje equivocado.

            const resultadoTipado = datos as ResultadoCarga & { mensaje?: string };

            if (respuesta.ok) {
                setResultado(resultadoTipado);
                setArchivo(null);
                return;
            }

            if (resultadoTipado.errores) {
                setResultado(resultadoTipado);
            } else {
                setMensajeError(resultadoTipado.mensaje ?? 'No se pudo procesar el archivo');
            }

        } catch {
            setMensajeError('No se pudo conectar con el servidor');
            // Ahora este catch SOLO atrapa fallos de red de verdad:
            // servidor apagado, sin internet, CORS bloqueado.
        } finally {
            setSubiendo(false);
        }
    }

    return (
        <main className="max-w-3xl p-8">
            <h1 className="text-2xl font-bold mb-2">Carga masiva de clientes</h1>
            <p className="text-gray-600 mb-6 text-sm">
                Sube un archivo .xlsx o .csv. La primera fila debe ser de encabezados,
                en este orden: Nombres, Apellidos, Identificación, Correo, Teléfono,
                Dirección, Referencia.
            </p>

            <div className="bg-amber-50 border border-amber-200 rounded p-4 mb-6 text-sm text-amber-900">
                <strong>Importante:</strong> formatea como Texto las columnas de
                identificación y teléfono antes de escribirlas, o Excel borrará el
                cero del inicio.
            </div>
            {/* El aviso que te habría ahorrado los dos últimos mensajes.
          Cuando conoces una trampa, avisarle al usuario es parte del trabajo. */}

            <div className="bg-white border rounded-lg p-6 mb-6">
                <input
                    type="file"
                    accept=".xlsx,.csv"
                    onChange={manejarSeleccion}
                    className="block w-full text-sm mb-4
                     file:mr-4 file:py-2 file:px-4 file:rounded file:border-0
                     file:bg-blue-50 file:text-blue-700 file:font-medium
                     hover:file:bg-blue-100"
                />
                {/* accept solo FILTRA lo que muestra el explorador de archivos.
            No es seguridad: el usuario puede elegir "todos los archivos".
            La validación de verdad es la del backend, que revisa la
            extensión. Capa 1 y capa 2, otra vez. */}

                {archivo && (
                    <p className="text-sm text-gray-600 mb-4">
                        Seleccionado: <strong>{archivo.name}</strong>{' '}
                        ({(archivo.size / 1024).toFixed(1)} KB)
                    </p>
                )}

                <button
                    onClick={subir}
                    disabled={subiendo || !archivo}
                    className="bg-blue-600 text-white rounded px-5 py-2 font-medium
                     disabled:bg-gray-400 disabled:cursor-not-allowed"
                >
                    {subiendo ? 'Importando...' : 'Importar clientes'}
                </button>
            </div>

            {mensajeError && (
                <div className="mb-4 p-3 rounded bg-red-100 text-red-800">
                    {mensajeError}
                </div>
            )}

            {resultado?.exitoso && (
                <div className="p-4 rounded bg-green-100 text-green-800">
                    <p className="font-medium">{resultado.mensaje}</p>
                    <p className="text-sm mt-1">
                        Filas leídas: {resultado.filasLeidas} · Importadas: {resultado.filasImportadas}
                    </p>
                </div>
            )}
            {/* El "?." antes de .exitoso: si resultado es null, toda la
          expresión da undefined y no se dibuja nada, en vez de reventar. */}

            {resultado && !resultado.exitoso && (
                <div>
                    <div className="p-4 rounded bg-red-100 text-red-800 mb-4">
                        <p className="font-medium">{resultado.mensaje}</p>
                        <p className="text-sm mt-1">
                            Corrige el archivo y vuelve a subirlo completo.
                        </p>
                    </div>

                    <table className="w-full bg-white border rounded-lg overflow-hidden text-sm">
                        <thead className="bg-gray-100">
                            <tr>
                                <th className="text-left px-4 py-2 w-20">Fila</th>
                                <th className="text-left px-4 py-2">Problemas encontrados</th>
                            </tr>
                        </thead>
                        <tbody>
                            {resultado.errores.map((e) => (
                                <tr key={e.fila} className="border-t">
                                    <td className="px-4 py-2 font-mono font-medium align-top">
                                        {e.fila}
                                    </td>
                                    <td className="px-4 py-2">
                                        <ul className="list-disc list-inside">
                                            {e.errores.map((mensaje) => (
                                                <li key={mensaje}>{mensaje}</li>
                                            ))}
                                        </ul>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                    {/* Ese número de fila es lo que hace útil este reporte:
              el usuario abre su Excel, va a esa fila y la corrige.
              Por eso en el backend guardamos RowNumber() y no un índice. */}
                </div>
            )}
        </main>
    );
}