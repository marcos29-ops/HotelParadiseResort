import { createTheme, alpha } from '@mui/material/styles';
import type { EstadoHabitacion } from '../tipos/api';

/**
 * Identidad visual del sistema.
 *
 * La paleta se apoya en un rojo vino corporativo y una escala de grises neutros.
 * El color se reserva para comunicar significado —el estado de una habitación, el
 * resultado de una acción—, nunca como adorno: es lo que mantiene la interfaz
 * despejada pese a la densidad de información que maneja recepción.
 */

const ROJO_PRINCIPAL = '#8E1F2F';
const ROJO_CLARO = '#B3343F';
const NEUTRO_FONDO = '#F5F7F9';
const NEUTRO_SUPERFICIE = '#FFFFFF';
const TEXTO_PRINCIPAL = '#16202A';
const TEXTO_SECUNDARIO = '#5A6774';

/** Fondo del menú lateral: vino profundo que enmarca el área de trabajo clara. */
export const VINO_PROFUNDO = '#3E1016';

/** Azul reservado a los avisos informativos: el rojo ya identifica a la marca. */
const AZUL_INFO = '#2C6E91';

/** Duraciones compartidas: mantienen coherente el ritmo de toda la interfaz. */
export const DURACION = {
  rapida: 160,
  normal: 240,
  pausada: 380,
  lenta: 560,
} as const;

/** Curva de salida suave: arranca rápido y desacelera, sin rebotes. */
export const CURVA = 'cubic-bezier(0.4, 0, 0.2, 1)';

/** Curva con un ligero adelanto: da carácter a las entradas sin resultar saltona. */
export const CURVA_ENTRADA = 'cubic-bezier(0.16, 1, 0.3, 1)';

/**
 * Sombras estratificadas. Combinan una capa de contacto muy tenue con otra de
 * difusión amplia: es lo que separa una superficie que parece flotar de una con un
 * borde gris pegado debajo.
 */
export const SOMBRA = {
  sutil: '0 1px 2px rgba(22,32,42,0.05), 0 1px 3px rgba(22,32,42,0.04)',
  media: '0 2px 4px rgba(22,32,42,0.04), 0 8px 20px rgba(22,32,42,0.07)',
  elevada: '0 4px 8px rgba(22,32,42,0.05), 0 16px 32px rgba(22,32,42,0.10)',
  flotante: '0 8px 16px rgba(22,32,42,0.07), 0 24px 48px rgba(22,32,42,0.14)',
} as const;

/**
 * Color asignado a cada estado de habitación. Es la leyenda del panel principal y
 * se usa igual en el grid, en los chips y en las tablas para que el significado no
 * cambie según la pantalla.
 */
export const COLOR_ESTADO_HABITACION: Record<
  EstadoHabitacion,
  { principal: string; fondo: string; borde: string; etiqueta: string }
> = {
  Disponible: {
    principal: '#2E7D57',
    fondo: '#E8F5EE',
    borde: '#A8D5BE',
    etiqueta: 'Disponible',
  },
  Reservada: {
    principal: '#B4820A',
    fondo: '#FDF6E3',
    borde: '#E8CE85',
    etiqueta: 'Reservada',
  },
  Ocupada: {
    principal: '#C0392B',
    fondo: '#FCEDEB',
    borde: '#EFB0A8',
    etiqueta: 'Ocupada',
  },
  EnLimpieza: {
    principal: '#2C6E91',
    fondo: '#E9F2F7',
    borde: '#A6C9DB',
    etiqueta: 'En limpieza',
  },
  EnMantenimiento: {
    principal: '#5A6774',
    fondo: '#EEF0F2',
    borde: '#C3CAD1',
    etiqueta: 'En mantenimiento',
  },
};

export const tema = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: ROJO_PRINCIPAL,
      light: ROJO_CLARO,
      dark: '#6B1624',
      contrastText: '#FFFFFF',
    },
    secondary: {
      main: '#5A6774',
      contrastText: '#FFFFFF',
    },
    success: { main: '#2E7D57' },
    warning: { main: '#B4820A' },
    error: { main: '#C0392B' },
    info: { main: AZUL_INFO },
    background: {
      default: NEUTRO_FONDO,
      paper: NEUTRO_SUPERFICIE,
    },
    text: {
      primary: TEXTO_PRINCIPAL,
      secondary: TEXTO_SECUNDARIO,
    },
    divider: '#E2E6EA',
  },

  shape: { borderRadius: 10 },

  typography: {
    fontFamily: [
      '"Inter"',
      '-apple-system',
      'BlinkMacSystemFont',
      '"Segoe UI"',
      'Roboto',
      '"Helvetica Neue"',
      'Arial',
      'sans-serif',
    ].join(','),
    h1: { fontSize: '1.9rem', fontWeight: 600, letterSpacing: '-0.02em' },
    h2: { fontSize: '1.55rem', fontWeight: 600, letterSpacing: '-0.015em' },
    h3: { fontSize: '1.3rem', fontWeight: 600 },
    h4: { fontSize: '1.15rem', fontWeight: 600 },
    h5: { fontSize: '1rem', fontWeight: 600 },
    h6: { fontSize: '0.9rem', fontWeight: 600 },
    subtitle1: { fontSize: '0.95rem', fontWeight: 500 },
    subtitle2: { fontSize: '0.85rem', fontWeight: 500, color: TEXTO_SECUNDARIO },
    body2: { fontSize: '0.875rem' },
    button: { textTransform: 'none', fontWeight: 600, letterSpacing: 0 },
    caption: { fontSize: '0.75rem', color: TEXTO_SECUNDARIO },
  },

  transitions: {
    duration: {
      shortest: DURACION.rapida,
      shorter: DURACION.rapida,
      short: DURACION.normal,
      standard: DURACION.normal,
      complex: DURACION.pausada,
    },
  },

  components: {
    MuiCssBaseline: {
      styleOverrides: {
        // Desplazamiento suave al navegar por anclas y foco visible para teclado.
        html: { scrollBehavior: 'smooth' },
        '*:focus-visible': {
          outline: `2px solid ${ROJO_CLARO}`,
          outlineOffset: 2,
        },
        // Barra de desplazamiento discreta, coherente con la paleta.
        '::-webkit-scrollbar': { width: 10, height: 10 },
        '::-webkit-scrollbar-track': { background: 'transparent' },
        '::-webkit-scrollbar-thumb': {
          background: '#C7CFD6',
          borderRadius: 8,
          border: '2px solid transparent',
          backgroundClip: 'content-box',
        },
        '::-webkit-scrollbar-thumb:hover': { background: '#AAB4BD', backgroundClip: 'content-box' },
      },
    },

    MuiPaper: {
      styleOverrides: {
        root: { backgroundImage: 'none' },
        elevation1: { boxShadow: SOMBRA.sutil },
      },
    },

    MuiCard: {
      defaultProps: { elevation: 0 },
      styleOverrides: {
        root: {
          border: '1px solid #E4E8EC',
          boxShadow: SOMBRA.sutil,
          transition: `box-shadow ${DURACION.normal}ms ${CURVA}, transform ${DURACION.normal}ms ${CURVA}, border-color ${DURACION.normal}ms ${CURVA}`,
        },
      },
    },

    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: {
          borderRadius: 9,
          paddingInline: 18,
          position: 'relative',
          overflow: 'hidden',
          transition: `background-color ${DURACION.rapida}ms ${CURVA}, box-shadow ${DURACION.normal}ms ${CURVA}, transform ${DURACION.rapida}ms ${CURVA}`,
          '&:active': { transform: 'scale(0.97)' },
        },
        containedPrimary: {
          background: `linear-gradient(180deg, ${ROJO_CLARO} 0%, ${ROJO_PRINCIPAL} 100%)`,
          boxShadow: `0 1px 2px ${alpha(ROJO_PRINCIPAL, 0.28)}`,
          '&:hover': {
            background: `linear-gradient(180deg, ${ROJO_CLARO} 0%, ${ROJO_PRINCIPAL} 90%)`,
            boxShadow: `0 4px 14px ${alpha(ROJO_PRINCIPAL, 0.34)}`,
            transform: 'translateY(-1px)',
          },
          // Sin esto el degradado persiste al deshabilitar el botón y el texto
          // atenuado queda ilegible sobre el fondo oscuro.
          '&.Mui-disabled': {
            background: '#E3E7EB',
            color: '#9AA5AF',
            boxShadow: 'none',
          },
        },
        outlined: {
          borderColor: '#D6DCE2',
          '&:hover': { borderColor: ROJO_CLARO, backgroundColor: alpha(ROJO_CLARO, 0.05) },
        },
      },
    },

    MuiIconButton: {
      styleOverrides: {
        root: {
          transition: `background-color ${DURACION.rapida}ms ${CURVA}, transform ${DURACION.rapida}ms ${CURVA}`,
          '&:hover': { transform: 'translateY(-1px) scale(1.06)' },
          '&:active': { transform: 'scale(0.94)' },
        },
      },
    },

    MuiChip: {
      styleOverrides: {
        root: { fontWeight: 600, fontSize: '0.75rem' },
        sizeSmall: { height: 22 },
      },
    },

    MuiTextField: { defaultProps: { size: 'small' } },
    MuiSelect: { defaultProps: { size: 'small' } },
    MuiFormControl: { defaultProps: { size: 'small' } },

    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          transition: `box-shadow ${DURACION.rapida}ms ${CURVA}`,
          '&.Mui-focused': { boxShadow: `0 0 0 3px ${alpha(ROJO_CLARO, 0.14)}` },
        },
      },
    },

    MuiTableCell: {
      styleOverrides: {
        head: {
          fontWeight: 600,
          fontSize: '0.78rem',
          textTransform: 'uppercase',
          letterSpacing: '0.04em',
          color: TEXTO_SECUNDARIO,
          backgroundColor: '#F8F9FB',
          borderBottom: '1px solid #E2E6EA',
          whiteSpace: 'nowrap',
        },
        body: { fontSize: '0.875rem' },
      },
    },

    MuiTableRow: {
      styleOverrides: {
        root: {
          transition: `background-color ${DURACION.rapida}ms ${CURVA}`,
          '&:last-child td': { borderBottom: 0 },
          // Marca lateral al pasar el cursor: guía la lectura de la fila sin
          // recurrir a un fondo de color saturado.
          '&.MuiTableRow-hover:hover': {
            backgroundColor: alpha(ROJO_CLARO, 0.035),
            boxShadow: `inset 3px 0 0 ${alpha(ROJO_CLARO, 0.55)}`,
          },
        },
      },
    },

    MuiTab: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          fontWeight: 600,
          minHeight: 48,
          transition: `color ${DURACION.rapida}ms ${CURVA}, background-color ${DURACION.rapida}ms ${CURVA}`,
          '&:hover': { backgroundColor: alpha(ROJO_CLARO, 0.04) },
        },
      },
    },

    MuiTabs: {
      styleOverrides: {
        indicator: { height: 3, borderRadius: '3px 3px 0 0' },
      },
    },

    MuiTooltip: {
      defaultProps: { arrow: true, enterDelay: 400 },
      styleOverrides: {
        tooltip: {
          fontSize: '0.75rem',
          backgroundColor: '#24303B',
          padding: '7px 11px',
          borderRadius: 7,
          boxShadow: SOMBRA.media,
        },
        arrow: { color: '#24303B' },
      },
    },

    MuiDialog: {
      styleOverrides: {
        paper: { borderRadius: 16, boxShadow: SOMBRA.flotante },
      },
    },

    MuiMenu: {
      styleOverrides: {
        paper: { borderRadius: 12, boxShadow: SOMBRA.elevada, border: '1px solid #E4E8EC' },
      },
    },

    MuiAlert: {
      styleOverrides: {
        root: { borderRadius: 10, alignItems: 'center' },
        standardSuccess: { backgroundColor: '#E8F5EE', color: '#1E5C3E' },
        standardError: { backgroundColor: '#FCEDEB', color: '#8E2A1F' },
        standardWarning: { backgroundColor: '#FDF6E3', color: '#7D5A07' },
        standardInfo: { backgroundColor: '#E9F2F7', color: '#1B4965' },
      },
    },

    MuiSkeleton: {
      styleOverrides: { root: { backgroundColor: '#E8EBEE' } },
    },

    MuiLinearProgress: {
      styleOverrides: { root: { height: 3, borderRadius: 0 } },
    },
  },
});
