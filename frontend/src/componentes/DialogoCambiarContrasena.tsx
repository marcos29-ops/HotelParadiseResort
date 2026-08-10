import { useMutation } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
} from '@mui/material';
import { apiAutenticacion } from '../api/servicios';
import { describirError } from '../api/clienteHttp';
import { useNotificaciones } from '../contexto/ContextoNotificaciones';

interface Formulario {
  contrasenaActual: string;
  contrasenaNueva: string;
  confirmacion: string;
}

const LONGITUD_MINIMA = 8;

/**
 * Cambio de la contraseña propia, disponible para cualquier usuario autenticado
 * desde el menú de su cuenta. El endpoint existía desde la Fase 2 pero ninguna
 * pantalla lo invocaba.
 */
export function DialogoCambiarContrasena({
  abierto,
  onCerrar,
}: {
  abierto: boolean;
  onCerrar: () => void;
}) {
  const notificar = useNotificaciones();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<Formulario>({
    defaultValues: { contrasenaActual: '', contrasenaNueva: '', confirmacion: '' },
  });

  const mutacion = useMutation({
    mutationFn: (datos: Formulario) =>
      apiAutenticacion.cambiarContrasena(datos.contrasenaActual, datos.contrasenaNueva),
    onSuccess: () => {
      reset();
      onCerrar();
      notificar.exito('Su contraseña se actualizó correctamente.');
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const cerrar = () => {
    reset();
    onCerrar();
  };

  return (
    <Dialog open={abierto} onClose={cerrar} maxWidth="xs" fullWidth>
      <DialogTitle>Cambiar contraseña</DialogTitle>

      <Box component="form" onSubmit={handleSubmit((d) => mutacion.mutate(d))} noValidate>
        <DialogContent dividers>
          <Stack spacing={2.25}>
            <TextField
              label="Contraseña actual"
              type="password"
              fullWidth
              autoFocus
              error={Boolean(errors.contrasenaActual)}
              helperText={errors.contrasenaActual?.message}
              {...register('contrasenaActual', { required: 'Indique su contraseña actual.' })}
            />

            <TextField
              label="Contraseña nueva"
              type="password"
              fullWidth
              error={Boolean(errors.contrasenaNueva)}
              helperText={errors.contrasenaNueva?.message ?? `Mínimo ${LONGITUD_MINIMA} caracteres.`}
              {...register('contrasenaNueva', {
                required: 'La contraseña nueva es obligatoria.',
                minLength: {
                  value: LONGITUD_MINIMA,
                  message: `Debe tener al menos ${LONGITUD_MINIMA} caracteres.`,
                },
              })}
            />

            <TextField
              label="Confirmar contraseña"
              type="password"
              fullWidth
              error={Boolean(errors.confirmacion)}
              helperText={errors.confirmacion?.message}
              {...register('confirmacion', {
                validate: (valor, campos) =>
                  valor === campos.contrasenaNueva || 'Las contraseñas no coinciden.',
              })}
            />
          </Stack>
        </DialogContent>

        <DialogActions sx={{ px: 3, py: 2 }}>
          <Button color="inherit" onClick={cerrar}>
            Cancelar
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={mutacion.isPending}
            startIcon={mutacion.isPending && <CircularProgress size={16} color="inherit" />}
          >
            Cambiar
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}
