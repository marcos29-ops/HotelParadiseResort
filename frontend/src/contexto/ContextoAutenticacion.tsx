import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { almacenSesion, registrarManejadorSesionExpirada } from '../api/clienteHttp';
import { apiAutenticacion } from '../api/servicios';
import type { RolUsuario, SolicitudInicioSesion, UsuarioAutenticado } from '../tipos/api';

interface ValorContexto {
  usuario: UsuarioAutenticado | null;
  estaAutenticado: boolean;
  esAdministrador: boolean;
  iniciarSesion: (solicitud: SolicitudInicioSesion) => Promise<void>;
  cerrarSesion: () => void;
  tieneRol: (...roles: RolUsuario[]) => boolean;
}

const ContextoAutenticacion = createContext<ValorContexto | undefined>(undefined);

/**
 * Sesión del usuario. Se rehidrata desde el almacenamiento local al abrir la
 * aplicación, de modo que recargar la página no obligue a autenticarse otra vez
 * mientras el token siga vigente.
 */
export function ProveedorAutenticacion({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<UsuarioAutenticado | null>(() =>
    almacenSesion.obtenerToken() ? almacenSesion.obtenerUsuario<UsuarioAutenticado>() : null,
  );

  const cerrarSesion = useCallback(() => {
    almacenSesion.limpiar();
    setUsuario(null);
  }, []);

  // El cliente HTTP avisa cuando la API rechaza el token: la sesión se cierra sin
  // que cada pantalla tenga que contemplarlo.
  useEffect(() => {
    registrarManejadorSesionExpirada(() => setUsuario(null));
  }, []);

  const iniciarSesion = useCallback(async (solicitud: SolicitudInicioSesion) => {
    const sesion = await apiAutenticacion.iniciarSesion(solicitud);
    almacenSesion.guardar(sesion.token, sesion.usuario);
    setUsuario(sesion.usuario);
  }, []);

  const valor = useMemo<ValorContexto>(
    () => ({
      usuario,
      estaAutenticado: usuario !== null,
      esAdministrador: usuario?.rol === 'Administrador',
      iniciarSesion,
      cerrarSesion,
      tieneRol: (...roles) => (usuario ? roles.includes(usuario.rol) : false),
    }),
    [usuario, iniciarSesion, cerrarSesion],
  );

  return <ContextoAutenticacion.Provider value={valor}>{children}</ContextoAutenticacion.Provider>;
}

export function useAutenticacion() {
  const contexto = useContext(ContextoAutenticacion);

  if (!contexto) {
    throw new Error('useAutenticacion debe usarse dentro de ProveedorAutenticacion.');
  }

  return contexto;
}
