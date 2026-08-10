import { useState } from 'react';
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
  FormControl,
  FormControlLabel,
  Grow,
  InputAdornment,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
  alpha,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong';
import PrintOutlinedIcon from '@mui/icons-material/PrintOutlined';
import PaymentsOutlinedIcon from '@mui/icons-material/PaymentsOutlined';
import { apiEstadias, apiFacturacion } from '../api/servicios';
import { describirError } from '../api/clienteHttp';
import { useNotificaciones } from '../contexto/ContextoNotificaciones';
import { ChipEstadoPago, EstadoVacio } from '../componentes/Estados';
import { DialogoConfirmacion } from '../componentes/DialogoConfirmacion';
import { formatearValor } from '../componentes/TarjetaKpi';
import { etiquetaLegible, formatearFecha, formatearFechaHora } from '../utilidades/formato';
import { CURVA, DURACION } from '../tema/tema';
import type { CuentaEstadia, Estadia, Factura, MetodoPago } from '../tipos/api';

const METODOS: { valor: MetodoPago; etiqueta: string }[] = [
  { valor: 'Efectivo', etiqueta: 'Efectivo' },
  { valor: 'TarjetaCredito', etiqueta: 'Tarjeta de crédito' },
  { valor: 'TarjetaDebito', etiqueta: 'Tarjeta de débito' },
  { valor: 'TransferenciaBancaria', etiqueta: 'Transferencia bancaria' },
];

/**
 * PANTALLA 5 — Facturación.
 *
 * Reproduce el wireframe: detalle de hospedaje, tabla de consumos, método y estado
 * de pago, totales con descuento y las acciones Reimprimir / Emitir factura.
 */
export function PantallaFacturacion() {
  const notificar = useNotificaciones();
  const clienteConsultas = useQueryClient();

  const [termino, setTermino] = useState('');
  const [estadia, setEstadia] = useState<Estadia | null>(null);
  const [factura, setFactura] = useState<Factura | null>(null);
  const [cuenta, setCuenta] = useState<CuentaEstadia | null>(null);
  const [buscando, setBuscando] = useState(false);
  const [mensajeBusqueda, setMensajeBusqueda] = useState<string | null>(null);

  const [metodoPago, setMetodoPago] = useState<MetodoPago>('Efectivo');
  const [pagarAhora, setPagarAhora] = useState(true);
  const [descuento, setDescuento] = useState('');
  const [justificacion, setJustificacion] = useState('');
  const [confirmarEmision, setConfirmarEmision] = useState(false);

  /**
   * Estadías con el check-out ya registrado y sin factura: son exactamente las que
   * quedan pendientes de cobrar.
   *
   * Antes esta pantalla solo sabía llegar a una estadía por «/estadias/por-habitacion»,
   * que devuelve únicamente las que están EnCurso. Tras el check-out la estadía pasa a
   * Finalizada, de modo que el buscador daba 404 y no había forma de facturarla.
   */
  const consultaPendientes = useQuery({
    queryKey: ['estadias-pendientes-factura'],
    queryFn: () => apiEstadias.listar({ pagina: 1, tamanoPagina: 50, estado: 'Finalizada' }),
  });

  const pendientes = (consultaPendientes.data?.elementos ?? []).filter((e) => !e.tieneFactura);

  const cargarEstadia = async (encontrada: Estadia) => {
    setEstadia(encontrada);
    setCuenta(await apiEstadias.cuenta(encontrada.id));

    if (encontrada.tieneFactura) {
      setFactura(await apiFacturacion.porEstadia(encontrada.id));
    }
  };

  const seleccionarPendiente = async (pendiente: Estadia) => {
    setMensajeBusqueda(null);
    setFactura(null);
    setCuenta(null);
    setEstadia(null);

    try {
      await cargarEstadia(await apiEstadias.obtener(pendiente.id));
    } catch (error) {
      setMensajeBusqueda(describirError(error));
    }
  };

  const buscar = async () => {
    const valor = termino.trim();
    if (!valor) return;

    setBuscando(true);
    setMensajeBusqueda(null);
    setFactura(null);
    setCuenta(null);
    setEstadia(null);

    try {
      await cargarEstadia(await apiEstadias.buscarPorHabitacion(valor));
    } catch {
      // No hay estadía en curso en esa habitación. Puede ser una que ya hizo
      // check-out y espera factura, o una factura ya emitida.
      try {
        const criterio = valor.toLowerCase();
        const pendiente = pendientes.find(
          (e) =>
            e.habitacionNumero.toLowerCase() === criterio ||
            e.clienteIdentificacion.toLowerCase() === criterio ||
            e.clienteNombre.toLowerCase().includes(criterio),
        );

        if (pendiente) {
          await cargarEstadia(await apiEstadias.obtener(pendiente.id));
          return;
        }

        const listado = await apiFacturacion.listar({ busqueda: valor, tamanoPagina: 1 });

        if (listado.elementos.length > 0) {
          setFactura(await apiFacturacion.obtener(listado.elementos[0].id));
        } else {
          setMensajeBusqueda(
            `No hay ninguna estadía ni factura que corresponda a «${valor}». Verifique el número de habitación, el comprobante o el cliente.`,
          );
        }
      } catch (error) {
        setMensajeBusqueda(describirError(error));
      }
    } finally {
      setBuscando(false);
    }
  };

  const mutacionEmitir = useMutation({
    mutationFn: () =>
      apiFacturacion.generar({
        estadiaId: estadia!.id,
        descuento: descuento === '' ? null : Number(descuento),
        justificacionDescuento: descuento === '' ? null : justificacion.trim(),
        metodoPago,
        registrarPagoInmediato: pagarAhora,
      }),
    onSuccess: (emitida) => {
      setFactura(emitida);
      setConfirmarEmision(false);
      clienteConsultas.invalidateQueries({ queryKey: ['panel'] });
      clienteConsultas.invalidateQueries({ queryKey: ['facturas'] });
      clienteConsultas.invalidateQueries({ queryKey: ['estadias-pendientes-factura'] });
      notificar.exito(
        `Factura ${emitida.numero} emitida por ${formatearValor(emitida.total, 'moneda')}.`,
      );
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const mutacionPagar = useMutation({
    mutationFn: () => apiFacturacion.registrarPago(factura!.id, metodoPago),
    onSuccess: (actualizada) => {
      setFactura(actualizada);
      notificar.exito('Pago registrado correctamente.');
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const descuentoInvalido = descuento !== '' && !justificacion.trim();
  const puedeFacturar = estadia?.estado === 'Finalizada' && !factura && !descuentoInvalido;

  const detalle = factura?.detalles ?? cuenta?.lineas ?? [];
  const hospedaje = detalle.filter((d) => d.esHospedaje);
  const consumos = detalle.filter((d) => !d.esHospedaje);

  const subtotalHospedaje = factura?.subtotalHospedaje ?? cuenta?.subtotalHospedaje ?? 0;
  const subtotalConsumos = factura?.subtotalConsumos ?? cuenta?.subtotalConsumos ?? 0;
  const descuentoAplicado = factura?.descuento ?? (descuento === '' ? 0 : Number(descuento) || 0);
  const total = factura?.total ?? subtotalHospedaje + subtotalConsumos - descuentoAplicado;

  return (
    <Stack spacing={2.5}>
      <Card>
        <CardContent>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              label="Buscar por habitación, número de factura o cliente"
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

          <Collapse in={Boolean(mensajeBusqueda)} timeout={DURACION.normal}>
            <Alert severity="warning" sx={{ mt: 2 }} onClose={() => setMensajeBusqueda(null)}>
              {mensajeBusqueda}
            </Alert>
          </Collapse>
        </CardContent>
      </Card>

      {!estadia && !factura ? (
        <Card>
          {pendientes.length > 0 ? (
            <>
              <CardContent sx={{ pb: 1.5 }}>
                <Typography variant="h4">Pendientes de facturar</Typography>
                <Typography variant="caption">
                  {pendientes.length} estadía(s) con el check-out registrado y sin comprobante
                  emitido.
                </Typography>
              </CardContent>

              <Divider />

              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Habitación</TableCell>
                      <TableCell>Huésped</TableCell>
                      <TableCell align="center">Noches</TableCell>
                      <TableCell align="right">Consumos</TableCell>
                      <TableCell align="right">Acción</TableCell>
                    </TableRow>
                  </TableHead>

                  <TableBody>
                    {pendientes.map((pendiente) => (
                      <TableRow key={pendiente.id} hover>
                        <TableCell>
                          <Typography variant="body2" fontWeight={600}>
                            {pendiente.habitacionNumero}
                          </Typography>
                          <Typography variant="caption">{pendiente.tipoHabitacion}</Typography>
                        </TableCell>

                        <TableCell>
                          <Typography variant="body2">{pendiente.clienteNombre}</Typography>
                          <Typography variant="caption">
                            {pendiente.clienteIdentificacion}
                          </Typography>
                        </TableCell>

                        <TableCell align="center">
                          <Typography variant="body2">{pendiente.noches}</Typography>
                        </TableCell>

                        <TableCell align="right">
                          <Typography variant="body2">
                            {formatearValor(pendiente.totalConsumos, 'moneda')}
                          </Typography>
                        </TableCell>

                        <TableCell align="right">
                          <Button
                            size="small"
                            variant="contained"
                            startIcon={<ReceiptLongIcon />}
                            onClick={() => seleccionarPendiente(pendiente)}
                          >
                            Facturar
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </>
          ) : (
            <CardContent>
              <EstadoVacio
                icono={<ReceiptLongIcon />}
                titulo={
                  consultaPendientes.isPending ? 'Cargando…' : 'No hay estadías pendientes de cobro'
                }
                descripcion="Cuando registre un check-out, la estadía aparecerá aquí lista para facturar. También puede buscar un comprobante ya emitido para reimprimirlo."
              />
            </CardContent>
          )}
        </Card>
      ) : (
        <Grow in timeout={DURACION.pausada}>
          <Box>
            {/* Cabecera del huésped */}
            <Card sx={{ mb: 2.5 }}>
              <CardContent>
                <Stack
                  direction={{ xs: 'column', md: 'row' }}
                  justifyContent="space-between"
                  spacing={2}
                >
                  <Box>
                    <Typography variant="h3">
                      {factura?.clienteNombre ?? estadia?.clienteNombre}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Reserva {factura?.reservaCodigo ?? estadia?.reservaCodigo} · habitación{' '}
                      {factura?.habitacionNumero ?? estadia?.habitacionNumero} ·{' '}
                      {formatearFecha(factura?.fechaEntrada ?? estadia?.fechaCheckIn)} al{' '}
                      {formatearFecha(factura?.fechaSalida ?? estadia?.fechaSalidaPrevista)}
                    </Typography>
                  </Box>

                  {factura && (
                    <Stack alignItems={{ xs: 'flex-start', md: 'flex-end' }} spacing={0.75}>
                      <Chip label={factura.numero} color="primary" />
                      {factura.metodoPago && (
                        <Typography variant="caption">
                          {etiquetaLegible(factura.metodoPago)}
                        </Typography>
                      )}
                      <ChipEstadoPago estado={factura.estadoPago} />
                      <Typography variant="caption">
                        Emitida {formatearFechaHora(factura.fechaEmision)}
                      </Typography>
                    </Stack>
                  )}
                </Stack>
              </CardContent>
            </Card>

            {estadia?.estado === 'EnCurso' && !factura && (
              <Alert severity="warning" sx={{ mb: 2.5 }}>
                La estadía sigue en curso. Registre el check-out antes de emitir la factura.
              </Alert>
            )}

            {/* Detalle de hospedaje */}
            <Card sx={{ mb: 2.5 }}>
              <CardContent>
                <Typography variant="h4" gutterBottom>
                  Detalle de hospedaje
                </Typography>

                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Concepto</TableCell>
                        <TableCell align="center">Cantidad</TableCell>
                        <TableCell align="right">Precio unitario</TableCell>
                        <TableCell align="right">Subtotal</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {hospedaje.map((linea, indice) => (
                        <TableRow key={indice}>
                          <TableCell>{linea.concepto}</TableCell>
                          <TableCell align="center">{linea.cantidad} noches</TableCell>
                          <TableCell align="right">
                            {formatearValor(linea.precioUnitario, 'moneda')}
                          </TableCell>
                          <TableCell align="right">
                            {formatearValor(linea.subtotal, 'moneda')}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>

                <Typography variant="h4" sx={{ mt: 3, mb: 1 }}>
                  Consumos adicionales
                </Typography>

                {consumos.length === 0 ? (
                  <Typography variant="body2" color="text.secondary" py={2}>
                    El huésped no registró consumos de servicios adicionales.
                  </Typography>
                ) : (
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Servicio</TableCell>
                          <TableCell>Fecha</TableCell>
                          <TableCell align="center">Cant.</TableCell>
                          <TableCell align="right">Monto</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {consumos.map((linea, indice) => (
                          <TableRow key={indice}>
                            <TableCell>{linea.concepto}</TableCell>
                            <TableCell>{formatearFecha(fechaDeLinea(linea))}</TableCell>
                            <TableCell align="center">{linea.cantidad}</TableCell>
                            <TableCell align="right">
                              {formatearValor(linea.subtotal, 'moneda')}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                )}
              </CardContent>
            </Card>

            {/* Pago y totales */}
            <Card>
              <CardContent>
                <Stack direction={{ xs: 'column', md: 'row' }} spacing={3}>
                  {/* Columna de pago */}
                  <Stack spacing={2.25} flex={1}>
                    <Typography variant="h4">Pago</Typography>

                    <FormControl fullWidth disabled={factura?.estadoPago === 'Pagado'}>
                      <InputLabel id="metodo-pago">Método de pago</InputLabel>
                      <Select
                        labelId="metodo-pago"
                        label="Método de pago"
                        value={metodoPago}
                        onChange={(e) => setMetodoPago(e.target.value as MetodoPago)}
                      >
                        {METODOS.map((metodo) => (
                          <MenuItem key={metodo.valor} value={metodo.valor}>
                            {metodo.etiqueta}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>

                    {!factura && (
                      <>
                        <FormControlLabel
                          control={
                            <Switch
                              checked={pagarAhora}
                              onChange={(e) => setPagarAhora(e.target.checked)}
                            />
                          }
                          label={
                            <Typography variant="body2">
                              Registrar el pago al emitir la factura
                            </Typography>
                          }
                        />

                        <Divider />

                        <TextField
                          label="Descuento"
                          type="number"
                          value={descuento}
                          onChange={(e) => setDescuento(e.target.value)}
                          inputProps={{ min: 0, step: 0.01 }}
                          InputProps={{
                            startAdornment: <InputAdornment position="start">$</InputAdornment>,
                          }}
                          helperText="Opcional. Requiere justificación."
                        />

                        <Collapse in={descuento !== ''} timeout={DURACION.normal}>
                          <TextField
                            label="Justificación del descuento"
                            fullWidth
                            multiline
                            rows={2}
                            value={justificacion}
                            onChange={(e) => setJustificacion(e.target.value)}
                            error={descuentoInvalido}
                            helperText={
                              descuentoInvalido
                                ? 'Todo descuento debe justificarse para la auditoría.'
                                : 'Queda registrada en el comprobante.'
                            }
                          />
                        </Collapse>
                      </>
                    )}

                    {factura?.justificacionDescuento && (
                      <Alert severity="info">
                        Descuento aplicado: {factura.justificacionDescuento}
                      </Alert>
                    )}
                  </Stack>

                  {/* Columna de totales */}
                  <Box
                    sx={{
                      minWidth: { md: 320 },
                      p: 2.5,
                      borderRadius: 2,
                      alignSelf: 'flex-start',
                      backgroundColor: (t) => alpha(t.palette.primary.main, 0.04),
                      border: '1px solid',
                      borderColor: 'divider',
                    }}
                  >
                    <FilaTotal etiqueta="Subtotal hospedaje" valor={subtotalHospedaje} />
                    <FilaTotal etiqueta="Subtotal consumos" valor={subtotalConsumos} />

                    <Collapse in={descuentoAplicado > 0} timeout={DURACION.rapida}>
                      <FilaTotal
                        etiqueta="Descuento aplicado"
                        valor={-descuentoAplicado}
                        color="error.main"
                      />
                    </Collapse>

                    <Divider sx={{ my: 1.5 }} />

                    <Stack direction="row" justifyContent="space-between" alignItems="baseline">
                      <Typography variant="h5">TOTAL A PAGAR</Typography>
                      <Typography
                        variant="h2"
                        color="primary.main"
                        sx={{ transition: `color ${DURACION.normal}ms ${CURVA}` }}
                      >
                        {formatearValor(total, 'moneda')}
                      </Typography>
                    </Stack>
                  </Box>
                </Stack>

                <Divider sx={{ my: 2.5 }} />

                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} justifyContent="flex-end">
                  {factura && (
                    <Button
                      variant="outlined"
                      startIcon={<PrintOutlinedIcon />}
                      onClick={() => window.print()}
                    >
                      Reimprimir
                    </Button>
                  )}

                  {factura && factura.estadoPago === 'Pendiente' && (
                    <Button
                      variant="outlined"
                      color="success"
                      startIcon={<PaymentsOutlinedIcon />}
                      onClick={() => mutacionPagar.mutate()}
                      disabled={mutacionPagar.isPending}
                    >
                      Registrar pago
                    </Button>
                  )}

                  {!factura && (
                    <Button
                      variant="contained"
                      size="large"
                      startIcon={
                        mutacionEmitir.isPending ? (
                          <CircularProgress size={16} color="inherit" />
                        ) : (
                          <ReceiptLongIcon />
                        )
                      }
                      onClick={() => setConfirmarEmision(true)}
                      disabled={!puedeFacturar || mutacionEmitir.isPending}
                    >
                      {mutacionEmitir.isPending ? 'Emitiendo…' : 'Emitir factura'}
                    </Button>
                  )}
                </Stack>
              </CardContent>
            </Card>
          </Box>
        </Grow>
      )}

      {/* Confirmación de emisión: la factura no puede anularse desde la interfaz. */}
      <DialogoConfirmacion
        abierto={confirmarEmision}
        titulo="Emitir factura"
        mensaje="Se emitirá el comprobante de la estadía. Una vez emitido, la estadía no puede volver a facturarse."
        detalle={
          <Stack spacing={0.5}>
            <Typography variant="body2" fontWeight={600}>
              {estadia?.clienteNombre}
            </Typography>
            <Typography variant="caption">
              Hospedaje {formatearValor(subtotalHospedaje, 'moneda')} · Consumos{' '}
              {formatearValor(subtotalConsumos, 'moneda')}
              {descuentoAplicado > 0 &&
                ` · Descuento -${formatearValor(descuentoAplicado, 'moneda')}`}
            </Typography>
            <Typography variant="h5" color="primary.main" sx={{ pt: 0.5 }}>
              Total: {formatearValor(total, 'moneda')}
            </Typography>
            <Typography variant="caption">
              {pagarAhora
                ? `Se registrará el pago por ${etiquetaLegible(metodoPago)}.`
                : 'La factura quedará pendiente de pago.'}
            </Typography>
          </Stack>
        }
        etiquetaConfirmar="Emitir factura"
        enProceso={mutacionEmitir.isPending}
        onConfirmar={() => mutacionEmitir.mutate()}
        onCancelar={() => setConfirmarEmision(false)}
      />
    </Stack>
  );
}

/**
 * La factura ya emitida expone la fecha del cargo como `fechaConsumo`, mientras que
 * la vista previa de la cuenta la llama `fecha`. Ambas describen lo mismo, y esta
 * función evita repetir la comprobación en cada renglón de la tabla.
 */
function fechaDeLinea(linea: {
  fechaConsumo?: string | null;
  fecha?: string | null;
}): string | null | undefined {
  return linea.fechaConsumo ?? linea.fecha;
}

function FilaTotal({
  etiqueta,
  valor,
  color,
}: {
  etiqueta: string;
  valor: number;
  color?: string;
}) {
  return (
    <Stack direction="row" justifyContent="space-between" sx={{ py: 0.6 }}>
      <Typography variant="body2" color="text.secondary">
        {etiqueta}
      </Typography>
      <Typography variant="body2" fontWeight={600} color={color}>
        {formatearValor(valor, 'moneda')}
      </Typography>
    </Stack>
  );
}
