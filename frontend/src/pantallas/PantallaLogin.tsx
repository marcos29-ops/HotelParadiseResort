import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Navigate, useLocation } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Collapse,
  FormControlLabel,
  IconButton,
  InputAdornment,
  Link,
  Stack,
  TextField,
  Typography,
  alpha,
} from '@mui/material';
import HotelIcon from '@mui/icons-material/Hotel';
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined';
import VisibilityOffOutlinedIcon from '@mui/icons-material/VisibilityOffOutlined';
import LoginIcon from '@mui/icons-material/Login';
import { useAutenticacion } from '../contexto/ContextoAutenticacion';
import { useNotificaciones } from '../contexto/ContextoNotificaciones';
import { describirError } from '../api/clienteHttp';
import { CURVA, DURACION } from '../tema/tema';

interface Formulario {
  nombreUsuario: string;
  contrasena: string;
  recordarme: boolean;
}

const CLAVE_USUARIO_RECORDADO = 'paradise.usuarioRecordado';

/**
 * PANTALLA 1 — Inicio de sesión.
 *
 * Acceso restringido al personal de recepción y administración. Reproduce el
 * wireframe de la Etapa 2, incluidas las opciones "Recordarme" y de recuperación
 * de contraseña.
 */
export function PantallaLogin() {
  const { iniciarSesion, estaAutenticado } = useAutenticacion();
  const notificar = useNotificaciones();
  const ubicacion = useLocation();

  const [verContrasena, setVerContrasena] = useState(false);
  const [errorAcceso, setErrorAcceso] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<Formulario>({
    mode: 'onBlur',
    defaultValues: {
      nombreUsuario: localStorage.getItem(CLAVE_USUARIO_RECORDADO) ?? '',
      contrasena: '',
      recordarme: Boolean(localStorage.getItem(CLAVE_USUARIO_RECORDADO)),
    },
  });

  if (estaAutenticado) {
    const destino = (ubicacion.state as { desde?: string } | null)?.desde ?? '/panel';
    return <Navigate to={destino} replace />;
  }

  const enviar = handleSubmit(async (datos) => {
    setErrorAcceso(null);

    try {
      await iniciarSesion({
        nombreUsuario: datos.nombreUsuario.trim(),
        contrasena: datos.contrasena,
      });

      // "Recordarme" solo conserva el nombre de usuario: la contraseña jamás se guarda.
      if (datos.recordarme) {
        localStorage.setItem(CLAVE_USUARIO_RECORDADO, datos.nombreUsuario.trim());
      } else {
        localStorage.removeItem(CLAVE_USUARIO_RECORDADO);
      }

      notificar.exito('Sesión iniciada correctamente.');
    } catch (error) {
      setErrorAcceso(describirError(error));
    }
  });

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        p: 2,
        position: 'relative',
        overflow: 'hidden',
        background: (t) =>
          `linear-gradient(150deg, ${t.palette.primary.dark} 0%, ${t.palette.primary.main} 45%, #24607F 100%)`,
      }}
    >
      {/* Formas difusas de fondo: aportan profundidad sin competir con el formulario. */}
      <Box
        aria-hidden
        sx={{
          position: 'absolute',
          width: 620,
          height: 620,
          borderRadius: '50%',
          top: -220,
          right: -180,
          background: (t) => alpha(t.palette.common.white, 0.05),
          animation: `flotar 14s ease-in-out infinite`,
          '@keyframes flotar': {
            '0%, 100%': { transform: 'translate(0, 0) scale(1)' },
            '50%': { transform: 'translate(-28px, 24px) scale(1.05)' },
          },
        }}
      />
      <Box
        aria-hidden
        sx={{
          position: 'absolute',
          width: 460,
          height: 460,
          borderRadius: '50%',
          bottom: -190,
          left: -140,
          background: (t) => alpha(t.palette.common.white, 0.04),
          animation: `flotarLento 18s ease-in-out infinite`,
          '@keyframes flotarLento': {
            '0%, 100%': { transform: 'translate(0, 0)' },
            '50%': { transform: 'translate(26px, -20px)' },
          },
        }}
      />

      <Card
        sx={{
          width: '100%',
          maxWidth: 428,
          position: 'relative',
          border: 'none',
          boxShadow: '0 24px 64px rgba(9, 24, 35, 0.32)',
          animation: `entrar ${DURACION.pausada}ms ${CURVA} both`,
          '@keyframes entrar': {
            from: { opacity: 0, transform: 'translateY(16px) scale(0.985)' },
            to: { opacity: 1, transform: 'translateY(0) scale(1)' },
          },
        }}
      >
        <CardContent sx={{ p: { xs: 3, sm: 4.5 } }}>
          <Stack alignItems="center" spacing={1.25} mb={3.5}>
            <Box
              sx={{
                width: 56,
                height: 56,
                borderRadius: 2.5,
                display: 'grid',
                placeItems: 'center',
                backgroundColor: 'primary.main',
                color: 'primary.contrastText',
                boxShadow: (t) => `0 8px 20px ${alpha(t.palette.primary.main, 0.32)}`,
              }}
            >
              <HotelIcon sx={{ fontSize: 28 }} />
            </Box>

            <Typography variant="h2" component="h1" sx={{ letterSpacing: '0.06em' }}>
              PARADISE RESORT
            </Typography>

            <Typography variant="body2" color="text.secondary">
              Sistema de Gestión Hotelera
            </Typography>
          </Stack>

          <Collapse in={Boolean(errorAcceso)} timeout={DURACION.normal}>
            <Alert severity="error" sx={{ mb: 2.5 }} onClose={() => setErrorAcceso(null)}>
              {errorAcceso}
            </Alert>
          </Collapse>

          <Box component="form" onSubmit={enviar} noValidate>
            <Stack spacing={2.25}>
              <TextField
                label="Usuario"
                autoComplete="username"
                autoFocus
                fullWidth
                error={Boolean(errors.nombreUsuario)}
                helperText={errors.nombreUsuario?.message}
                {...register('nombreUsuario', {
                  required: 'Ingrese su nombre de usuario.',
                })}
              />

              <TextField
                label="Contraseña"
                type={verContrasena ? 'text' : 'password'}
                autoComplete="current-password"
                fullWidth
                error={Boolean(errors.contrasena)}
                helperText={errors.contrasena?.message}
                InputProps={{
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton
                        onClick={() => setVerContrasena((v) => !v)}
                        edge="end"
                        size="small"
                        aria-label={verContrasena ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                      >
                        {verContrasena ? (
                          <VisibilityOffOutlinedIcon fontSize="small" />
                        ) : (
                          <VisibilityOutlinedIcon fontSize="small" />
                        )}
                      </IconButton>
                    </InputAdornment>
                  ),
                }}
                {...register('contrasena', { required: 'Ingrese su contraseña.' })}
              />

              <Stack direction="row" alignItems="center" justifyContent="space-between">
                <FormControlLabel
                  control={<Checkbox size="small" {...register('recordarme')} />}
                  label={<Typography variant="body2">Recordarme</Typography>}
                />

                <Link
                  component="button"
                  type="button"
                  variant="body2"
                  underline="hover"
                  color="text.secondary"
                  onClick={() =>
                    notificar.informacion(
                      'Solicite el restablecimiento de su contraseña al administrador del sistema.',
                    )
                  }
                >
                  ¿Olvidó su contraseña?
                </Link>
              </Stack>

              <Button
                type="submit"
                variant="contained"
                size="large"
                fullWidth
                disabled={isSubmitting}
                startIcon={!isSubmitting && <LoginIcon />}
                sx={{ py: 1.35, mt: 0.5 }}
              >
                {isSubmitting ? 'Verificando…' : 'Iniciar sesión'}
              </Button>
            </Stack>
          </Box>

          <Typography
            variant="caption"
            display="block"
            textAlign="center"
            sx={{ mt: 3.5, lineHeight: 1.6 }}
          >
            Acceso restringido a personal autorizado
            <br />
            Recepción · Administración
          </Typography>
        </CardContent>
      </Card>
    </Box>
  );
}
