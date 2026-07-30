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
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Divider,
  FormControl,
  IconButton,
  InputAdornment,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
  alpha,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import AddIcon from '@mui/icons-material/Add';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import RestaurantOutlinedIcon from '@mui/icons-material/RestaurantOutlined';
import { apiEstadias } from '../api/servicios';
import { describirError } from '../api/clienteHttp';
import { useNotificaciones } from '../contexto/ContextoNotificaciones';
import { EstadoVacio } from '../componentes/Estados';
import { formatearValor } from '../componentes/TarjetaKpi';
import { etiquetaLegible, formatearFechaHora } from '../utilidades/formato';
import { DURACION } from '../tema/tema';
import type { Consumo, Estadia } from '../tipos/api';

/**
 * PANTALLA — Consumos.
 *
 * Registro de los servicios adicionales que el enunciado señala como fuente de
 * fugas: restaurante, lavandería, transporte y actividades. Los cargos se imputan
 * a la estadía, que es el vínculo que evita que queden fuera de la factura.
 */
export function PantallaConsumos() {
  const notificar = useNotificaciones();
  const clienteConsultas = useQueryClient();

  const [termino, setTermino] = useState('');
  const [estadia, setEstadia] = useState<Estadia | null>(null);
  const [buscando, setBuscando] = useState(false);
  const [sinResultado, setSinResultado] = useState(false);
  const [consumoAEliminar, setConsumoAEliminar] = useState<Consumo | null>(null);

  const [servicioId, setServicioId] = useState<number | ''>('');
  const [descripcion, setDescripcion] = useState('');
  const [cantidad, setCantidad] = useState(1);
  const [precio, setPrecio] = useState<string>('');

  const consultaServicios = useQuery({
    queryKey: ['servicios-adicionales'],
    queryFn: apiEstadias.servicios,
  });

  const servicios = (consultaServicios.data ?? []).filter((s) => s.activo);

  const buscar = async () => {
    const valor = termino.trim();
    if (!valor) return;

    setBuscando(true);
    setSinResultado(false);

    try {
      const encontrada = await apiEstadias.buscarPorHabitacion(valor);
      setEstadia(encontrada);
    } catch {
      setEstadia(null);
      setSinResultado(true);
    } finally {
      setBuscando(false);
    }
  };

  const refrescarEstadia = async (id: number) => {
    const actualizada = await apiEstadias.obtener(id);
    setEstadia(actualizada);
    clienteConsultas.invalidateQueries({ queryKey: ['cuenta', id] });
  };

  const mutacionRegistrar = useMutation({
    mutationFn: () =>
      apiEstadias.registrarConsumo(estadia!.id, {
        servicioAdicionalId: Number(servicioId),
        descripcion: descripcion.trim(),
        cantidad,
        precioUnitario: precio === '' ? null : Number(precio),
      }),
    onSuccess: async (consumo) => {
      await refrescarEstadia(estadia!.id);
      setDescripcion('');
      setCantidad(1);
      setPrecio('');
      notificar.exito(
        `Consumo registrado: ${consumo.servicio} · ${formatearValor(consumo.monto, 'moneda')}`,
      );
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const mutacionEliminar = useMutation({
    mutationFn: (consumo: Consumo) => apiEstadias.eliminarConsumo(estadia!.id, consumo.id),
    onSuccess: async () => {
      await refrescarEstadia(estadia!.id);
      setConsumoAEliminar(null);
      notificar.exito('Consumo eliminado de la cuenta.');
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const servicioSeleccionado = servicios.find((s) => s.id === servicioId);
  const puedeRegistrar =
    estadia?.estado === 'EnCurso' && servicioId !== '' && descripcion.trim().length > 0;

  return (
    <Stack spacing={2.5}>
      <Card>
        <CardContent>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              label="Buscar estadía por número de habitación"
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

          {sinResultado && (
            <Alert severity="warning" sx={{ mt: 2 }} onClose={() => setSinResultado(false)}>
              Esa habitación no tiene una estadía en curso. Solo pueden cargarse consumos a
              huéspedes hospedados.
            </Alert>
          )}
        </CardContent>
      </Card>

      {!estadia ? (
        <Card>
          <CardContent>
            <EstadoVacio
              icono={<RestaurantOutlinedIcon />}
              titulo="Busque una estadía"
              descripcion="Indique el número de habitación del huésped para registrar sus consumos de restaurante, lavandería, transporte o actividades."
            />
          </CardContent>
        </Card>
      ) : (
        <>
          {/* Cabecera del huésped */}
          <Card>
            <CardContent>
              <Stack
                direction={{ xs: 'column', md: 'row' }}
                justifyContent="space-between"
                spacing={2}
              >
                <Box>
                  <Typography variant="h3">{estadia.clienteNombre}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Habitación {estadia.habitacionNumero} · {estadia.tipoHabitacion} · reserva{' '}
                    {estadia.reservaCodigo}
                  </Typography>
                </Box>

                <Box
                  sx={{
                    px: 2.5,
                    py: 1.5,
                    borderRadius: 2,
                    textAlign: { xs: 'left', md: 'right' },
                    backgroundColor: (t) => alpha(t.palette.primary.main, 0.05),
                    border: '1px solid',
                    borderColor: (t) => alpha(t.palette.primary.main, 0.16),
                  }}
                >
                  <Typography variant="caption">Total de consumos</Typography>
                  <Typography variant="h3" color="primary.main">
                    {formatearValor(estadia.totalConsumos, 'moneda')}
                  </Typography>
                </Box>
              </Stack>
            </CardContent>
          </Card>

          {estadia.estado !== 'EnCurso' && (
            <Alert severity="info">
              La estadía ya fue cerrada, por lo que no admite consumos nuevos.
            </Alert>
          )}

          {/* Registro de consumo */}
          {estadia.estado === 'EnCurso' && (
            <Card>
              <CardContent>
                <Typography variant="h4" gutterBottom>
                  Registrar consumo
                </Typography>

                <Divider sx={{ mb: 2.5 }} />

                <Stack spacing={2}>
                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                    <FormControl fullWidth>
                      <InputLabel id="servicio">Servicio</InputLabel>
                      <Select
                        labelId="servicio"
                        label="Servicio"
                        value={servicioId}
                        onChange={(e) => {
                          const id = Number(e.target.value);
                          setServicioId(id);

                          // El precio del catálogo se propone como valor inicial y
                          // puede ajustarse si el cargo real difiere.
                          const servicio = servicios.find((s) => s.id === id);
                          if (servicio && servicio.precioBase > 0) {
                            setPrecio(String(servicio.precioBase));
                          }
                        }}
                      >
                        {servicios.map((servicio) => (
                          <MenuItem key={servicio.id} value={servicio.id}>
                            {servicio.nombre}
                            {servicio.precioBase > 0 &&
                              ` · ${formatearValor(servicio.precioBase, 'moneda')}`}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>

                    <TextField
                      label="Descripción"
                      fullWidth
                      value={descripcion}
                      onChange={(e) => setDescripcion(e.target.value)}
                      placeholder="Cena - mesa 4"
                    />
                  </Stack>

                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems="flex-start">
                    <TextField
                      label="Cantidad"
                      type="number"
                      value={cantidad}
                      onChange={(e) => setCantidad(Math.max(1, Number(e.target.value)))}
                      inputProps={{ min: 1, max: 999 }}
                      sx={{ width: { xs: '100%', md: 140 } }}
                    />

                    <TextField
                      label="Precio unitario"
                      type="number"
                      value={precio}
                      onChange={(e) => setPrecio(e.target.value)}
                      inputProps={{ min: 0, step: 0.01 }}
                      InputProps={{
                        startAdornment: <InputAdornment position="start">$</InputAdornment>,
                      }}
                      helperText={
                        servicioSeleccionado
                          ? `Precio de catálogo: ${formatearValor(servicioSeleccionado.precioBase, 'moneda')}`
                          : 'Se toma el del catálogo si se deja vacío.'
                      }
                      sx={{ width: { xs: '100%', md: 220 } }}
                    />

                    <Box
                      sx={{
                        flex: 1,
                        py: 1,
                        textAlign: { xs: 'left', md: 'right' },
                        transition: `opacity ${DURACION.normal}ms`,
                        opacity: precio && cantidad ? 1 : 0.4,
                      }}
                    >
                      <Typography variant="caption" display="block">
                        Monto del cargo
                      </Typography>
                      <Typography variant="h4">
                        {formatearValor((Number(precio) || 0) * cantidad, 'moneda')}
                      </Typography>
                    </Box>

                    <Button
                      variant="contained"
                      size="large"
                      onClick={() => mutacionRegistrar.mutate()}
                      disabled={!puedeRegistrar || mutacionRegistrar.isPending}
                      startIcon={
                        mutacionRegistrar.isPending ? (
                          <CircularProgress size={16} color="inherit" />
                        ) : (
                          <AddIcon />
                        )
                      }
                      sx={{ whiteSpace: 'nowrap', mt: { md: 0.5 } }}
                    >
                      Registrar
                    </Button>
                  </Stack>
                </Stack>
              </CardContent>
            </Card>
          )}

          {/* Consumos acumulados */}
          <Card>
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
                <Typography variant="h4">Consumos de la estadía</Typography>
                <Chip size="small" label={`${estadia.consumos.length} registro(s)`} variant="outlined" />
              </Stack>

              <Divider sx={{ mb: 1 }} />

              {estadia.consumos.length === 0 ? (
                <EstadoVacio
                  icono={<RestaurantOutlinedIcon />}
                  titulo="Sin consumos registrados"
                  descripcion="Los cargos que registre aparecerán aquí y se incluirán automáticamente en la factura final."
                />
              ) : (
                <TableContainer>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Servicio</TableCell>
                        <TableCell>Descripción</TableCell>
                        <TableCell>Fecha</TableCell>
                        <TableCell align="center">Cant.</TableCell>
                        <TableCell align="right">P. unitario</TableCell>
                        <TableCell align="right">Monto</TableCell>
                        <TableCell align="right" />
                      </TableRow>
                    </TableHead>

                    <TableBody>
                      {estadia.consumos.map((consumo) => (
                        <TableRow key={consumo.id} hover>
                          <TableCell>
                            <Typography variant="body2" fontWeight={600}>
                              {etiquetaLegible(consumo.servicio)}
                            </Typography>
                          </TableCell>
                          <TableCell>{consumo.descripcion}</TableCell>
                          <TableCell>{formatearFechaHora(consumo.fechaConsumo)}</TableCell>
                          <TableCell align="center">{consumo.cantidad}</TableCell>
                          <TableCell align="right">
                            {formatearValor(consumo.precioUnitario, 'moneda')}
                          </TableCell>
                          <TableCell align="right">
                            <Typography variant="body2" fontWeight={600}>
                              {formatearValor(consumo.monto, 'moneda')}
                            </Typography>
                          </TableCell>
                          <TableCell align="right">
                            {estadia.estado === 'EnCurso' && (
                              <Tooltip title="Eliminar consumo">
                                <IconButton
                                  size="small"
                                  color="error"
                                  onClick={() => setConsumoAEliminar(consumo)}
                                >
                                  <DeleteOutlineIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            )}
                          </TableCell>
                        </TableRow>
                      ))}

                      <TableRow>
                        <TableCell colSpan={5} align="right" sx={{ border: 0 }}>
                          <Typography variant="subtitle2">Total de consumos</Typography>
                        </TableCell>
                        <TableCell align="right" sx={{ border: 0 }}>
                          <Typography variant="h5" color="primary.main">
                            {formatearValor(estadia.totalConsumos, 'moneda')}
                          </Typography>
                        </TableCell>
                        <TableCell sx={{ border: 0 }} />
                      </TableRow>
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
            </CardContent>
          </Card>
        </>
      )}

      <Dialog open={Boolean(consumoAEliminar)} onClose={() => setConsumoAEliminar(null)}>
        <DialogTitle>Eliminar consumo</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Se eliminará «{consumoAEliminar?.descripcion}» por{' '}
            {formatearValor(consumoAEliminar?.monto ?? 0, 'moneda')} de la cuenta del huésped.
          </DialogContentText>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button color="inherit" onClick={() => setConsumoAEliminar(null)}>
            Volver
          </Button>
          <Button
            variant="contained"
            color="error"
            disabled={mutacionEliminar.isPending}
            onClick={() => consumoAEliminar && mutacionEliminar.mutate(consumoAEliminar)}
          >
            Eliminar
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
