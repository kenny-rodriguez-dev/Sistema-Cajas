// Las mismas formas de datos que en React, solo que aquí uso
// "interface" en vez de "type". En TypeScript son casi equivalentes;
// en Angular la convención es interface.

export interface RespuestaLogin {
    token: string;
    username: string;
    rol: string;
    expira: string;
}

export interface Sesion {
    token: string;
    username: string;
    rol: string;
}

export interface OpcionMenu {
    etiqueta: string;
    ruta: string;
}

export interface BienvenidaDto {   // ← antes decía "Bienvenida"
    username: string;
    rol: string;
    correo: string;
    miembroDesde: string;
    turnosAtendidosHoy: number;
    descripcionTurnos: string;
    usuariosPendientesAprobacion: number | null;
}
// El sufijo Dto deja claro que es una forma de datos que viene de la
// API, no un componente. Y es el mismo nombre que la clase de C#,
// así que las dos puntas del sistema hablan igual.
// ─── CLIENTES ───
export interface CrearClienteDto {
    nombres: string;
    apellidos: string;
    identificacion: string;
    correo: string;
    telefono: string;
    direccion: string;
    referenciaDireccion: string;
}

// ─── CARGA MASIVA ───
export interface ErrorFila {
    fila: number;          // el número de fila tal como se ve en Excel
    errores: string[];
}

export interface ResultadoCarga {
    exitoso: boolean;
    filasLeidas: number;
    filasImportadas: number;
    errores: ErrorFila[];
    mensaje: string;
}