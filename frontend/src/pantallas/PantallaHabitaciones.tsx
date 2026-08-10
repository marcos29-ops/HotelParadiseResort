import { useMemo, useState } from 'react';
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
  InputAdornment,
  LinearProgress,
  MenuItem,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import MeetingRoomOutlinedIcon from '@mui/icons-material/MeetingRoomOutlined';
import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined';
import { apiHabitaciones } from '../api/servicios';
import { describirError } from '../api/clienteHttp';
import { useAutenticacion } from '../contexto/ContextoAutenticacion';
import { useNotificaciones } from '../contexto/ContextoNotificaciones';
import { ChipEstadoHabitacion, EstadoError, EstadoVacio, FilasEsqueleto } from '../componentes/Estados';
import { formatearValor } from '../componentes/TarjetaKpi';
import { etiquetaLegible } from '../utilidades/formato';
import { useBusquedaDiferida } from '../utilidades/hooks';
import type { EstadoHabitacion, Habitacion, TipoHabitacion } from '../tipos/api';

interface FormularioHabitacion {
  numero: string;
  piso: string;
  tipoHabitacionId: string;
  observaciones: string;
}

interface FormularioTipo {
  nombre: string;
  descripcion: string;
  tarifaBasePorNoche: string;
  capacidadMaxima: string;
}

/**
 * Estados a los que el personal puede llevar una habitación desde esta pantalla.
 * Las transiciones las valida el patrón State en el dominio: si una no procede,
 * la API responde 422 y el mensaje se muestra tal cual.
 */
const ESTADOS_OPERATIVOS: EstadoHabitacion[] = ['Disponible', 'EnLimpieza', 'EnMantenimiento'];

const COLUMNAS_HABITACION = 6;
const COLUMNAS_TIPO = 5;

/**
 * Gestión de habitaciones (RF02).
 *
 * El catálogo, las tarifas y el estado en tiempo real que exige el requerimiento.
 * Sin esta pantalla el inventario solo podía cargarse por Swagger, de modo que un
 * sistema recién instalado no tenía nada que reservar.
 *
 * Ver el inventario y cambiar el estado corresponde a todo el personal; crear y
 * editar queda reservado al Administrador, igual que en el controlador.
 */
export function PantallaHabitaciones() {
  const notificar = useNotificaciones();
  const clienteConsultas = useQueryClient();
  const { esAdministrador } = useAutenticacion();

  const [pestana, setPestana] = useState(0);
  const { texto, textoDiferido, setTexto } = useBusquedaDiferida();

  const [dialogoHabitacion, setDialogoHabitacion] = useState(false);
  const [habitacionEditando, setHabitacionEditando] = useState<Habitacion | null>(null);
  const [dialogoTipo, setDialogoTipo] = useState(false);
  const [tipoEditando, setTipoEditando] = useState<TipoHabitacion | null>(null);

  const consultaHabitaciones = useQuery({
    queryKey: ['habitaciones'],
    queryFn: () => apiHabitaciones.listar(),
  });

  const consultaTipos = useQuery({
    queryKey: ['tipos-habitacion'],
    queryFn: () => apiHabitaciones.tipos(),
  });

  const formHabitacion = useForm<FormularioHabitacion>();
  const formTipo = useForm<FormularioTipo>();

  const habitacionesFiltradas = useMemo(() => {
    const todas = consultaHabitaciones.data ?? [];
    if (!textoDiferido) return todas;

    const criterio = textoDiferido.toLowerCase();
    return todas.filter(
      (h) =>
        h.numero.toLowerCase().includes(criterio) ||
        h.tipoHabitacion.toLowerCase().includes(criterio) ||
        String(h.piso) === criterio,
    );
  }, [consultaHabitaciones.data, textoDiferido]);

  const tipos = consultaTipos.data ?? [];
  const tiposActivos = tipos.filter((t) => t.activo);

  // --- Habitaciones ---------------------------------------------------------

  const abrirDialogoHabitacion = (habitacion: Habitacion | null) => {
    setHabitacionEditando(habitacion);
    formHabitacion.reset({
      numero: habitacion?.numero ?? '',
      piso: habitacion ? String(habitacion.piso) : '1',
      tipoHabitacionId: habitacion
        ? String(habitacion.tipoHabitacionId)
        : String(tiposActivos[0]?.id ?? ''),
      observaciones: habitacion?.observaciones ?? '',
    });
    setDialogoHabitacion(true);
  };

  const mutacionHabitacion = useMutation({
    mutationFn: (datos: FormularioHabitacion) => {
      const comunes = {
        numero: datos.numero.trim(),
        piso: Number(datos.piso),
        tipoHabitacionId: Number(datos.tipoHabitacionId),
        observaciones: datos.observaciones.trim() || null,
      };

      return habitacionEditando
        ? apiHabitaciones.actualizar(habitacionEditando.id, { ...comunes, activo: true })
        : apiHabitaciones.crear(comunes);
    },
    onSuccess: (habitacion) => {
      clienteConsultas.invalidateQueries({ queryKey: ['habitaciones'] });
      clienteConsultas.invalidateQueries({ queryKey: ['tipos-habitacion'] });
      clienteConsultas.invalidateQueries({ queryKey: ['panel'] });
      setDialogoHabitacion(false);
      notificar.exito(
        habitacionEditando
          ? `Habitación ${habitacion.numero} actualizada.`
          : `Habitación ${habitacion.numero} agregada al inventario.`,
      );
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const mutacionEstado = useMutation({
    mutationFn: ({ id, estado }: { id: number; estado: EstadoHabitacion }) =>
      apiHabitaciones.cambiarEstado(id, estado),
    onSuccess: (habitacion) => {
      clienteConsultas.invalidateQueries({ queryKey: ['habitaciones'] });
      clienteConsultas.invalidateQueries({ queryKey: ['panel'] });
      notificar.exito(
        `La habitación ${habitacion.numero} pasó a «${etiquetaLegible(habitacion.estado)}».`,
      );
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  // --- Tipos de habitación --------------------------------------------------

  const abrirDialogoTipo = (tipo: TipoHabitacion | null) => {
    setTipoEditando(tipo);
    formTipo.reset({
      nombre: tipo?.nombre ?? '',
      descripcion: tipo?.descripcion ?? '',
      tarifaBasePorNoche: tipo ? String(tipo.tarifaBasePorNoche) : '',
      capacidadMaxima: tipo ? String(tipo.capacidadMaxima) : '2',
    });
    setDialogoTipo(true);
  };

  const mutacionTipo = useMutation({
    mutationFn: (datos: FormularioTipo) => {
      const comunes = {
        nombre: datos.nombre.trim(),
        descripcion: datos.descripcion.trim() || null,
        tarifaBasePorNoche: Number(datos.tarifaBasePorNoche),
        capacidadMaxima: Number(datos.capacidadMaxima),
      };

      return tipoEditando
        ? apiHabitaciones.actualizarTipo(tipoEditando.id, { ...comunes, activo: true })
        : apiHabitaciones.crearTipo(comunes);
    },
    onSuccess: (tipo) => {
      clienteConsultas.invalidateQueries({ queryKey: ['tipos-habitacion'] });
      clienteConsultas.invalidateQueries({ queryKey: ['habitaciones'] });
      setDialogoTipo(false);
      notificar.exito(
        tipoEditando ? `Tipo «${tipo.nombre}» actualizado.` : `Tipo «${tipo.nombre}» creado.`,
      );
    },
    onError: (error) => notificar.error(describirError(error)),
  });

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
              <Typography variant="h4">Gestión de habitaciones</Typography>
              <Typography variant="caption">
                {consultaHabitaciones.data
                  ? `${consultaHabitaciones.data.length} habitación(es) · ${tipos.length} tipo(s)`
                  : 'Cargando…'}
              </Typography>
            </Box>

            {pestana === 0 && (
              <TextField
                placeholder="Buscar por número, piso o tipo"
                value={texto}
                onChange={(e) => setTexto(e.target.value)}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon fontSize="small" color="action" />
                    </InputAdornment>
                  ),
                }}
                sx={{ minWidth: 280 }}
              />
            )}

            {esAdministrador && (
              <Button
                variant="contained"
                startIcon={<AddCircleOutlineIcon />}
                onClick={() => (pestana === 0 ? abrirDialogoHabitacion(null) : abrirDialogoTipo(null))}
                disabled={pestana === 0 && tiposActivos.length === 0}
                sx={{ whiteSpace: 'nowrap' }}
              >
                {pestana === 0 ? 'Nueva habitación' : 'Nuevo tipo'}
              </Button>
            )}
          </Stack>
        </CardContent>

        <Tabs value={pestana} onChange={(_, v) => setPestana(v)} sx={{ px: 2 }}>
          <Tab icon={<MeetingRoomOutlinedIcon />} iconPosition="start" label="Habitaciones" />
          <Tab icon={<CategoryOutlinedIcon />} iconPosition="start" label="Tipos y tarifas" />
        </Tabs>

        <Box sx={{ height: 3 }}>
          {(consultaHabitaciones.isFetching || consultaTipos.isFetching) &&
            !consultaHabitaciones.isPending && <LinearProgress />}
        </Box>

        <Divider />

        {/* --- Pestaña 1: inventario de habitaciones --- */}
        {pestana === 0 &&
          (consultaHabitaciones.isError ? (
            <Box p={3}>
              <EstadoError
                mensaje={describirError(consultaHabitaciones.error)}
                onReintentar={() => consultaHabitaciones.refetch()}
              />
            </Box>
          ) : (
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Número</TableCell>
                    <TableCell>Piso</TableCell>
                    <TableCell>Tipo</TableCell>
                    <TableCell align="right">Tarifa / noche</TableCell>
                    <TableCell align="center">Estado</TableCell>
                    <TableCell align="right">Acciones</TableCell>
                  </TableRow>
                </TableHead>

                <TableBody>
                  {consultaHabitaciones.isPending ? (
                    <FilasEsqueleto columnas={COLUMNAS_HABITACION} filas={5} />
                  ) : habitacionesFiltradas.length > 0 ? (
                    habitacionesFiltradas.map((habitacion) => (
                      <TableRow key={habitacion.id} hover>
                        <TableCell>
                          <Typography variant="body2" fontWeight={600}>
                            {habitacion.numero}
                          </Typography>
                        </TableCell>

                        <TableCell>
                          <Typography variant="body2">Piso {habitacion.piso}</Typography>
                        </TableCell>

                        <TableCell>
                          <Typography variant="body2">{habitacion.tipoHabitacion}</Typography>
                          <Typography variant="caption">
                            Hasta {habitacion.capacidadMaxima} huésped(es)
                          </Typography>
                        </TableCell>

                        <TableCell align="right">
                          <Typography variant="body2" fontWeight={600}>
                            {formatearValor(habitacion.tarifaBasePorNoche, 'moneda')}
                          </Typography>
                        </TableCell>

                        <TableCell align="center">
                          <ChipEstadoHabitacion estado={habitacion.estado} />
                        </TableCell>

                        <TableCell align="right">
                          <Stack direction="row" spacing={1} justifyContent="flex-end">
                            <TextField
                              select
                              size="small"
                              label="Cambiar estado"
                              value=""
                              onChange={(e) =>
                                mutacionEstado.mutate({
                                  id: habitacion.id,
                                  estado: e.target.value as EstadoHabitacion,
                                })
                              }
                              disabled={mutacionEstado.isPending}
                              sx={{ minWidth: 170 }}
                            >
                              {ESTADOS_OPERATIVOS.filter((e) => e !== habitacion.estado).map((e) => (
                                <MenuItem key={e} value={e}>
                                  {etiquetaLegible(e)}
                                </MenuItem>
                              ))}
                            </TextField>

                            {esAdministrador && (
                              <Tooltip title="Editar habitación">
                                <IconButton
                                  size="small"
                                  color="primary"
                                  onClick={() => abrirDialogoHabitacion(habitacion)}
                                >
                                  <EditOutlinedIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            )}
                          </Stack>
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={COLUMNAS_HABITACION} sx={{ border: 0 }}>
                        <EstadoVacio
                          icono={<MeetingRoomOutlinedIcon />}
                          titulo="Sin habitaciones"
                          descripcion={
                            textoDiferido
                              ? `Ninguna habitación coincide con «${textoDiferido}».`
                              : 'El inventario está vacío. Agregá habitaciones para poder registrar reservas.'
                          }
                          accion={
                            esAdministrador && tiposActivos.length > 0 ? (
                              <Button
                                variant="contained"
                                startIcon={<AddCircleOutlineIcon />}
                                onClick={() => abrirDialogoHabitacion(null)}
                              >
                                Agregar la primera habitación
                              </Button>
                            ) : undefined
                          }
                        />
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          ))}

        {/* --- Pestaña 2: tipos y tarifas --- */}
        {pestana === 1 &&
          (consultaTipos.isError ? (
            <Box p={3}>
              <EstadoError
                mensaje={describirError(consultaTipos.error)}
                onReintentar={() => consultaTipos.refetch()}
              />
            </Box>
          ) : (
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Tipo</TableCell>
                    <TableCell align="right">Tarifa base / noche</TableCell>
                    <TableCell align="center">Capacidad</TableCell>
                    <TableCell align="center">Habitaciones</TableCell>
                    <TableCell align="right">Acciones</TableCell>
                  </TableRow>
                </TableHead>

                <TableBody>
                  {consultaTipos.isPending ? (
                    <FilasEsqueleto columnas={COLUMNAS_TIPO} filas={4} />
                  ) : tipos.length > 0 ? (
                    tipos.map((tipo) => (
                      <TableRow key={tipo.id} hover>
                        <TableCell>
                          <Typography variant="body2" fontWeight={600}>
                            {tipo.nombre}
                          </Typography>
                          {tipo.descripcion && (
                            <Typography variant="caption">{tipo.descripcion}</Typography>
                          )}
                        </TableCell>

                        <TableCell align="right">
                          <Typography variant="body2" fontWeight={600}>
                            {formatearValor(tipo.tarifaBasePorNoche, 'moneda')}
                          </Typography>
                        </TableCell>

                        <TableCell align="center">
                          <Typography variant="body2">{tipo.capacidadMaxima}</Typography>
                        </TableCell>

                        <TableCell align="center">
                          <Chip
                            size="small"
                            label={tipo.cantidadHabitaciones}
                            color={tipo.cantidadHabitaciones > 0 ? 'primary' : 'default'}
                            variant={tipo.cantidadHabitaciones > 0 ? 'filled' : 'outlined'}
                          />
                        </TableCell>

                        <TableCell align="right">
                          {esAdministrador && (
                            <Tooltip title="Editar tipo">
                              <IconButton
                                size="small"
                                color="primary"
                                onClick={() => abrirDialogoTipo(tipo)}
                              >
                                <EditOutlinedIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={COLUMNAS_TIPO} sx={{ border: 0 }}>
                        <EstadoVacio
                          icono={<CategoryOutlinedIcon />}
                          titulo="Sin tipos de habitación"
                          descripcion="Definí al menos un tipo con su tarifa antes de agregar habitaciones."
                        />
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          ))}
      </Card>

      {/* Alta y edición de habitaciones */}
      <Dialog
        open={dialogoHabitacion}
        onClose={() => setDialogoHabitacion(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          {habitacionEditando ? `Editar habitación ${habitacionEditando.numero}` : 'Nueva habitación'}
        </DialogTitle>

        <Box
          component="form"
          onSubmit={formHabitacion.handleSubmit((d) => mutacionHabitacion.mutate(d))}
          noValidate
        >
          <DialogContent dividers>
            <Stack spacing={2.25}>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField
                  label="Número"
                  fullWidth
                  autoFocus
                  placeholder="204"
                  error={Boolean(formHabitacion.formState.errors.numero)}
                  helperText={formHabitacion.formState.errors.numero?.message}
                  {...formHabitacion.register('numero', {
                    required: 'El número es obligatorio.',
                  })}
                />

                <TextField
                  label="Piso"
                  type="number"
                  fullWidth
                  error={Boolean(formHabitacion.formState.errors.piso)}
                  helperText={formHabitacion.formState.errors.piso?.message}
                  {...formHabitacion.register('piso', {
                    required: 'El piso es obligatorio.',
                    min: { value: 1, message: 'El piso debe ser 1 o mayor.' },
                  })}
                />
              </Stack>

              {/*
                El desplegable va con Controller y no con register: MUI monta su Select
                sin valor y react-hook-form se lo asigna después, lo que React reporta
                como el paso de un campo no controlado a controlado.
              */}
              <Controller
                name="tipoHabitacionId"
                control={formHabitacion.control}
                rules={{ required: 'Seleccioná un tipo.' }}
                defaultValue=""
                render={({ field, fieldState }) => (
                  <TextField
                    {...field}
                    select
                    label="Tipo de habitación"
                    fullWidth
                    error={Boolean(fieldState.error)}
                    helperText={
                      fieldState.error?.message ?? 'La tarifa y la capacidad las define el tipo.'
                    }
                  >
                    {tiposActivos.map((tipo) => (
                      <MenuItem key={tipo.id} value={String(tipo.id)}>
                        {tipo.nombre} · {formatearValor(tipo.tarifaBasePorNoche, 'moneda')} ·{' '}
                        {tipo.capacidadMaxima} huésped(es)
                      </MenuItem>
                    ))}
                  </TextField>
                )}
              />

              <TextField
                label="Observaciones"
                fullWidth
                multiline
                rows={2}
                {...formHabitacion.register('observaciones')}
              />
            </Stack>
          </DialogContent>

          <DialogActions sx={{ px: 3, py: 2 }}>
            <Button color="inherit" onClick={() => setDialogoHabitacion(false)}>
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="contained"
              disabled={mutacionHabitacion.isPending}
              startIcon={
                mutacionHabitacion.isPending && <CircularProgress size={16} color="inherit" />
              }
            >
              {habitacionEditando ? 'Guardar cambios' : 'Agregar habitación'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* Alta y edición de tipos */}
      <Dialog open={dialogoTipo} onClose={() => setDialogoTipo(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          {tipoEditando ? `Editar tipo «${tipoEditando.nombre}»` : 'Nuevo tipo de habitación'}
        </DialogTitle>

        <Box
          component="form"
          onSubmit={formTipo.handleSubmit((d) => mutacionTipo.mutate(d))}
          noValidate
        >
          <DialogContent dividers>
            <Stack spacing={2.25}>
              {tipoEditando && tipoEditando.cantidadHabitaciones > 0 && (
                <Alert severity="info">
                  Cambiar la tarifa afecta a las reservas que se creen de ahora en adelante. Las ya
                  registradas conservan el monto con el que se confirmaron.
                </Alert>
              )}

              <TextField
                label="Nombre"
                fullWidth
                autoFocus
                placeholder="Doble – Vista al mar"
                error={Boolean(formTipo.formState.errors.nombre)}
                helperText={formTipo.formState.errors.nombre?.message}
                {...formTipo.register('nombre', { required: 'El nombre es obligatorio.' })}
              />

              <TextField
                label="Descripción"
                fullWidth
                multiline
                rows={2}
                {...formTipo.register('descripcion')}
              />

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField
                  label="Tarifa base por noche"
                  type="number"
                  fullWidth
                  InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment> }}
                  inputProps={{ step: '0.01' }}
                  error={Boolean(formTipo.formState.errors.tarifaBasePorNoche)}
                  helperText={formTipo.formState.errors.tarifaBasePorNoche?.message}
                  {...formTipo.register('tarifaBasePorNoche', {
                    required: 'La tarifa es obligatoria.',
                    min: { value: 0.01, message: 'La tarifa debe ser mayor que cero.' },
                  })}
                />

                <TextField
                  label="Capacidad máxima"
                  type="number"
                  fullWidth
                  error={Boolean(formTipo.formState.errors.capacidadMaxima)}
                  helperText={formTipo.formState.errors.capacidadMaxima?.message}
                  {...formTipo.register('capacidadMaxima', {
                    required: 'La capacidad es obligatoria.',
                    min: { value: 1, message: 'Debe admitir al menos un huésped.' },
                  })}
                />
              </Stack>
            </Stack>
          </DialogContent>

          <DialogActions sx={{ px: 3, py: 2 }}>
            <Button color="inherit" onClick={() => setDialogoTipo(false)}>
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="contained"
              disabled={mutacionTipo.isPending}
              startIcon={mutacionTipo.isPending && <CircularProgress size={16} color="inherit" />}
            >
              {tipoEditando ? 'Guardar cambios' : 'Crear tipo'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Stack>
  );
}
