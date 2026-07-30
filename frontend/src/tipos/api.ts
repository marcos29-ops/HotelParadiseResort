/**
 * Contratos de la API REST. Reflejan exactamente los DTO del backend para que el
 * compilador detecte cualquier divergencia entre ambos lados.
 */

export interface ResultadoPaginado<T> {
  elementos: T[];
  totalRegistros: number;
  pagina: number;
  tamanoPagina: number;
  totalPaginas: number;
  tienePaginaAnterior: boolean;
  tienePaginaSiguiente: boolean;
}

export interface ParametrosPaginacion {
  pagina?: number;
  tamanoPagina?: number;
  busqueda?: string;
  ordenarPor?: string;
  descendente?: boolean;
}

/** Formato ProblemDetails que devuelve el manejador global de errores. */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

// --- Autenticación -----------------------------------------------------------

export interface UsuarioAutenticado {
  id: number;
  nombre: string;
  nombreUsuario: string;
  correo: string;
  rol: RolUsuario;
}

export type RolUsuario = 'Administrador' | 'Recepcionista';

export interface RespuestaInicioSesion {
  token: string;
  expiracion: string;
  usuario: UsuarioAutenticado;
}

export interface SolicitudInicioSesion {
  nombreUsuario: string;
  contrasena: string;
}

// --- Clientes ----------------------------------------------------------------

export interface Cliente {
  id: number;
  identificacion: string;
  nombre: string;
  apellidos: string;
  nombreCompleto: string;
  correo?: string | null;
  telefono?: string | null;
  nacionalidad?: string | null;
  fechaNacimiento?: string | null;
  activo: boolean;
  cantidadReservas: number;
}

export interface CrearCliente {
  identificacion: string;
  nombre: string;
  apellidos: string;
  correo?: string | null;
  telefono?: string | null;
  nacionalidad?: string | null;
  fechaNacimiento?: string | null;
}

export type ActualizarCliente = Omit<CrearCliente, 'identificacion'> & { activo: boolean };

export interface HistorialCliente {
  id: number;
  accion: string;
  detalle: string;
  usuario: string;
  fechaRegistro: string;
}

// --- Habitaciones ------------------------------------------------------------

export type EstadoHabitacion =
  | 'Disponible'
  | 'Reservada'
  | 'Ocupada'
  | 'EnLimpieza'
  | 'EnMantenimiento';

export interface Habitacion {
  id: number;
  numero: string;
  piso: number;
  tipoHabitacionId: number;
  tipoHabitacion: string;
  tarifaBasePorNoche: number;
  capacidadMaxima: number;
  estado: EstadoHabitacion;
  estadoDescripcion: string;
  observaciones?: string | null;
  activo: boolean;
}

export interface TipoHabitacion {
  id: number;
  nombre: string;
  descripcion?: string | null;
  tarifaBasePorNoche: number;
  capacidadMaxima: number;
  activo: boolean;
  cantidadHabitaciones: number;
}

export interface HabitacionDisponible {
  id: number;
  numero: string;
  piso: number;
  tipoHabitacion: string;
  capacidadMaxima: number;
  noches: number;
  tarifaPorNoche: number;
  montoTotal: number;
  estrategiaTarifa: string;
  descripcionTarifa: string;
}

export interface ConsultaDisponibilidad {
  fechaEntrada: string;
  fechaSalida: string;
  tipoHabitacionId?: number | null;
  cantidadHuespedes?: number | null;
}

// --- Reservas ----------------------------------------------------------------

export type EstadoReserva = 'Pendiente' | 'Confirmada' | 'Cancelada' | 'Completada';

export type CanalOrigen =
  | 'Telefono'
  | 'CorreoElectronico'
  | 'RedesSociales'
  | 'Presencial'
  | 'PlataformaExterna';

export interface Reserva {
  id: number;
  codigo: string;
  clienteId: number;
  clienteNombre: string;
  clienteIdentificacion: string;
  habitacionId: number;
  habitacionNumero: string;
  tipoHabitacion: string;
  fechaEntrada: string;
  fechaSalida: string;
  noches: number;
  cantidadHuespedes: number;
  estado: EstadoReserva;
  canalOrigen: CanalOrigen;
  montoEstimado: number;
  observaciones?: string | null;
  registradaPor: string;
  fechaCreacion: string;
  tieneEstadia: boolean;
  permiteModificacion: boolean;
  permiteCheckIn: boolean;
}

export interface CrearReserva {
  clienteId?: number | null;
  clienteNuevo?: CrearCliente | null;
  habitacionId: number;
  fechaEntrada: string;
  fechaSalida: string;
  cantidadHuespedes: number;
  canalOrigen: CanalOrigen;
  observaciones?: string | null;
}

// --- Estadías y consumos -----------------------------------------------------

export type EstadoEstadia = 'EnCurso' | 'Finalizada' | 'Facturada';

export interface Estadia {
  id: number;
  reservaId: number;
  reservaCodigo: string;
  clienteId: number;
  clienteNombre: string;
  clienteIdentificacion: string;
  habitacionId: number;
  habitacionNumero: string;
  tipoHabitacion: string;
  fechaCheckIn: string;
  fechaCheckOut?: string | null;
  fechaSalidaPrevista: string;
  noches: number;
  cantidadHuespedes: number;
  estado: EstadoEstadia;
  observaciones?: string | null;
  registradaPor: string;
  totalConsumos: number;
  tieneFactura: boolean;
  consumos: Consumo[];
}

export interface Consumo {
  id: number;
  estadiaId: number;
  servicioAdicionalId: number;
  servicio: string;
  tipoServicio: string;
  descripcion: string;
  cantidad: number;
  precioUnitario: number;
  monto: number;
  fechaConsumo: string;
  registradoPor: string;
}

export interface RegistrarCheckIn {
  reservaId: number;
  fechaCheckIn?: string | null;
  cantidadHuespedes: number;
  observaciones?: string | null;
}

export interface RegistrarConsumo {
  servicioAdicionalId: number;
  descripcion: string;
  cantidad: number;
  precioUnitario?: number | null;
  fechaConsumo?: string | null;
}

export interface LineaCuenta {
  concepto: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
  esHospedaje: boolean;
  fecha?: string | null;
}

export interface CuentaEstadia {
  estadiaId: number;
  descripcion: string;
  noches: number;
  tarifaPorNoche: number;
  subtotalHospedaje: number;
  subtotalConsumos: number;
  total: number;
  estrategiaTarifa: string;
  lineas: LineaCuenta[];
}

export type TipoServicio =
  | 'Restaurante'
  | 'Lavanderia'
  | 'Transporte'
  | 'ActividadRecreativa';

export interface ServicioAdicional {
  id: number;
  nombre: string;
  tipo: TipoServicio;
  descripcion?: string | null;
  precioBase: number;
  activo: boolean;
}

// --- Facturación -------------------------------------------------------------

export type MetodoPago =
  | 'Efectivo'
  | 'TarjetaCredito'
  | 'TarjetaDebito'
  | 'TransferenciaBancaria';

export type EstadoPago = 'Pendiente' | 'Pagado' | 'Anulado';

export interface DetalleFactura {
  id: number;
  concepto: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
  esHospedaje: boolean;
  fechaConsumo?: string | null;
}

export interface Factura {
  id: number;
  numero: string;
  estadiaId: number;
  clienteId: number;
  clienteNombre: string;
  clienteIdentificacion: string;
  habitacionNumero: string;
  tipoHabitacion: string;
  reservaCodigo: string;
  fechaEntrada: string;
  fechaSalida: string;
  fechaEmision: string;
  noches: number;
  tarifaPorNoche: number;
  estrategiaTarifa: string;
  subtotalHospedaje: number;
  subtotalConsumos: number;
  descuento: number;
  justificacionDescuento?: string | null;
  total: number;
  metodoPago?: MetodoPago | null;
  estadoPago: EstadoPago;
  fechaPago?: string | null;
  emitidaPor: string;
  detalles: DetalleFactura[];
}

export interface GenerarFactura {
  estadiaId: number;
  descuento?: number | null;
  justificacionDescuento?: string | null;
  metodoPago?: MetodoPago | null;
  registrarPagoInmediato: boolean;
}

// --- Reportes y panel --------------------------------------------------------

export interface Indicador {
  nombre: string;
  valor: number;
  formato: 'moneda' | 'porcentaje' | 'entero' | 'decimal';
  descripcion?: string | null;
}

export interface PuntoSerie {
  etiqueta: string;
  valor: number;
  valorSecundario: number;
}

export interface Reporte {
  tipo: string;
  titulo: string;
  fechaDesde: string;
  fechaHasta: string;
  fechaGeneracion: string;
  indicadores: Indicador[];
  series: PuntoSerie[];
}

export interface ResumenPanel {
  totalHabitaciones: number;
  habitacionesOcupadas: number;
  habitacionesDisponibles: number;
  habitacionesReservadas: number;
  habitacionesEnLimpieza: number;
  habitacionesEnMantenimiento: number;
  reservasActivas: number;
  llegadasHoy: number;
  checkInsHoy: number;
  checkOutsHoy: number;
  ingresosHoy: number;
  porcentajeOcupacion: number;
}
