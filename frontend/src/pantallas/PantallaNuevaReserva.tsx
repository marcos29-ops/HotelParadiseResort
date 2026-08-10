import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Collapse,
  Divider,
  Fade,
  FormControl,
  Grow,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
  alpha,
} from '@mui/material';
import PersonSearchOutlinedIcon from '@mui/icons-material/PersonSearchOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';
import EventAvailableIcon from '@mui/icons-material/EventAvailable';
import PersonAddAltOutlinedIcon from '@mui/icons-material/PersonAddAltOutlined';
import { apiClientes, apiHabitaciones, apiReservas } from '../api/servicios';
import { describirError } from '../api/clienteHttp';
import { useNotificaciones } from '../contexto/ContextoNotificaciones';
import { formatearValor } from '../componentes/TarjetaKpi';
import { CURVA, DURACION } from '../tema/tema';
import type { CanalOrigen, Cliente, HabitacionDisponible } from '../tipos/api';

interface Formulario {
  identificacion: string;
  nombre: string;
  apellidos: string;
  telefono: string;
  correo: string;
  fechaEntrada: string;
  fechaSalida: string;
  cantidadHuespedes: number;
  tipoHabitacionId: number | '';
  habitacionId: number | '';
  canalOrigen: CanalOrigen;
  observaciones: string;
}

const CANALES: { valor: CanalOrigen; etiqueta: string }[] = [
  { valor: 'Telefono', etiqueta: 'Teléfono' },
  { valor: 'CorreoElectronico', etiqueta: 'Correo electrónico' },
  { valor: 'RedesSociales', etiqueta: 'Redes sociales' },
  { valor: 'Presencial', etiqueta: 'Presencial' },
];

/**
 * PANTALLA 3 — Nueva reserva.
 *
 * Tres secciones, como el wireframe: datos del cliente, detalles de la reserva y
 * confirmación con la tarifa preliminar. La verificación de disponibilidad se
 * muestra con un aviso visual antes de confirmar (prevención de errores, Etapa 2).
 */
export function PantallaNuevaReserva() {
  const navegar = useNavigate();
  const ubicacion = useLocation();
  const notificar = useNotificaciones();
  const clienteConsultas = useQueryClient();

  const estadoPrevio = ubicacion.state as
    | { fechaEntrada?: string; fechaSalida?: string; tipoHabitacionId?: number | 'todos' }
    | null;

  const [clienteEncontrado, setClienteEncontrado] = useState<Cliente | null>(null);
  const [buscandoCliente, setBuscandoCliente] = useState(false);
  const [clienteEsNuevo, setClienteEsNuevo] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    getValues,
    formState: { errors },
  } = useForm<Formulario>({
    mode: 'onBlur',
    defaultValues: {
      identificacion: '',
      nombre: '',
      apellidos: '',
      telefono: '',
      correo: '',
      fechaEntrada: estadoPrevio?.fechaEntrada ?? new Date().toISOString().slice(0, 10),
      fechaSalida:
        estadoPrevio?.fechaSalida ??
        new Date(Date.now() + 2 * 86400000).toISOString().slice(0, 10),
      cantidadHuespedes: 2,
      tipoHabitacionId:
        typeof estadoPrevio?.tipoHabitacionId === 'number' ? estadoPrevio.tipoHabitacionId : '',
      habitacionId: '',
      canalOrigen: 'Telefono',
      observaciones: '',
    },
  });

  const fechaEntrada = watch('fechaEntrada');
  const fechaSalida = watch('fechaSalida');
  const tipoHabitacionId = watch('tipoHabitacionId');
  const cantidadHuespedes = watch('cantidadHuespedes');
  const habitacionId = watch('habitacionId');

  // Los datos del cliente se rellenan con setValue, y MUI no se entera de que el
  // campo dejó de estar vacío: la etiqueta se queda encima del valor y se lee
  // encimada. Al observar el valor podemos subir la etiqueta nosotros. Con
  // `undefined` se devuelve el control a MUI, que ya acierta al escribir a mano.
  const nombre = watch('nombre');
  const apellidos = watch('apellidos');
  const telefono = watch('telefono');
  const correo = watch('correo');

  const etiquetaArriba = (valor?: string) => ({ shrink: Boolean(valor) || undefined });

  const consultaTipos = useQuery({
    queryKey: ['tipos-habitacion'],
    queryFn: apiHabitaciones.tipos,
  });

  const rangoValido = Boolean(fechaEntrada && fechaSalida && fechaSalida >= fechaEntrada);

  const consultaDisponibilidad = useQuery({
    queryKey: ['disponibilidad', fechaEntrada, fechaSalida, tipoHabitacionId, cantidadHuespedes],
    queryFn: () =>
      apiHabitaciones.disponibilidad({
        fechaEntrada,
        fechaSalida,
        tipoHabitacionId: tipoHabitacionId === '' ? null : tipoHabitacionId,
        cantidadHuespedes: cantidadHuespedes || null,
      }),
    enabled: rangoValido,
  });

  const disponibles = consultaDisponibilidad.data ?? [];
  const seleccionada = disponibles.find((h) => h.id === habitacionId);

  // Si la habitación elegida deja de estar disponible al cambiar los filtros, se
  // limpia la selección para no enviar una reserva sobre datos obsoletos.
  useEffect(() => {
    if (habitacionId !== '' && !disponibles.some((h) => h.id === habitacionId)) {
      setValue('habitacionId', '');
    }
  }, [disponibles, habitacionId, setValue]);

  const buscarCliente = async () => {
    const identificacion = getValues('identificacion').trim();

    if (!identificacion) {
      notificar.advertencia('Ingrese la identificación del cliente para buscarlo.');
      return;
    }

    setBuscandoCliente(true);

    try {
      const cliente = await apiClientes.buscarPorIdentificacion(identificacion);

      setClienteEncontrado(cliente);
      setClienteEsNuevo(false);
      setValue('nombre', cliente.nombre);
      setValue('apellidos', cliente.apellidos);
      setValue('telefono', cliente.telefono ?? '');
      setValue('correo', cliente.correo ?? '');

      notificar.exito(`Cliente localizado: ${cliente.nombreCompleto}`);
    } catch {
      // No encontrarlo es un caso normal: se habilita el alta en el mismo paso.
      setClienteEncontrado(null);
      setClienteEsNuevo(true);
      setValue('nombre', '');
      setValue('apellidos', '');
      notificar.informacion('El cliente no está registrado. Complete sus datos para darlo de alta.');
    } finally {
      setBuscandoCliente(false);
    }
  };

  const mutacionCrear = useMutation({
    mutationFn: (datos: Formulario) =>
      apiReservas.crear(
        {
          clienteId: clienteEncontrado?.id ?? null,
          clienteNuevo: clienteEncontrado
            ? null
            : {
                identificacion: datos.identificacion.trim(),
                nombre: datos.nombre.trim(),
                apellidos: datos.apellidos.trim(),
                telefono: datos.telefono.trim() || null,
                correo: datos.correo.trim() || null,
              },
          habitacionId: Number(datos.habitacionId),
          fechaEntrada: datos.fechaEntrada,
          fechaSalida: datos.fechaSalida,
          cantidadHuespedes: Number(datos.cantidadHuespedes),
          canalOrigen: datos.canalOrigen,
          observaciones: datos.observaciones.trim() || null,
        },
        true,
      ),
    onSuccess: (reserva) => {
      clienteConsultas.invalidateQueries({ queryKey: ['reservas'] });
      clienteConsultas.invalidateQueries({ queryKey: ['habitaciones'] });
      clienteConsultas.invalidateQueries({ queryKey: ['panel'] });

      notificar.exito(
        `Reserva ${reserva.codigo} confirmada para ${reserva.clienteNombre} · habitación ${reserva.habitacionNumero}.`,
      );

      navegar('/reservas');
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const enviar = handleSubmit((datos) => {
    if (datos.habitacionId === '') {
      notificar.advertencia('Seleccione una habitación disponible antes de confirmar.');
      return;
    }

    mutacionCrear.mutate(datos);
  });

  return (
    <Box component="form" onSubmit={enviar} noValidate>
      <Stack spacing={2.5}>
        {/* --- Sección 1: datos del cliente --- */}
        <Card>
          <CardContent>
            <EncabezadoSeccion numero={1} titulo="Datos del cliente" />

            <Stack spacing={2}>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField
                  label="Cliente (buscar por identificación)"
                  fullWidth
                  error={Boolean(errors.identificacion)}
                  helperText={errors.identificacion?.message}
                  {...register('identificacion', {
                    required: 'La identificación del cliente es obligatoria.',
                  })}
                />

                <Button
                  variant="outlined"
                  onClick={buscarCliente}
                  disabled={buscandoCliente}
                  startIcon={
                    buscandoCliente ? (
                      <CircularProgress size={16} />
                    ) : (
                      <PersonSearchOutlinedIcon />
                    )
                  }
                  sx={{ minWidth: 230, whiteSpace: 'nowrap' }}
                >
                  Buscar / registrar nuevo
                </Button>
              </Stack>

              <Collapse in={Boolean(clienteEncontrado) || clienteEsNuevo} timeout={DURACION.normal}>
                <Alert
                  severity={clienteEncontrado ? 'success' : 'info'}
                  icon={clienteEncontrado ? <CheckCircleOutlineIcon /> : <PersonAddAltOutlinedIcon />}
                  sx={{ mb: 2 }}
                >
                  {clienteEncontrado
                    ? `Cliente registrado · ${clienteEncontrado.cantidadReservas} reserva(s) previa(s)`
                    : 'Cliente nuevo: se registrará junto con la reserva.'}
                </Alert>
              </Collapse>

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField
                  label="Nombre"
                  fullWidth
                  disabled={Boolean(clienteEncontrado)}
                  error={Boolean(errors.nombre)}
                  helperText={errors.nombre?.message}
                  InputLabelProps={etiquetaArriba(nombre)}
                  {...register('nombre', { required: 'El nombre es obligatorio.' })}
                />
                <TextField
                  label="Apellidos"
                  fullWidth
                  disabled={Boolean(clienteEncontrado)}
                  error={Boolean(errors.apellidos)}
                  helperText={errors.apellidos?.message}
                  InputLabelProps={etiquetaArriba(apellidos)}
                  {...register('apellidos', { required: 'Los apellidos son obligatorios.' })}
                />
              </Stack>

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField
                  label="Teléfono"
                  fullWidth
                  disabled={Boolean(clienteEncontrado)}
                  InputLabelProps={etiquetaArriba(telefono)}
                  {...register('telefono')}
                />
                <TextField
                  label="Correo electrónico"
                  type="email"
                  fullWidth
                  disabled={Boolean(clienteEncontrado)}
                  error={Boolean(errors.correo)}
                  helperText={errors.correo?.message}
                  InputLabelProps={etiquetaArriba(correo)}
                  {...register('correo', {
                    pattern: {
                      value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                      message: 'El correo no tiene un formato válido.',
                    },
                  })}
                />
              </Stack>
            </Stack>
          </CardContent>
        </Card>

        {/* --- Sección 2: detalles de la reserva --- */}
        <Card>
          <CardContent>
            <EncabezadoSeccion numero={2} titulo="Detalles de la reserva" />

            <Stack spacing={2}>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField
                  label="Fecha de entrada"
                  type="date"
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                  error={Boolean(errors.fechaEntrada)}
                  helperText={errors.fechaEntrada?.message}
                  {...register('fechaEntrada', { required: 'Indique la fecha de entrada.' })}
                />
                <TextField
                  label="Fecha de salida"
                  type="date"
                  fullWidth
                  InputLabelProps={{ shrink: true }}
                  error={Boolean(errors.fechaSalida)}
                  helperText={
                    errors.fechaSalida?.message ??
                    (!rangoValido && fechaSalida ? 'Debe ser posterior a la entrada.' : undefined)
                  }
                  {...register('fechaSalida', { required: 'Indique la fecha de salida.' })}
                />
                <TextField
                  label="Número de huéspedes"
                  type="number"
                  fullWidth
                  inputProps={{ min: 1, max: 20 }}
                  error={Boolean(errors.cantidadHuespedes)}
                  helperText={errors.cantidadHuespedes?.message}
                  {...register('cantidadHuespedes', {
                    required: 'Indique la cantidad de huéspedes.',
                    min: { value: 1, message: 'Debe haber al menos un huésped.' },
                    valueAsNumber: true,
                  })}
                />
              </Stack>

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <FormControl fullWidth>
                  <InputLabel id="tipo-habitacion">Tipo de habitación</InputLabel>
                  <Select
                    labelId="tipo-habitacion"
                    label="Tipo de habitación"
                    value={tipoHabitacionId}
                    onChange={(e) => {
                      // MUI tipa el valor según el primer MenuItem; se normaliza a la
                      // unión que realmente maneja el formulario.
                      const valor = e.target.value as number | '';
                      setValue('tipoHabitacionId', valor === '' ? '' : Number(valor));
                    }}
                  >
                    <MenuItem value="">Todos los tipos</MenuItem>
                    {(consultaTipos.data ?? []).map((tipo) => (
                      <MenuItem key={tipo.id} value={tipo.id}>
                        {tipo.nombre} · {formatearValor(tipo.tarifaBasePorNoche, 'moneda')} por noche
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>

                <FormControl fullWidth>
                  <InputLabel id="canal-origen">Canal de origen</InputLabel>
                  <Select
                    labelId="canal-origen"
                    label="Canal de origen"
                    defaultValue="Telefono"
                    {...register('canalOrigen')}
                  >
                    {CANALES.map((canal) => (
                      <MenuItem key={canal.valor} value={canal.valor}>
                        {canal.etiqueta}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Stack>

              <TextField
                label="Observaciones"
                fullWidth
                multiline
                rows={2}
                placeholder="Solicitudes especiales del huésped"
                {...register('observaciones')}
              />
            </Stack>

            <Divider sx={{ my: 2.5 }} />

            {/* Verificación de disponibilidad con aviso visual */}
            <AvisoDisponibilidad
              cargando={consultaDisponibilidad.isFetching}
              rangoValido={rangoValido}
              cantidad={disponibles.length}
              error={consultaDisponibilidad.isError ? describirError(consultaDisponibilidad.error) : null}
            />

            {disponibles.length > 0 && (
              <Box
                sx={{
                  display: 'grid',
                  gap: 1.5,
                  mt: 2,
                  gridTemplateColumns: {
                    xs: '1fr',
                    sm: 'repeat(2, 1fr)',
                    lg: 'repeat(3, 1fr)',
                  },
                }}
              >
                {disponibles.map((habitacion, indice) => (
                  <TarjetaDisponible
                    key={habitacion.id}
                    habitacion={habitacion}
                    seleccionada={habitacionId === habitacion.id}
                    retraso={indice * 30}
                    onSeleccionar={() => setValue('habitacionId', habitacion.id)}
                  />
                ))}
              </Box>
            )}
          </CardContent>
        </Card>

        {/* --- Sección 3: confirmación --- */}
        <Card>
          <CardContent>
            <EncabezadoSeccion numero={3} titulo="Confirmación" />

            <Collapse in={Boolean(seleccionada)} timeout={DURACION.normal}>
              {seleccionada && (
                <Box
                  sx={{
                    p: 2.5,
                    mb: 2.5,
                    borderRadius: 2,
                    backgroundColor: (t) => alpha(t.palette.primary.main, 0.05),
                    border: '1px solid',
                    borderColor: (t) => alpha(t.palette.primary.main, 0.18),
                  }}
                >
                  <Typography variant="subtitle2" gutterBottom>
                    Tarifa preliminar
                  </Typography>

                  <Stack
                    direction={{ xs: 'column', sm: 'row' }}
                    alignItems={{ sm: 'baseline' }}
                    spacing={1}
                  >
                    <Typography variant="h2" color="primary.main">
                      {formatearValor(seleccionada.montoTotal, 'moneda')}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {seleccionada.noches} noche(s) ×{' '}
                      {formatearValor(seleccionada.tarifaPorNoche, 'moneda')} · habitación{' '}
                      {seleccionada.numero}
                    </Typography>
                  </Stack>

                  <Typography variant="caption" display="block" sx={{ mt: 0.5 }}>
                    {seleccionada.descripcionTarifa} · La reserva se confirmará al registrarla.
                  </Typography>
                </Box>
              )}
            </Collapse>

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} justifyContent="flex-end">
              <Button
                variant="outlined"
                color="inherit"
                onClick={() => navegar('/reservas')}
                disabled={mutacionCrear.isPending}
              >
                Cancelar
              </Button>

              <Button
                type="submit"
                variant="contained"
                size="large"
                disabled={mutacionCrear.isPending || !seleccionada}
                startIcon={
                  mutacionCrear.isPending ? (
                    <CircularProgress size={16} color="inherit" />
                  ) : (
                    <EventAvailableIcon />
                  )
                }
              >
                {mutacionCrear.isPending ? 'Confirmando…' : 'Confirmar reserva'}
              </Button>
            </Stack>
          </CardContent>
        </Card>
      </Stack>
    </Box>
  );
}

function EncabezadoSeccion({ numero, titulo }: { numero: number; titulo: string }) {
  return (
    <Stack direction="row" alignItems="center" spacing={1.5} mb={2.5}>
      <Box
        sx={{
          width: 26,
          height: 26,
          borderRadius: '50%',
          display: 'grid',
          placeItems: 'center',
          fontSize: '0.78rem',
          fontWeight: 700,
          backgroundColor: 'primary.main',
          color: 'primary.contrastText',
        }}
      >
        {numero}
      </Box>
      <Typography variant="h4">{titulo}</Typography>
    </Stack>
  );
}

function AvisoDisponibilidad({
  cargando,
  rangoValido,
  cantidad,
  error,
}: {
  cargando: boolean;
  rangoValido: boolean;
  cantidad: number;
  error: string | null;
}) {
  if (!rangoValido) {
    return (
      <Alert severity="info" icon={<ErrorOutlineIcon />}>
        Seleccione un rango de fechas válido para consultar la disponibilidad.
      </Alert>
    );
  }

  if (cargando) {
    return (
      <Alert severity="info" icon={<CircularProgress size={18} />}>
        Verificando disponibilidad…
      </Alert>
    );
  }

  if (error) {
    return <Alert severity="error">{error}</Alert>;
  }

  return (
    <Fade in timeout={DURACION.normal}>
      <Alert severity={cantidad > 0 ? 'success' : 'warning'}>
        {cantidad > 0
          ? `Disponibilidad verificada: ${cantidad} habitación(es) disponible(s) para las fechas seleccionadas.`
          : 'No hay habitaciones disponibles para las fechas seleccionadas. Pruebe con otro rango o tipo de habitación.'}
      </Alert>
    </Fade>
  );
}

function TarjetaDisponible({
  habitacion,
  seleccionada,
  retraso,
  onSeleccionar,
}: {
  habitacion: HabitacionDisponible;
  seleccionada: boolean;
  retraso: number;
  onSeleccionar: () => void;
}) {
  return (
    <Grow in timeout={DURACION.pausada} style={{ transitionDelay: `${retraso}ms` }}>
      <Box
        role="button"
        tabIndex={0}
        onClick={onSeleccionar}
        onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && onSeleccionar()}
        sx={{
          p: 2,
          borderRadius: 2,
          cursor: 'pointer',
          border: '1px solid',
          borderColor: seleccionada ? 'primary.main' : 'divider',
          backgroundColor: (t) =>
            seleccionada ? alpha(t.palette.primary.main, 0.06) : 'background.paper',
          transition: `all ${DURACION.rapida}ms ${CURVA}`,
          '&:hover': {
            borderColor: 'primary.main',
            transform: 'translateY(-2px)',
            boxShadow: '0 8px 18px rgba(26,32,39,0.08)',
          },
        }}
      >
        <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
          <Box minWidth={0}>
            <Typography variant="h4">Habitación {habitacion.numero}</Typography>
            <Typography variant="caption" display="block" noWrap>
              {habitacion.tipoHabitacion} · hasta {habitacion.capacidadMaxima} huésped(es)
            </Typography>
          </Box>

          {seleccionada && (
            <CheckCircleOutlineIcon color="primary" fontSize="small" sx={{ flexShrink: 0 }} />
          )}
        </Stack>

        <Stack direction="row" alignItems="baseline" spacing={0.75} mt={1.5}>
          <Typography variant="h4" color="primary.main">
            {formatearValor(habitacion.montoTotal, 'moneda')}
          </Typography>
          <Typography variant="caption">
            {habitacion.noches} × {formatearValor(habitacion.tarifaPorNoche, 'moneda')}
          </Typography>
        </Stack>

        {habitacion.estrategiaTarifa !== 'ESTANDAR' && (
          <Chip size="small" label={habitacion.descripcionTarifa} color="warning" sx={{ mt: 1 }} />
        )}
      </Box>
    </Grow>
  );
}
