import type { ReactNode } from 'react';
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Stack,
  alpha,
} from '@mui/material';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';

/**
 * Confirmación de operaciones críticas e irreversibles: check-out, emisión de
 * facturas, cancelaciones y eliminaciones.
 *
 * La Etapa 2 lo pide como medida de prevención de errores; el resumen de lo que va a
 * ocurrir se muestra antes de confirmar, para que el recepcionista no dependa de
 * recordar el estado de la pantalla anterior.
 */
export function DialogoConfirmacion({
  abierto,
  titulo,
  mensaje,
  detalle,
  etiquetaConfirmar,
  color = 'primary',
  enProceso = false,
  onConfirmar,
  onCancelar,
}: {
  abierto: boolean;
  titulo: string;
  mensaje: string;
  /** Resumen opcional de la operación (importes, habitación, huésped). */
  detalle?: ReactNode;
  etiquetaConfirmar: string;
  color?: 'primary' | 'error' | 'success';
  enProceso?: boolean;
  onConfirmar: () => void;
  onCancelar: () => void;
}) {
  return (
    <Dialog
      open={abierto}
      onClose={() => !enProceso && onCancelar()}
      maxWidth="xs"
      fullWidth
    >
      <DialogTitle sx={{ pb: 1.5 }}>
        <Stack direction="row" spacing={1.5} alignItems="center">
          <Box
            sx={{
              width: 38,
              height: 38,
              flexShrink: 0,
              borderRadius: '50%',
              display: 'grid',
              placeItems: 'center',
              backgroundColor: (t) => alpha(t.palette[color].main, 0.11),
              color: `${color}.main`,
            }}
          >
            <WarningAmberOutlinedIcon fontSize="small" />
          </Box>
          {titulo}
        </Stack>
      </DialogTitle>

      <DialogContent>
        <DialogContentText>{mensaje}</DialogContentText>

        {detalle && (
          <Box
            sx={{
              mt: 2,
              p: 2,
              borderRadius: 2,
              backgroundColor: 'action.hover',
              border: '1px solid',
              borderColor: 'divider',
            }}
          >
            {detalle}
          </Box>
        )}
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2.5 }}>
        <Button color="inherit" onClick={onCancelar} disabled={enProceso}>
          Volver
        </Button>
        <Button
          variant="contained"
          color={color}
          onClick={onConfirmar}
          disabled={enProceso}
          startIcon={enProceso ? <CircularProgress size={16} color="inherit" /> : undefined}
        >
          {enProceso ? 'Procesando…' : etiquetaConfirmar}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
