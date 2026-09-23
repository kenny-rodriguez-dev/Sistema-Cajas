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

// ─── USUARIOS ───
export interface UsuarioLista {
    id: number;
    username: string;
    correo: string;
    rol: string;
    rolId: number;
    activo: boolean;
    aprobado: boolean;
    fechaCreacion: string;
    creadoPor: string | null;
}

export interface CrearUsuarioDto {
    username: string;
    password: string;
    correo: string;
    rolId: number;
}

// ─── DASHBOARD ───
export interface IndicadoresUsuarios {
    total: number;
    activos: number;
    inactivos: number;
    pendientesAprobacion: number;
    conSesionAbierta: number;
    gestores: number;
    cajeros: number;
}

export interface IndicadorCaja {
    caja: string;
    cajerosAsignados: number;
    turnosAtendidos: number;
    ocupadaPor: string | null;
}

export interface DashboardDto {
    usuarios: IndicadoresUsuarios;
    cajas: IndicadorCaja[];
    turnosEmitidos: number;
    turnosAtendidos: number;
    turnosPendientes: number;
    contratosActivos: number;
}

// ─── TURNOS Y CAJAS ───
export interface CajaLista {
    id: number;
    nombre: string;
}

export interface CajeroAsignado {
    id: number;
    username: string;
}

export interface CajaDetalle extends CajaLista {
    cajerosAsignados: CajeroAsignado[];
    ocupadaPor: string | null;
}
// "extends" hereda id y nombre de CajaLista y agrega lo demás.
// La pantalla de turnos sigue usando CajaLista sin enterarse del cambio.

export interface TipoAtencion {
    id: number;
    codigo: string;        // "AC", "PS"
    descripcion: string;
}

export interface Turno {
    id: number;
    codigo: string;        // "AC0001"
    tipoAtencion: string;
    caja: string;
    estado: string;        // PEN | ATE | CAN
    fechaHoraEmision: string;
    atendidoPor: string | null;
}

export interface MiCaja {
    id: number;
    nombre: string;
    ocupadaPor: string | null;
    esMia: boolean;
}

export interface MisCajasRespuesta {
    miSesionCajaId: number | null;   // null = no tengo ninguna abierta
    cajas: MiCaja[];
}