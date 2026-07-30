import axios, { AxiosError } from 'axios';
import type { ProblemDetails } from '../tipos/api';

/**
 * Cliente HTTP único del sistema.
 *
 * Concentra tres responsabilidades que, de repartirse por las pantallas, acabarían
 * duplicadas: adjuntar el token, traducir los errores de la API a un mensaje legible
 * y cerrar la sesión cuando el token caduca.
 */

const CLAVE_TOKEN = 'paradise.token';
const CLAVE_USUARIO = 'paradise.usuario';

export const almacenSesion = {
  obtenerToken: () => localStorage.getItem(CLAVE_TOKEN),

  guardar(token: string, usuario: unknown) {
    localStorage.setItem(CLAVE_TOKEN, token);
    localStorage.setItem(CLAVE_USUARIO, JSON.stringify(usuario));
  },

  obtenerUsuario<T>(): T | null {
    const crudo = localStorage.getItem(CLAVE_USUARIO);
    if (!crudo) return null;

    try {
      return JSON.parse(crudo) as T;
    } catch {
      // Un valor corrupto se descarta en lugar de romper el arranque de la app.
      localStorage.removeItem(CLAVE_USUARIO);
      return null;
    }
  },

  limpiar() {
    localStorage.removeItem(CLAVE_TOKEN);
    localStorage.removeItem(CLAVE_USUARIO);
  },
};

export const clienteHttp = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5170/api/v1',
  headers: { 'Content-Type': 'application/json' },
  timeout: 20000,
});

clienteHttp.interceptors.request.use((configuracion) => {
  const token = almacenSesion.obtenerToken();

  if (token) {
    configuracion.headers.Authorization = `Bearer ${token}`;
  }

  return configuracion;
});

/** Se invoca cuando la sesión deja de ser válida; lo define el proveedor de autenticación. */
let alExpirarSesion: (() => void) | null = null;

export function registrarManejadorSesionExpirada(manejador: () => void) {
  alExpirarSesion = manejador;
}

clienteHttp.interceptors.response.use(
  (respuesta) => respuesta,
  (error: AxiosError<ProblemDetails>) => {
    if (error.response?.status === 401) {
      almacenSesion.limpiar();
      alExpirarSesion?.();
    }

    return Promise.reject(error);
  },
);

/**
 * Traduce cualquier fallo a un mensaje comprensible para el personal del hotel.
 * La interfaz nunca muestra códigos de estado ni trazas técnicas.
 */
export function describirError(error: unknown): string {
  if (!axios.isAxiosError(error)) {
    return 'Ocurrió un error inesperado. Intente nuevamente.';
  }

  const problema = error.response?.data as ProblemDetails | undefined;

  // Errores de validación de FluentValidation: se muestra el primero, que es el
  // que el usuario debe corregir antes de reintentar.
  if (problema?.errors) {
    const primerMensaje = Object.values(problema.errors).flat()[0];
    if (primerMensaje) return primerMensaje;
  }

  if (problema?.detail) return problema.detail;

  if (error.code === 'ECONNABORTED') {
    return 'La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.';
  }

  if (!error.response) {
    return 'No se pudo contactar con el servidor. Verifique que el sistema esté disponible.';
  }

  switch (error.response.status) {
    case 401:
      return 'Su sesión expiró. Vuelva a iniciar sesión.';
    case 403:
      return 'No cuenta con permisos para realizar esta operación.';
    case 404:
      return 'No se encontró la información solicitada.';
    default:
      return 'Ocurrió un error inesperado. Intente nuevamente.';
  }
}
