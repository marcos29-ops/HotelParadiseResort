import type { ReactNode } from 'react';
import {
  Box,
  Button,
  Chip,
  Fade,
  Paper,
  Skeleton,
  Stack,
  TableCell,
  TableRow,
  Typography,
  alpha,
} from '@mui/material';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';
import RefreshIcon from '@mui/icons-material/Refresh';
import { COLOR_ESTADO_HABITACION, CURVA, DURACION } from '../tema/tema';
import type { EstadoEstadia, EstadoHabitacion, EstadoPago, EstadoReserva } from '../tipos/api';

/**
 * Estado vacío. Explica por qué no hay datos y, cuando corresponde, ofrece la
 * acción que lo resuelve: una pantalla en blanco deja al usuario sin saber qué hacer.
 */
export function EstadoVacio({
  icono,
  titulo,
  descripcion,
  accion,
}: {
  icono: ReactNode;
  titulo: string;
  descripcion: string;
  accion?: ReactNode;
}) {
  return (
    <Fade in timeout={DURACION.pausada}>
      <Stack alignItems="center" spacing={1.5} sx={{ py: 7, px: 3, textAlign: 'center' }}>
        <Box
          sx={{
            width: 64,
            height: 64,
            borderRadius: '50%',
            display: 'grid',
            placeItems: 'center',
            backgroundColor: 'action.hover',
            color: 'text.disabled',
            '& svg': { fontSize: 30 },
          }}
        >
          {icono}
        </Box>

        <Typography variant="h4">{titulo}</Typography>

        <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 420 }}>
          {descripcion}
        </Typography>

        {accion && <Box pt={1}>{accion}</Box>}
      </Stack>
    </Fade>
  );
}

/** Error de carga con la posibilidad de reintentar sin recargar la página. */
export function EstadoError({
  mensaje,
  onReintentar,
}: {
  mensaje: string;
  onReintentar?: () => void;
}) {
  return (
    <Fade in timeout={DURACION.normal}>
      <Paper
        variant="outlined"
        sx={{
          p: 3,
          textAlign: 'center',
          borderColor: (t) => alpha(t.palette.error.main, 0.3),
          backgroundColor: (t) => alpha(t.palette.error.main, 0.03),
        }}
      >
        <Stack alignItems="center" spacing={1.5}>
          <ErrorOutlineIcon color="error" sx={{ fontSize: 34 }} />
          <Typography variant="h5">No se pudo cargar la información</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 460 }}>
            {mensaje}
          </Typography>
          {onReintentar && (
            <Button startIcon={<RefreshIcon />} onClick={onReintentar} sx={{ mt: 0.5 }}>
              Reintentar
            </Button>
          )}
        </Stack>
      </Paper>
    </Fade>
  );
}

/** Filas de esqueleto: preservan la altura de la tabla y evitan saltos al cargar. */
export function FilasEsqueleto({ filas = 5, columnas }: { filas?: number; columnas: number }) {
  return (
    <>
      {Array.from({ length: filas }).map((_, indiceFila) => (
        <TableRow key={indiceFila}>
          {Array.from({ length: columnas }).map((__, indiceColumna) => (
            <TableCell key={indiceColumna}>
              <Skeleton
                height={20}
                width={indiceColumna === 0 ? '55%' : '80%'}
                sx={{ animationDelay: `${indiceFila * 60}ms` }}
              />
            </TableCell>
          ))}
        </TableRow>
      ))}
    </>
  );
}

/** Distintivo de estado de habitación, con el mismo código de color en todo el sistema. */
export function ChipEstadoHabitacion({
  estado,
  size = 'small',
}: {
  estado: EstadoHabitacion;
  size?: 'small' | 'medium';
}) {
  const colores = COLOR_ESTADO_HABITACION[estado];

  return (
    <Chip
      size={size}
      label={colores.etiqueta}
      sx={{
        color: colores.principal,
        backgroundColor: colores.fondo,
        border: `1px solid ${colores.borde}`,
        transition: `all ${DURACION.rapida}ms ${CURVA}`,
      }}
    />
  );
}

const COLOR_ESTADO_RESERVA: Record<EstadoReserva, 'default' | 'warning' | 'success' | 'error'> = {
  Pendiente: 'warning',
  Confirmada: 'success',
  Cancelada: 'error',
  Completada: 'default',
};

export function ChipEstadoReserva({ estado }: { estado: EstadoReserva }) {
  return <Chip size="small" label={estado} color={COLOR_ESTADO_RESERVA[estado]} variant="outlined" />;
}

const ETIQUETA_ESTADIA: Record<EstadoEstadia, { texto: string; color: 'info' | 'warning' | 'success' }> = {
  EnCurso: { texto: 'En curso', color: 'info' },
  Finalizada: { texto: 'Finalizada', color: 'warning' },
  Facturada: { texto: 'Facturada', color: 'success' },
};

export function ChipEstadoEstadia({ estado }: { estado: EstadoEstadia }) {
  const { texto, color } = ETIQUETA_ESTADIA[estado];
  return <Chip size="small" label={texto} color={color} variant="outlined" />;
}

const COLOR_ESTADO_PAGO: Record<EstadoPago, 'warning' | 'success' | 'error'> = {
  Pendiente: 'warning',
  Pagado: 'success',
  Anulado: 'error',
};

export function ChipEstadoPago({ estado }: { estado: EstadoPago }) {
  return <Chip size="small" label={estado} color={COLOR_ESTADO_PAGO[estado]} />;
}
