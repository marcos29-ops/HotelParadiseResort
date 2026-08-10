import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Controller, useForm } from 'react-hook-form';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  LinearProgress,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import PersonAddAltIcon from '@mui/icons-material/PersonAddAlt';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import KeyOutlinedIcon from '@mui/icons-material/KeyOutlined';
import BlockOutlinedIcon from '@mui/icons-material/BlockOutlined';
import ManageAccountsOutlinedIcon from '@mui/icons-material/ManageAccountsOutlined';
import { apiUsuarios } from '../api/servicios';
import { describirError } from '../api/clienteHttp';
import { useAutenticacion } from '../contexto/ContextoAutenticacion';
import { useNotificaciones } from '../contexto/ContextoNotificaciones';
import { EstadoError, EstadoVacio, FilasEsqueleto } from '../componentes/Estados';
import { DialogoConfirmacion } from '../componentes/DialogoConfirmacion';
import { formatearFechaHora } from '../utilidades/formato';
import type { RolUsuario, Usuario } from '../tipos/api';

interface FormularioUsuario {
  nombre: string;
  nombreUsuario: string;
  correo: string;
  contrasena: string;
  rol: RolUsuario;
  activo: string;
}

interface FormularioContrasena {
  contrasenaNueva: string;
  confirmacion: string;
}

const COLUMNAS = 6;
const LONGITUD_MINIMA_CONTRASENA = 8;

/**
 * Gestión de usuarios del sistema (regla de negocio 8).
 *
 * Sin esta pantalla las cuentas del personal solo podían crearse desde Swagger, y
 * el aviso del inicio de sesión —«solicite el restablecimiento al administrador»—
 * remitía a una operación que la interfaz no ofrecía.
 *
 * El backend protege el módulo entero con [Authorize(Administrador)]; la ruta
 * repite la restricción para no mostrar una pantalla que devolvería 403.
 */
export function PantallaUsuarios() {
  const notificar = useNotificaciones();
  const clienteConsultas = useQueryClient();
  const { usuario: sesion } = useAutenticacion();

  const [pagina, setPagina] = useState(0);
  const [tamanoPagina, setTamanoPagina] = useState(10);

  const [dialogoAbierto, setDialogoAbierto] = useState(false);
  const [usuarioEditando, setUsuarioEditando] = useState<Usuario | null>(null);
  const [usuarioContrasena, setUsuarioContrasena] = useState<Usuario | null>(null);
  const [usuarioDesactivar, setUsuarioDesactivar] = useState<Usuario | null>(null);

  const consulta = useQuery({
    queryKey: ['usuarios', pagina, tamanoPagina],
    queryFn: () => apiUsuarios.listar({ pagina: pagina + 1, tamanoPagina }),
  });

  const formUsuario = useForm<FormularioUsuario>();
  const formContrasena = useForm<FormularioContrasena>();

  const abrirDialogo = (usuario: Usuario | null) => {
    setUsuarioEditando(usuario);
    formUsuario.reset({
      nombre: usuario?.nombre ?? '',
      nombreUsuario: usuario?.nombreUsuario ?? '',
      correo: usuario?.correo ?? '',
      contrasena: '',
      rol: usuario?.rol ?? 'Recepcionista',
      activo: usuario ? String(usuario.activo) : 'true',
    });
    setDialogoAbierto(true);
  };

  const mutacionGuardar = useMutation({
    mutationFn: (datos: FormularioUsuario) =>
      usuarioEditando
        ? apiUsuarios.actualizar(usuarioEditando.id, {
            nombre: datos.nombre.trim(),
            correo: datos.correo.trim(),
            rol: datos.rol,
            activo: datos.activo === 'true',
          })
        : apiUsuarios.crear({
            nombre: datos.nombre.trim(),
            nombreUsuario: datos.nombreUsuario.trim(),
            correo: datos.correo.trim(),
            contrasena: datos.contrasena,
            rol: datos.rol,
          }),
    onSuccess: (usuario) => {
      clienteConsultas.invalidateQueries({ queryKey: ['usuarios'] });
      setDialogoAbierto(false);
      notificar.exito(
        usuarioEditando
          ? `Usuario ${usuario.nombreUsuario} actualizado.`
          : `Usuario ${usuario.nombreUsuario} creado.`,
      );
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const mutacionContrasena = useMutation({
    mutationFn: (datos: FormularioContrasena) =>
      apiUsuarios.restablecerContrasena(usuarioContrasena!.id, datos.contrasenaNueva),
    onSuccess: () => {
      const nombre = usuarioContrasena?.nombreUsuario;
      setUsuarioContrasena(null);
      notificar.exito(`Contraseña de ${nombre} restablecida.`);
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const mutacionDesactivar = useMutation({
    mutationFn: (usuario: Usuario) => apiUsuarios.desactivar(usuario.id),
    onSuccess: (_, usuario) => {
      clienteConsultas.invalidateQueries({ queryKey: ['usuarios'] });
      setUsuarioDesactivar(null);
      notificar.exito(`Cuenta de ${usuario.nombreUsuario} desactivada.`);
    },
    onError: (error) => {
      setUsuarioDesactivar(null);
      notificar.error(describirError(error));
    },
  });

  const datos = consulta.data;

  return (
    <Stack spacing={2.5}>
      <Card>
        <CardContent>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={2}
            alignItems={{ xs: 'stretch', sm: 'center' }}
          >
            <Box flex={1}>
              <Typography variant="h4">Usuarios del sistema</Typography>
              <Typography variant="caption">
                {datos ? `${datos.totalRegistros} cuenta(s) registrada(s)` : 'Cargando…'}
              </Typography>
            </Box>

            <Button
              variant="contained"
              startIcon={<PersonAddAltIcon />}
              onClick={() => abrirDialogo(null)}
              sx={{ whiteSpace: 'nowrap' }}
            >
              Nuevo usuario
            </Button>
          </Stack>
        </CardContent>

        <Box sx={{ height: 3 }}>
          {consulta.isFetching && !consulta.isPending && <LinearProgress />}
        </Box>

        <Divider />

        {consulta.isError ? (
          <Box p={3}>
            <EstadoError
              mensaje={describirError(consulta.error)}
              onReintentar={() => consulta.refetch()}
            />
          </Box>
        ) : (
          <>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Usuario</TableCell>
                    <TableCell>Nombre</TableCell>
                    <TableCell>Correo</TableCell>
                    <TableCell align="center">Rol</TableCell>
                    <TableCell>Último acceso</TableCell>
                    <TableCell align="right">Acciones</TableCell>
                  </TableRow>
                </TableHead>

                <TableBody>
                  {consulta.isPending ? (
                    <FilasEsqueleto columnas={COLUMNAS} filas={4} />
                  ) : datos && datos.elementos.length > 0 ? (
                    datos.elementos.map((usuario) => {
                      const esUnoMismo = usuario.id === sesion?.id;

                      return (
                        <TableRow key={usuario.id} hover>
                          <TableCell>
                            <Typography variant="body2" fontWeight={600}>
                              {usuario.nombreUsuario}
                            </Typography>
                            {esUnoMismo && (
                              <Typography variant="caption" color="primary">
                                Su cuenta
                              </Typography>
                            )}
                          </TableCell>

                          <TableCell>
                            <Typography variant="body2">{usuario.nombre}</Typography>
                          </TableCell>

                          <TableCell>
                            <Typography variant="body2">{usuario.correo}</Typography>
                          </TableCell>

                          <TableCell align="center">
                            <Chip
                              size="small"
                              label={usuario.rol}
                              color={usuario.rol === 'Administrador' ? 'primary' : 'default'}
                              variant={usuario.rol === 'Administrador' ? 'filled' : 'outlined'}
                            />
                          </TableCell>

                          <TableCell>
                            <Typography variant="body2">
                              {formatearFechaHora(usuario.ultimoAcceso)}
                            </Typography>
                            {!usuario.activo && (
                              <Chip size="small" label="Inactiva" color="error" variant="outlined" />
                            )}
                          </TableCell>

                          <TableCell align="right">
                            <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                              <Tooltip title="Restablecer contraseña">
                                <IconButton
                                  size="small"
                                  onClick={() => {
                                    formContrasena.reset({ contrasenaNueva: '', confirmacion: '' });
                                    setUsuarioContrasena(usuario);
                                  }}
                                >
                                  <KeyOutlinedIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>

                              <Tooltip title="Editar usuario">
                                <IconButton
                                  size="small"
                                  color="primary"
                                  onClick={() => abrirDialogo(usuario)}
                                >
                                  <EditOutlinedIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>

                              <Tooltip
                                title={
                                  esUnoMismo
                                    ? 'No puede desactivar su propia cuenta'
                                    : 'Desactivar cuenta'
                                }
                              >
                                <span>
                                  <IconButton
                                    size="small"
                                    color="error"
                                    disabled={esUnoMismo || !usuario.activo}
                                    onClick={() => setUsuarioDesactivar(usuario)}
                                  >
                                    <BlockOutlinedIcon fontSize="small" />
                                  </IconButton>
                                </span>
                              </Tooltip>
                            </Stack>
                          </TableCell>
                        </TableRow>
                      );
                    })
                  ) : (
                    <TableRow>
                      <TableCell colSpan={COLUMNAS} sx={{ border: 0 }}>
                        <EstadoVacio
                          icono={<ManageAccountsOutlinedIcon />}
                          titulo="Sin usuarios"
                          descripcion="No hay cuentas registradas en el sistema."
                        />
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>

            {datos && datos.totalRegistros > 0 && (
              <TablePagination
                component="div"
                count={datos.totalRegistros}
                page={pagina}
                onPageChange={(_, nueva) => setPagina(nueva)}
                rowsPerPage={tamanoPagina}
                onRowsPerPageChange={(e) => {
                  setTamanoPagina(Number(e.target.value));
                  setPagina(0);
                }}
                rowsPerPageOptions={[10, 25, 50]}
                labelRowsPerPage="Filas por página"
                labelDisplayedRows={({ from, to, count }) => `${from}–${to} de ${count}`}
              />
            )}
          </>
        )}
      </Card>

      {/* Alta y edición */}
      <Dialog open={dialogoAbierto} onClose={() => setDialogoAbierto(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          {usuarioEditando ? `Editar ${usuarioEditando.nombreUsuario}` : 'Nuevo usuario'}
        </DialogTitle>

        <Box
          component="form"
          onSubmit={formUsuario.handleSubmit((d) => mutacionGuardar.mutate(d))}
          noValidate
        >
          <DialogContent dividers>
            <Stack spacing={2.25}>
              <TextField
                label="Nombre completo"
                fullWidth
                autoFocus
                error={Boolean(formUsuario.formState.errors.nombre)}
                helperText={formUsuario.formState.errors.nombre?.message}
                {...formUsuario.register('nombre', { required: 'El nombre es obligatorio.' })}
              />

              <TextField
                label="Nombre de usuario"
                fullWidth
                disabled={Boolean(usuarioEditando)}
                error={Boolean(formUsuario.formState.errors.nombreUsuario)}
                helperText={
                  formUsuario.formState.errors.nombreUsuario?.message ??
                  (usuarioEditando ? 'El nombre de usuario no puede modificarse.' : undefined)
                }
                {...formUsuario.register('nombreUsuario', {
                  required: usuarioEditando ? false : 'El nombre de usuario es obligatorio.',
                })}
              />

              <TextField
                label="Correo electrónico"
                type="email"
                fullWidth
                error={Boolean(formUsuario.formState.errors.correo)}
                helperText={formUsuario.formState.errors.correo?.message}
                {...formUsuario.register('correo', {
                  required: 'El correo es obligatorio.',
                  pattern: {
                    value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                    message: 'El correo no tiene un formato válido.',
                  },
                })}
              />

              {!usuarioEditando && (
                <TextField
                  label="Contraseña inicial"
                  type="password"
                  fullWidth
                  error={Boolean(formUsuario.formState.errors.contrasena)}
                  helperText={
                    formUsuario.formState.errors.contrasena?.message ??
                    `Mínimo ${LONGITUD_MINIMA_CONTRASENA} caracteres.`
                  }
                  {...formUsuario.register('contrasena', {
                    required: 'La contraseña es obligatoria.',
                    minLength: {
                      value: LONGITUD_MINIMA_CONTRASENA,
                      message: `Debe tener al menos ${LONGITUD_MINIMA_CONTRASENA} caracteres.`,
                    },
                  })}
                />
              )}

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <Controller
                  name="rol"
                  control={formUsuario.control}
                  defaultValue="Recepcionista"
                  rules={{ required: 'Seleccioná un rol.' }}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      select
                      label="Rol"
                      fullWidth
                      error={Boolean(fieldState.error)}
                      helperText={
                        fieldState.error?.message ??
                        'El Administrador accede a reportes y a esta pantalla.'
                      }
                    >
                      <MenuItem value="Recepcionista">Recepcionista</MenuItem>
                      <MenuItem value="Administrador">Administrador</MenuItem>
                    </TextField>
                  )}
                />

                {usuarioEditando && (
                  <Controller
                    name="activo"
                    control={formUsuario.control}
                    defaultValue="true"
                    render={({ field }) => (
                      <TextField {...field} select label="Estado" fullWidth>
                        <MenuItem value="true">Activa</MenuItem>
                        <MenuItem value="false">Inactiva</MenuItem>
                      </TextField>
                    )}
                  />
                )}
              </Stack>
            </Stack>
          </DialogContent>

          <DialogActions sx={{ px: 3, py: 2 }}>
            <Button color="inherit" onClick={() => setDialogoAbierto(false)}>
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="contained"
              disabled={mutacionGuardar.isPending}
              startIcon={mutacionGuardar.isPending && <CircularProgress size={16} color="inherit" />}
            >
              {usuarioEditando ? 'Guardar cambios' : 'Crear usuario'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* Restablecimiento de contraseña */}
      <Dialog
        open={Boolean(usuarioContrasena)}
        onClose={() => setUsuarioContrasena(null)}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle>Restablecer contraseña</DialogTitle>

        <Box
          component="form"
          onSubmit={formContrasena.handleSubmit((d) => mutacionContrasena.mutate(d))}
          noValidate
        >
          <DialogContent dividers>
            <Stack spacing={2.25}>
              <Alert severity="warning">
                La contraseña de <strong>{usuarioContrasena?.nombreUsuario}</strong> se reemplaza de
                inmediato. Comuníquela por un medio seguro y pida que la cambie al entrar.
              </Alert>

              <TextField
                label="Contraseña nueva"
                type="password"
                fullWidth
                autoFocus
                error={Boolean(formContrasena.formState.errors.contrasenaNueva)}
                helperText={formContrasena.formState.errors.contrasenaNueva?.message}
                {...formContrasena.register('contrasenaNueva', {
                  required: 'La contraseña es obligatoria.',
                  minLength: {
                    value: LONGITUD_MINIMA_CONTRASENA,
                    message: `Debe tener al menos ${LONGITUD_MINIMA_CONTRASENA} caracteres.`,
                  },
                })}
              />

              <TextField
                label="Confirmar contraseña"
                type="password"
                fullWidth
                error={Boolean(formContrasena.formState.errors.confirmacion)}
                helperText={formContrasena.formState.errors.confirmacion?.message}
                {...formContrasena.register('confirmacion', {
                  validate: (valor, campos) =>
                    valor === campos.contrasenaNueva || 'Las contraseñas no coinciden.',
                })}
              />
            </Stack>
          </DialogContent>

          <DialogActions sx={{ px: 3, py: 2 }}>
            <Button color="inherit" onClick={() => setUsuarioContrasena(null)}>
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="contained"
              color="warning"
              disabled={mutacionContrasena.isPending}
              startIcon={
                mutacionContrasena.isPending && <CircularProgress size={16} color="inherit" />
              }
            >
              Restablecer
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <DialogoConfirmacion
        abierto={Boolean(usuarioDesactivar)}
        titulo="Desactivar cuenta"
        mensaje={`La cuenta de ${usuarioDesactivar?.nombreUsuario} dejará de poder iniciar sesión. El historial de sus operaciones se conserva.`}
        etiquetaConfirmar="Desactivar"
        color="error"
        enProceso={mutacionDesactivar.isPending}
        onConfirmar={() => usuarioDesactivar && mutacionDesactivar.mutate(usuarioDesactivar)}
        onCancelar={() => setUsuarioDesactivar(null)}
      />
    </Stack>
  );
}
