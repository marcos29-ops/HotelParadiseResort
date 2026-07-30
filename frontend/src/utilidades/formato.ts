/** Formato de fechas y horas en español, uniforme en toda la interfaz. */

const FORMATO_FECHA = new Intl.DateTimeFormat('es-CR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
});

const FORMATO_FECHA_HORA = new Intl.DateTimeFormat('es-CR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

export function formatearFecha(valor?: string | null): string {
  if (!valor) return '—';

  const fecha = new Date(valor);
  return Number.isNaN(fecha.getTime()) ? '—' : FORMATO_FECHA.format(fecha);
}

export function formatearFechaHora(valor?: string | null): string {
  if (!valor) return '—';

  const fecha = new Date(valor);
  return Number.isNaN(fecha.getTime()) ? '—' : FORMATO_FECHA_HORA.format(fecha);
}

/**
 * Etiquetas legibles de los valores que la API expone en notación de código.
 * Evita que la interfaz muestre «RedesSociales» o «TarjetaCredito» al usuario.
 */
const ETIQUETAS: Record<string, string> = {
  Telefono: 'Teléfono',
  CorreoElectronico: 'Correo electrónico',
  RedesSociales: 'Redes sociales',
  Presencial: 'Presencial',
  PlataformaExterna: 'Plataforma externa',
  Efectivo: 'Efectivo',
  TarjetaCredito: 'Tarjeta de crédito',
  TarjetaDebito: 'Tarjeta de débito',
  TransferenciaBancaria: 'Transferencia bancaria',
  Lavanderia: 'Lavandería',
  ActividadRecreativa: 'Actividad recreativa',
  EnCurso: 'En curso',
  EnLimpieza: 'En limpieza',
  EnMantenimiento: 'En mantenimiento',
};

export function etiquetaLegible(valor?: string | null): string {
  if (!valor) return '—';
  return ETIQUETAS[valor] ?? valor;
}

/** Fecha de hoy en formato ISO corto, apta para campos <input type="date">. */
export function fechaHoyIso(): string {
  return new Date().toISOString().slice(0, 10);
}

/** Fecha desplazada en días respecto de hoy, en formato ISO corto. */
export function fechaDesplazadaIso(dias: number): string {
  const fecha = new Date();
  fecha.setDate(fecha.getDate() + dias);
  return fecha.toISOString().slice(0, 10);
}
