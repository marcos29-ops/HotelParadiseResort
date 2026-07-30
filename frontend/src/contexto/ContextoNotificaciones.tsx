import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { Alert, Slide, Snackbar } from '@mui/material';
import type { SlideProps } from '@mui/material';

type Severidad = 'success' | 'error' | 'warning' | 'info';

interface Aviso {
  id: number;
  mensaje: string;
  severidad: Severidad;
}

interface ValorContexto {
  exito: (mensaje: string) => void;
  error: (mensaje: string) => void;
  advertencia: (mensaje: string) => void;
  informacion: (mensaje: string) => void;
}

const ContextoNotificaciones = createContext<ValorContexto | undefined>(undefined);

const Transicion = (props: SlideProps) => <Slide {...props} direction="up" />;

/**
 * Retroalimentación inmediata de las acciones del usuario mediante snackbars.
 *
 * La Etapa 2 exige confirmación visible tras registrar un check-in, aplicar un
 * descuento o emitir una factura. Nunca se usan diálogos nativos del navegador.
 */
export function ProveedorNotificaciones({ children }: { children: ReactNode }) {
  const [aviso, setAviso] = useState<Aviso | null>(null);
  const [visible, setVisible] = useState(false);

  const mostrar = useCallback((mensaje: string, severidad: Severidad) => {
    setAviso({ id: Date.now(), mensaje, severidad });
    setVisible(true);
  }, []);

  const valor = useMemo<ValorContexto>(
    () => ({
      exito: (m) => mostrar(m, 'success'),
      error: (m) => mostrar(m, 'error'),
      advertencia: (m) => mostrar(m, 'warning'),
      informacion: (m) => mostrar(m, 'info'),
    }),
    [mostrar],
  );

  return (
    <ContextoNotificaciones.Provider value={valor}>
      {children}

      <Snackbar
        key={aviso?.id}
        open={visible}
        // Los errores permanecen más tiempo: el usuario necesita leerlos y actuar.
        autoHideDuration={aviso?.severidad === 'error' ? 7000 : 4000}
        onClose={(_, motivo) => motivo !== 'clickaway' && setVisible(false)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
        TransitionComponent={Transicion}
      >
        <Alert
          onClose={() => setVisible(false)}
          severity={aviso?.severidad ?? 'info'}
          variant="standard"
          sx={{
            minWidth: 320,
            maxWidth: 520,
            boxShadow: '0 8px 24px rgba(26,32,39,0.16)',
            border: '1px solid',
            borderColor: 'divider',
          }}
        >
          {aviso?.mensaje}
        </Alert>
      </Snackbar>
    </ContextoNotificaciones.Provider>
  );
}

export function useNotificaciones() {
  const contexto = useContext(ContextoNotificaciones);

  if (!contexto) {
    throw new Error('useNotificaciones debe usarse dentro de ProveedorNotificaciones.');
  }

  return contexto;
}
