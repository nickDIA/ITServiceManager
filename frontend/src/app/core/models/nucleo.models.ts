import { RolTecnico } from './auth.models';

// ----------------------------------------------------------------- Enums (espejo del backend)

export type TipoActivo = 'Hardware' | 'Software' | 'EquipoRed';
export type EstadoActivo = 'Operativo' | 'EnReparacion' | 'EnAlmacen' | 'Retirado';
export type Prioridad = 'Baja' | 'Media' | 'Alta' | 'Critica';
export type EstadoTicket = 'Abierto' | 'EnProgreso' | 'Resuelto' | 'Cerrado' | 'Cancelado';

export const TIPOS_ACTIVO: TipoActivo[] = ['Hardware', 'Software', 'EquipoRed'];
export const PRIORIDADES: Prioridad[] = ['Baja', 'Media', 'Alta', 'Critica'];
export const ESTADOS_TICKET: EstadoTicket[] = ['Abierto', 'EnProgreso', 'Resuelto', 'Cerrado', 'Cancelado'];

/**
 * Espejo de Domain/EstadoActivoTransiciones.cs y EstadoTicketTransiciones.cs del backend:
 * el frontend las usa solo para PINTAR las opciones válidas; la validación real sigue
 * siendo del backend (409 si algo no cuadra).
 */
export const TRANSICIONES_ACTIVO: Record<EstadoActivo, EstadoActivo[]> = {
  Operativo: ['EnReparacion', 'EnAlmacen', 'Retirado'],
  EnReparacion: ['Operativo', 'EnAlmacen', 'Retirado'],
  EnAlmacen: ['Operativo', 'EnReparacion', 'Retirado'],
  Retirado: []
};

export const TRANSICIONES_TICKET: Record<EstadoTicket, EstadoTicket[]> = {
  Abierto: ['EnProgreso', 'Cancelado'],
  EnProgreso: ['Resuelto'],
  Resuelto: ['Cerrado'],
  Cerrado: [],
  Cancelado: []
};

/** Espejo de ResultadoPaginadoDto<T> del backend. */
export interface ResultadoPaginado<T> {
  items: T[];
  pagina: number;
  tamanoPagina: number;
  totalRegistros: number;
  hayMas: boolean;
}

// ----------------------------------------------------------------- Cliente

export interface Cliente {
  id: number;
  nombre: string;
  rfc: string;
  contacto: string | null;
  telefono: string | null;
  activo: boolean;
}

export interface CrearCliente {
  nombre: string;
  rfc: string;
  contacto: string | null;
  telefono: string | null;
}

export interface ActualizarCliente extends CrearCliente {
  activo: boolean;
}

// ----------------------------------------------------------------- Activo

export interface Activo {
  id: number;
  clienteId: number;
  clienteNombre: string;
  tipo: TipoActivo;
  nombre: string;
  numeroSerie: string;
  estado: EstadoActivo;
  fechaAdquisicion: string;
}

export interface CrearActivo {
  clienteId: number;
  tipo: TipoActivo;
  nombre: string;
  numeroSerie: string;
  fechaAdquisicion: string;
}

export interface HistorialActivo {
  id: number;
  activoId: number;
  estadoAnterior: EstadoActivo;
  estadoNuevo: EstadoActivo;
  motivo: string;
  fecha: string;
  tecnicoId: number;
  tecnicoNombre: string;
}

// ----------------------------------------------------------------- Ticket

export interface Ticket {
  id: number;
  clienteId: number;
  clienteNombre: string;
  activoId: number | null;
  activoNombre: string | null;
  titulo: string;
  descripcion: string;
  prioridad: Prioridad;
  estado: EstadoTicket;
  tecnicoId: number;
  tecnicoNombre: string;
  fechaCreacion: string;
  fechaCierre: string | null;
  /** SLA de respuesta (horas) del contrato activo del cliente. Null si no tiene contrato activo. */
  slaHoras: number | null;
}

export interface CrearTicket {
  clienteId: number;
  activoId: number | null;
  titulo: string;
  descripcion: string;
  prioridad: Prioridad;
  tecnicoId: number;
}

// ----------------------------------------------------------------- Técnico y reportes

export interface Tecnico {
  id: number;
  nombre: string;
  email: string;
  rol: RolTecnico;
}

export interface ReporteDashboard {
  clientesActivos: number;
  ticketsAbiertos: number;
  clientesSinTicketsAbiertos: number;
  ingresosMensualesRecurrentes: number;
  promedioHorasIncluidasContratos: number;
  activosPorEstado: Partial<Record<EstadoActivo, number>>;
  ticketsPorEstado: Partial<Record<EstadoTicket, number>>;
  ticketsPorPrioridad: Partial<Record<Prioridad, number>>;
}
