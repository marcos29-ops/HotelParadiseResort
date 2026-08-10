import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
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
  InputAdornment,
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
  Typography,
  alpha,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import LoginIcon from '@mui/icons-material/Login';
import LogoutIcon from '@mui/icons-material/Logout';
import PersonOutlineIcon from '@mui/icons-material/PersonOutline';
import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined';
import { apiEstadias, apiReservas } from '../api/servicios';
import { describirError } from '../api/clienteHttp';
import { useNotificaciones } from '../contexto/ContextoNotificaciones';
import { ChipEstadoEstadia, ChipEstadoReserva, EstadoVacio } from '../componentes/Estados';
import { DialogoConfirmacion } from '../componentes/DialogoConfirmacion';
import { formatearValor } from '../componentes/TarjetaKpi';
import { formatearFecha, formatearFechaHora } from '../utilidades/formato';
import { CURVA, DURACION } from '../tema/tema';
import type { Estadia, Reserva } from '../tipos/api';

/**
 * PANTALLA 4 — Check-in / Check-out.
 *
 * Buscador por habitación o cliente, tarjeta fija del huésped siempre visible
 * (minimiza la carga de memoria del recepcionista, Etapa 2), pestañas para cada
 * operación y tabla de consumos de la estadía.
 */
export function PantallaEstadias() {
  const notificar = useNotificaciones();
  const clienteConsultas = useQueryClient();
  const [parametrosUrl] = useSearchParams();

  const [termino, setTermino] = useState(parametrosUrl.get('habitacion') ?? '');
  const [pestana, setPestana] = useState(0);
  const [estadia, setEstadia] = useState<Estadia | null>(null);
  const [reservaPendiente, setReservaPendiente] = useState<Reserva | null>(null);
  const [buscando, setBuscando] = useState(false);
  const [sinResultado, setSinResultado] = useState(false);

  const [huespedes, setHuespedes] = useState(1);
  const [observacionesEntrada, setObservacionesEntrada] = useState('');
  const [observacionesSalida, setObservacionesSalida] = useState('');
  const [confirmarSalida, setConfirmarSalida] = useState(false);

  const idReservaUrl = parametrosUrl.get('reserva');

  // Llegada desde el listado de reservas: se precarga la reserva para el check-in.
  useEffect(() => {
    if (!idReservaUrl) return;

    apiReservas
      .obtener(Number(idReservaUrl))
      .then((reserva) => {
        setReservaPendiente(reserva);
        setEstadia(null);
        setHuespedes(reserva.cantidadHuespedes);
        setPestana(0);
      })
      .catch(() => notificar.error('No se pudo cargar la reserva indicada.'));
  }, [idReservaUrl, notificar]);

  const consultaCuenta = useQuery({
    queryKey: ['cuenta', estadia?.id],
    queryFn: () => apiEstadias.cuenta(estadia!.id),
    enabled: Boolean(estadia),
  });

  const buscar = async () => {
    const valor = termino.trim();
    if (!valor) return;

    setBuscando(true);
    setSinResultado(false);
    setReservaPendiente(null);

    try {
      const encontrada = await apiEstadias.buscarPorHabitacion(valor);
      setEstadia(encontrada);
      setHuespedes(encontrada.cantidadHuespedes);
      setPestana(1);
    } catch {
      // Sin estadía en curso: puede haber una reserva confirmada esperando check-in.
      setEstadia(null);

      try {
        const llegadas = await apiReservas.llegadasDelDia();
        const coincidencia = llegadas.find(
          (r) =>
            r.habitacionNumero.toLowerCase() === valor.toLowerCase() ||
            r.clienteNombre.toLowerCase().includes(valor.toLowerCase()) ||
            r.clienteIdentificacion.toLowerCase() === valor.toLowerCase(),
        );

        if (coincidencia) {
          setReservaPendiente(coincidencia);
          setHuespedes(coincidencia.cantidadHuespedes);
          setPestana(0);
        } else {
          setSinResultado(true);
        }
      } catch {
        setSinResultado(true);
      }
    } finally {
      setBuscando(false);
    }
  };

  const invalidar = () => {
    clienteConsultas.invalidateQueries({ queryKey: ['panel'] });
    clienteConsultas.invalidateQueries({ queryKey: ['habitaciones'] });
    clienteConsultas.invalidateQueries({ queryKey: ['reservas'] });
    clienteConsultas.invalidateQueries({ queryKey: ['estadias'] });
    // Tras el check-out la estadía queda pendiente de cobro: la pantalla de
    // facturación debe listarla sin que haga falta recargar.
    clienteConsultas.invalidateQueries({ queryKey: ['estadias-pendientes-factura'] });
  };

  const mutacionCheckIn = useMutation({
    mutationFn: () =>
      apiEstadias.checkIn({
        reservaId: reservaPendiente!.id,
        cantidadHuespedes: huespedes,
        observaciones: observacionesEntrada.trim() || null,
      }),
    onSuccess: (nueva) => {
      invalidar();
      setEstadia(nueva);
      setReservaPendiente(null);
      setObservacionesEntrada('');
      setPestana(1);
      notificar.exito(
        `Check-in registrado. La habitación ${nueva.habitacionNumero} pasó a estado "Ocupada".`,
      );
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const mutacionCheckOut = useMutation({
    mutationFn: () => apiEstadias.checkOut(estadia!.id, observacionesSalida.trim() || null),
    onSuccess: (cerrada) => {
      invalidar();
      setEstadia(cerrada);
      setObservacionesSalida('');
      setConfirmarSalida(false);
      notificar.exito('Check-out registrado. La estadía queda lista para facturar.');
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const huesped = estadia ?? reservaPendiente;

  return (
    <Stack spacing={2.5}>
      {/* Buscador */}
      <Card>
        <CardContent>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              label="Buscar por número de habitación o cliente"
              fullWidth
              value={termino}
              onChange={(e) => setTermino(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && buscar()}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon fontSize="small" color="action" />
                  </InputAdornment>
                ),
              }}
            />

            <Button
              variant="contained"
              onClick={buscar}
              disabled={buscando || !termino.trim()}
              startIcon={buscando ? <CircularProgress size={16} color="inherit" /> : <SearchIcon />}
              sx={{ minWidth: 140 }}
            >
              Buscar
            </Button>
          </Stack>

          <Collapse in={sinResultado} timeout={DURACION.normal}>
            <Alert severity="warning" sx={{ mt: 2 }} onClose={() => setSinResultado(false)}>
              No se encontró una estadía en curso ni una llegada prevista para hoy con ese dato.
            </Alert>
          </Collapse>
        </CardContent>
      </Card>

      {!huesped ? (
        <Card>
          <CardContent>
            <EstadoVacio
              icono={<PersonOutlineIcon />}
              titulo="Busque un huésped para comenzar"
              descripcion="Indique el número de habitación o el nombre del cliente para registrar su entrada o salida."
            />
          </CardContent>
        </Card>
      ) : (
        <Fade in timeout={DURACION.normal}>
          <Box>
            {/* Tarjeta fija del huésped: visible durante toda la operación */}
            <Card sx={{ mb: 2.5 }}>
              <CardContent>
                <Stack
                  direction={{ xs: 'column', md: 'row' }}
                  justifyContent="space-between"
                  spacing={2}
                >
                  <Box minWidth={0}>
                    <Typography variant="h3" gutterBottom>
                      {estadia?.clienteNombre ?? reservaPendiente?.clienteNombre}
                    </Typography>

                    <Typography variant="body2" color="text.secondary">
                      {estadia ? (
                        <>
                          Reserva {estadia.reservaCodigo} · {estadia.tipoHabitacion} · habitación{' '}
                          {estadia.habitacionNumero}
                        </>
                      ) : (
                        <>
                          Reserva {reservaPendiente!.codigo} · {reservaPendiente!.tipoHabitacion} ·
                          habitación {reservaPendiente!.habitacionNumero}
                        </>
                      )}
                    </Typography>

                    <Typography variant="body2" color="text.secondary">
                      {estadia ? (
                        <>
                          {formatearFecha(estadia.fechaCheckIn)} al{' '}
                          {formatearFecha(estadia.fechaSalidaPrevista)} ({estadia.noches} noches)
                        </>
                      ) : (
                        <>
                          {formatearFecha(reservaPendiente!.fechaEntrada)} al{' '}
                          {formatearFecha(reservaPendiente!.fechaSalida)} (
                          {reservaPendiente!.noches} noches)
                        </>
                      )}
                    </Typography>
                  </Box>

                  <Stack alignItems={{ xs: 'flex-start', md: 'flex-end' }} spacing={1}>
                    {estadia ? (
                      <ChipEstadoEstadia estado={estadia.estado} />
                    ) : (
                      <ChipEstadoReserva estado={reservaPendiente!.estado} />
                    )}

                    {estadia && (
                      <Typography variant="caption">
                        Consumos: {formatearValor(estadia.totalConsumos, 'moneda')}
                      </Typography>
                    )}
                  </Stack>
                </Stack>
              </CardContent>
            </Card>

            {/* Pestañas de operación */}
            <Card>
              <Tabs
                value={pestana}
                onChange={(_, valor) => setPestana(valor)}
                variant="fullWidth"
                sx={{ borderBottom: '1px solid', borderColor: 'divider' }}
              >
                <Tab label="Check-in" icon={<LoginIcon fontSize="small" />} iconPosition="start" />
                <Tab
                  label="Check-out"
                  icon={<LogoutIcon fontSize="small" />}
                  iconPosition="start"
                  disabled={!estadia}
                />
              </Tabs>

              <CardContent>
                {pestana === 0 ? (
                  <PanelCheckIn
                    reserva={reservaPendiente}
                    estadia={estadia}
                    huespedes={huespedes}
                    setHuespedes={setHuespedes}
                    observaciones={observacionesEntrada}
                    setObservaciones={setObservacionesEntrada}
                    enProceso={mutacionCheckIn.isPending}
                    onRegistrar={() => mutacionCheckIn.mutate()}
                  />
                ) : (
                  <PanelCheckOut
                    estadia={estadia}
                    total={consultaCuenta.data?.total}
                    observaciones={observacionesSalida}
                    setObservaciones={setObservacionesSalida}
                    enProceso={mutacionCheckOut.isPending}
                    onRegistrar={() => setConfirmarSalida(true)}
                  />
                )}
              </CardContent>
            </Card>

            {/* Consumos de la estadía */}
            {estadia && (
              <Card sx={{ mt: 2.5 }}>
                <CardContent>
                  <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
                    <Typography variant="h4">Consumos registrados en esta estadía</Typography>
                    <Chip
                      size="small"
                      label={formatearValor(estadia.totalConsumos, 'moneda')}
                      color="primary"
                      variant="outlined"
                    />
                  </Stack>

                  <Divider sx={{ mb: 1 }} />

                  {estadia.consumos.length === 0 ? (
                    <EstadoVacio
                      icono={<ReceiptLongOutlinedIcon />}
                      titulo="Sin consumos"
                      descripcion="El huésped todavía no registra consumos de servicios adicionales."
                    />
                  ) : (
                    <TableContainer>
                      <Table size="small">
                        <TableHead>
                          <TableRow>
                            <TableCell>Servicio</TableCell>
                            <TableCell>Descripción</TableCell>
                            <TableCell>Fecha</TableCell>
                            <TableCell align="right">Monto</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {estadia.consumos.map((consumo) => (
                            <TableRow key={consumo.id} hover>
                              <TableCell>{consumo.servicio}</TableCell>
                              <TableCell>{consumo.descripcion}</TableCell>
                              <TableCell>{formatearFechaHora(consumo.fechaConsumo)}</TableCell>
                              <TableCell align="right">
                                {formatearValor(consumo.monto, 'moneda')}
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  )}
                </CardContent>
              </Card>
            )}
          </Box>
        </Fade>
      )}

      {/* Confirmación del check-out: cierra la estadía y libera la habitación. */}
      <DialogoConfirmacion
        abierto={confirmarSalida}
        titulo="Registrar check-out"
        mensaje="Se cerrará la estadía y la habitación pasará a limpieza. La estadía quedará lista para facturar."
        detalle={
          estadia && (
            <Stack spacing={0.5}>
              <Typography variant="body2" fontWeight={600}>
                {estadia.clienteNombre}
              </Typography>
              <Typography variant="caption">
                Habitación {estadia.habitacionNumero} · {estadia.noches} noche(s)
              </Typography>
              <Typography variant="caption">
                Consumos: {formatearValor(estadia.totalConsumos, 'moneda')}
              </Typography>
            </Stack>
          )
        }
        etiquetaConfirmar="Registrar check-out"
        enProceso={mutacionCheckOut.isPending}
        onConfirmar={() => mutacionCheckOut.mutate()}
        onCancelar={() => setConfirmarSalida(false)}
      />
    </Stack>
  );
}

function PanelCheckIn({
  reserva,
  estadia,
  huespedes,
  setHuespedes,
  observaciones,
  setObservaciones,
  enProceso,
  onRegistrar,
}: {
  reserva: Reserva | null;
  estadia: Estadia | null;
  huespedes: number;
  setHuespedes: (valor: number) => void;
  observaciones: string;
  setObservaciones: (valor: string) => void;
  enProceso: boolean;
  onRegistrar: () => void;
}) {
  if (estadia) {
    return (
      <Alert severity="success">
        El check-in se registró el {formatearFechaHora(estadia.fechaCheckIn)} por{' '}
        {estadia.registradaPor}.
      </Alert>
    );
  }

  if (!reserva) {
    return <Alert severity="info">Busque una reserva confirmada para registrar la llegada.</Alert>;
  }

  return (
    <Stack spacing={2.5}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <TextField
          label="Fecha y hora de check-in"
          value={formatearFechaHora(new Date().toISOString())}
          fullWidth
          disabled
          helperText="Se registra el momento actual."
        />

        <TextField
          label="Número de huéspedes"
          type="number"
          fullWidth
          value={huespedes}
          onChange={(e) => setHuespedes(Number(e.target.value))}
          inputProps={{ min: 1, max: 20 }}
        />
      </Stack>

      <TextField
        label="Observaciones"
        fullWidth
        multiline
        rows={2}
        value={observaciones}
        onChange={(e) => setObservaciones(e.target.value)}
        placeholder="Solicitudes del huésped al momento de la llegada"
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems="center">
        <Button
          variant="contained"
          size="large"
          onClick={onRegistrar}
          disabled={enProceso || huespedes < 1}
          startIcon={enProceso ? <CircularProgress size={16} color="inherit" /> : <LoginIcon />}
        >
          {enProceso ? 'Registrando…' : 'Registrar check-in'}
        </Button>

        <Typography variant="caption">
          Al confirmar, la habitación {reserva.habitacionNumero} pasará a estado «Ocupada».
        </Typography>
      </Stack>
    </Stack>
  );
}

function PanelCheckOut({
  estadia,
  total,
  observaciones,
  setObservaciones,
  enProceso,
  onRegistrar,
}: {
  estadia: Estadia | null;
  total?: number;
  observaciones: string;
  setObservaciones: (valor: string) => void;
  enProceso: boolean;
  onRegistrar: () => void;
}) {
  if (!estadia) {
    return <Alert severity="info">No hay una estadía en curso.</Alert>;
  }

  if (estadia.estado !== 'EnCurso') {
    return (
      <Alert severity="success">
        El check-out se registró el {formatearFechaHora(estadia.fechaCheckOut)}.
        {estadia.estado === 'Finalizada' && ' La estadía está lista para facturarse.'}
      </Alert>
    );
  }

  return (
    <Stack spacing={2.5}>
      {total !== undefined && (
        <Box
          sx={{
            p: 2,
            borderRadius: 2,
            backgroundColor: (t) => alpha(t.palette.primary.main, 0.05),
            border: '1px solid',
            borderColor: (t) => alpha(t.palette.primary.main, 0.16),
            transition: `all ${DURACION.normal}ms ${CURVA}`,
          }}
        >
          <Typography variant="subtitle2">Total estimado de la cuenta</Typography>
          <Typography variant="h2" color="primary.main">
            {formatearValor(total, 'moneda')}
          </Typography>
          <Typography variant="caption">
            Hospedaje más consumos. El detalle se emite en la pantalla de facturación.
          </Typography>
        </Box>
      )}

      <TextField
        label="Observaciones de salida"
        fullWidth
        multiline
        rows={2}
        value={observaciones}
        onChange={(e) => setObservaciones(e.target.value)}
        placeholder="Estado de la habitación, novedades de la salida"
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems="center">
        <Button
          variant="contained"
          size="large"
          color="primary"
          onClick={onRegistrar}
          disabled={enProceso}
          startIcon={enProceso ? <CircularProgress size={16} color="inherit" /> : <LogoutIcon />}
        >
          {enProceso ? 'Registrando…' : 'Registrar check-out'}
        </Button>

        <Typography variant="caption">
          La habitación pasará a «En limpieza» y la estadía quedará lista para facturar.
        </Typography>
      </Stack>
    </Stack>
  );
}
