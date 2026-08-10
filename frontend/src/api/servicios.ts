import { clienteHttp } from './clienteHttp';
import type {
  Cliente,
  ConsultaDisponibilidad,
  Consumo,
  CrearCliente,
  ActualizarCliente,
  CrearReserva,
  CuentaEstadia,
  Estadia,
  Factura,
  GenerarFactura,
  ActualizarHabitacion,
  ActualizarTipoHabitacion,
  CrearHabitacion,
  CrearTipoHabitacion,
  Habitacion,
  HabitacionDisponible,
  HistorialCliente,
  ParametrosPaginacion,
  RegistrarCheckIn,
  RegistrarConsumo,
  Reporte,
  Reserva,
  RespuestaInicioSesion,
  ResultadoPaginado,
  ResumenPanel,
  ServicioAdicional,
  SolicitudInicioSesion,
  TipoHabitacion,
  EstadoHabitacion,
} from '../tipos/api';

/**
 * Acceso a la API agrupado por componente de negocio, en espejo de la fachada del
 * backend. Ninguna pantalla arma URLs por su cuenta.
 */

export const apiAutenticacion = {
  iniciarSesion: (solicitud: SolicitudInicioSesion) =>
    clienteHttp
      .post<RespuestaInicioSesion>('/autenticacion/iniciar-sesion', solicitud)
      .then((r) => r.data),

  cambiarContrasena: (contrasenaActual: string, contrasenaNueva: string) =>
    clienteHttp
      .post('/autenticacion/cambiar-contrasena', { contrasenaActual, contrasenaNueva })
      .then(() => undefined),
};

export const apiClientes = {
  listar: (parametros: ParametrosPaginacion) =>
    clienteHttp
      .get<ResultadoPaginado<Cliente>>('/clientes', { params: parametros })
      .then((r) => r.data),

  obtener: (id: number) => clienteHttp.get<Cliente>(`/clientes/${id}`).then((r) => r.data),

  buscarPorIdentificacion: (identificacion: string) =>
    clienteHttp
      .get<Cliente>(`/clientes/por-identificacion/${encodeURIComponent(identificacion)}`)
      .then((r) => r.data),

  registrar: (datos: CrearCliente) =>
    clienteHttp.post<Cliente>('/clientes', datos).then((r) => r.data),

  actualizar: (id: number, datos: ActualizarCliente) =>
    clienteHttp.put<Cliente>(`/clientes/${id}`, datos).then((r) => r.data),

  historial: (id: number) =>
    clienteHttp.get<HistorialCliente[]>(`/clientes/${id}/historial`).then((r) => r.data),

  reservas: (id: number) =>
    clienteHttp.get<Reserva[]>(`/clientes/${id}/reservas`).then((r) => r.data),
};

export const apiHabitaciones = {
  listar: () => clienteHttp.get<Habitacion[]>('/habitaciones').then((r) => r.data),

  obtener: (id: number) => clienteHttp.get<Habitacion>(`/habitaciones/${id}`).then((r) => r.data),

  disponibilidad: (consulta: ConsultaDisponibilidad) =>
    clienteHttp
      .get<HabitacionDisponible[]>('/habitaciones/disponibilidad', { params: consulta })
      .then((r) => r.data),

  cambiarEstado: (id: number, nuevoEstado: EstadoHabitacion) =>
    clienteHttp
      .patch<Habitacion>(`/habitaciones/${id}/estado`, { nuevoEstado })
      .then((r) => r.data),

  crear: (datos: CrearHabitacion) =>
    clienteHttp.post<Habitacion>('/habitaciones', datos).then((r) => r.data),

  actualizar: (id: number, datos: ActualizarHabitacion) =>
    clienteHttp.put<Habitacion>(`/habitaciones/${id}`, datos).then((r) => r.data),

  tipos: () => clienteHttp.get<TipoHabitacion[]>('/tipos-habitacion').then((r) => r.data),

  crearTipo: (datos: CrearTipoHabitacion) =>
    clienteHttp.post<TipoHabitacion>('/tipos-habitacion', datos).then((r) => r.data),

  actualizarTipo: (id: number, datos: ActualizarTipoHabitacion) =>
    clienteHttp.put<TipoHabitacion>(`/tipos-habitacion/${id}`, datos).then((r) => r.data),
};

export const apiReservas = {
  listar: (
    parametros: ParametrosPaginacion & { estado?: string; desde?: string; hasta?: string },
  ) =>
    clienteHttp
      .get<ResultadoPaginado<Reserva>>('/reservas', { params: parametros })
      .then((r) => r.data),

  obtener: (id: number) => clienteHttp.get<Reserva>(`/reservas/${id}`).then((r) => r.data),

  /** Con `confirmar` se usa la operación compuesta de la fachada: crea y confirma. */
  crear: (datos: CrearReserva, confirmar: boolean) =>
    clienteHttp
      .post<Reserva>('/reservas', datos, { params: { confirmar } })
      .then((r) => r.data),

  confirmar: (id: number) =>
    clienteHttp.post<Reserva>(`/reservas/${id}/confirmar`).then((r) => r.data),

  cancelar: (id: number, motivo: string) =>
    clienteHttp.post<Reserva>(`/reservas/${id}/cancelar`, { motivo }).then((r) => r.data),

  llegadasDelDia: (fecha?: string) =>
    clienteHttp.get<Reserva[]>('/reservas/llegadas', { params: { fecha } }).then((r) => r.data),
};

export const apiEstadias = {
  listar: (parametros: ParametrosPaginacion & { estado?: string }) =>
    clienteHttp
      .get<ResultadoPaginado<Estadia>>('/estadias', { params: parametros })
      .then((r) => r.data),

  obtener: (id: number) => clienteHttp.get<Estadia>(`/estadias/${id}`).then((r) => r.data),

  buscarPorHabitacion: (numero: string) =>
    clienteHttp
      .get<Estadia>(`/estadias/por-habitacion/${encodeURIComponent(numero)}`)
      .then((r) => r.data),

  checkIn: (datos: RegistrarCheckIn) =>
    clienteHttp.post<Estadia>('/estadias/check-in', datos).then((r) => r.data),

  checkOut: (id: number, observaciones?: string | null) =>
    clienteHttp
      .post<Estadia>(`/estadias/${id}/check-out`, { fechaCheckOut: null, observaciones })
      .then((r) => r.data),

  /** Operación compuesta de la fachada: cierra la estadía y emite el comprobante. */
  checkOutYFacturar: (id: number, observaciones: string | null, factura: GenerarFactura) =>
    clienteHttp
      .post<Factura>(`/estadias/${id}/check-out-y-facturar`, {
        checkOut: { fechaCheckOut: null, observaciones },
        factura,
      })
      .then((r) => r.data),

  cuenta: (id: number) =>
    clienteHttp.get<CuentaEstadia>(`/estadias/${id}/cuenta`).then((r) => r.data),

  registrarConsumo: (id: number, datos: RegistrarConsumo) =>
    clienteHttp.post<Consumo>(`/estadias/${id}/consumos`, datos).then((r) => r.data),

  eliminarConsumo: (estadiaId: number, consumoId: number) =>
    clienteHttp.delete(`/estadias/${estadiaId}/consumos/${consumoId}`).then(() => undefined),

  servicios: () =>
    clienteHttp.get<ServicioAdicional[]>('/servicios-adicionales').then((r) => r.data),
};

export const apiFacturacion = {
  listar: (parametros: ParametrosPaginacion & { desde?: string; hasta?: string }) =>
    clienteHttp
      .get<ResultadoPaginado<Factura>>('/facturas', { params: parametros })
      .then((r) => r.data),

  obtener: (id: number) => clienteHttp.get<Factura>(`/facturas/${id}`).then((r) => r.data),

  porEstadia: (estadiaId: number) =>
    clienteHttp.get<Factura>(`/facturas/por-estadia/${estadiaId}`).then((r) => r.data),

  generar: (datos: GenerarFactura) =>
    clienteHttp.post<Factura>('/facturas', datos).then((r) => r.data),

  aplicarDescuento: (id: number, monto: number, justificacion: string) =>
    clienteHttp
      .patch<Factura>(`/facturas/${id}/descuento`, { monto, justificacion })
      .then((r) => r.data),

  registrarPago: (id: number, metodoPago: string) =>
    clienteHttp.post<Factura>(`/facturas/${id}/pago`, { metodoPago }).then((r) => r.data),
};

export const apiReportes = {
  panel: () => clienteHttp.get<ResumenPanel>('/panel/resumen').then((r) => r.data),

  ocupacion: (fechaDesde: string, fechaHasta: string, tipoHabitacionId?: number | null) =>
    clienteHttp
      .get<Reporte>('/reportes/ocupacion', { params: { fechaDesde, fechaHasta, tipoHabitacionId } })
      .then((r) => r.data),

  ingresos: (fechaDesde: string, fechaHasta: string, tipoHabitacionId?: number | null) =>
    clienteHttp
      .get<Reporte>('/reportes/ingresos', { params: { fechaDesde, fechaHasta, tipoHabitacionId } })
      .then((r) => r.data),

  temporadas: (fechaDesde: string, fechaHasta: string, tipoHabitacionId?: number | null) =>
    clienteHttp
      .get<Reporte>('/reportes/temporadas', { params: { fechaDesde, fechaHasta, tipoHabitacionId } })
      .then((r) => r.data),
};
