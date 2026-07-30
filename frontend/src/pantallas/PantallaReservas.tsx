import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Box,
  Button,
  Card,
  CardContent,
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
  LinearProgress,
  MenuItem,
  Select,
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
import SearchIcon from '@mui/icons-material/Search';
import AddIcon from '@mui/icons-material/Add';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import CancelOutlinedIcon from '@mui/icons-material/CancelOutlined';
import LoginOutlinedIcon from '@mui/icons-material/LoginOutlined';
import EventBusyOutlinedIcon from '@mui/icons-material/EventBusyOutlined';
import { apiReservas } from '../api/servicios';
import { describirError } from '../api/clienteHttp';
import { useNotificaciones } from '../contexto/ContextoNotificaciones';
import { ChipEstadoReserva, EstadoError, EstadoVacio, FilasEsqueleto } from '../componentes/Estados';
import { formatearValor } from '../componentes/TarjetaKpi';
import { CeldaOrdenable, useOrden } from '../componentes/TablaOrdenable';
import { useBusquedaDiferida } from '../utilidades/hooks';
import { etiquetaLegible, formatearFecha } from '../utilidades/formato';
import type { Reserva } from '../tipos/api';

const COLUMNAS = 8;

/**
 * Listado de reservas con filtros por estado, búsqueda y paginación en servidor.
 * Desde aquí se confirman y cancelan, y se accede al check-in.
 */
export function PantallaReservas() {
  const navegar = useNavigate();
  const notificar = useNotificaciones();
  const clienteConsultas = useQueryClient();

  const [pagina, setPagina] = useState(0);
  const [tamanoPagina, setTamanoPagina] = useState(10);
  const [estado, setEstado] = useState('');
  const { texto, textoDiferido, setTexto } = useBusquedaDiferida();
  const { orden, alternar } = useOrden('fechaEntrada');

  const [reservaACancelar, setReservaACancelar] = useState<Reserva | null>(null);
  const [motivoCancelacion, setMotivoCancelacion] = useState('');

  const consulta = useQuery({
    queryKey: ['reservas', pagina, tamanoPagina, estado, textoDiferido, orden],
    queryFn: () =>
      apiReservas.listar({
        pagina: pagina + 1,
        tamanoPagina,
        busqueda: textoDiferido || undefined,
        estado: estado || undefined,
        ordenarPor: orden.campo,
        descendente: orden.descendente,
      }),
  });

  const invalidar = () => {
    clienteConsultas.invalidateQueries({ queryKey: ['reservas'] });
    clienteConsultas.invalidateQueries({ queryKey: ['habitaciones'] });
    clienteConsultas.invalidateQueries({ queryKey: ['panel'] });
  };

  const mutacionConfirmar = useMutation({
    mutationFn: (id: number) => apiReservas.confirmar(id),
    onSuccess: (reserva) => {
      invalidar();
      notificar.exito(`Reserva ${reserva.codigo} confirmada.`);
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const mutacionCancelar = useMutation({
    mutationFn: ({ id, motivo }: { id: number; motivo: string }) => apiReservas.cancelar(id, motivo),
    onSuccess: (reserva) => {
      invalidar();
      setReservaACancelar(null);
      setMotivoCancelacion('');
      notificar.exito(`Reserva ${reserva.codigo} cancelada.`);
    },
    onError: (error) => notificar.error(describirError(error)),
  });

  const datos = consulta.data;

  return (
    <Stack spacing={2.5}>
      <Card>
        <CardContent>
          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={2}
            alignItems={{ xs: 'stretch', md: 'center' }}
          >
            <TextField
              placeholder="Buscar por código, cliente o habitación"
              value={texto}
              onChange={(e) => {
                setTexto(e.target.value);
                setPagina(0);
              }}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon fontSize="small" color="action" />
                  </InputAdornment>
                ),
              }}
              sx={{ flex: 1, minWidth: 260 }}
            />

            <FormControl sx={{ minWidth: 180 }}>
              <InputLabel id="filtro-estado">Estado</InputLabel>
              <Select
                labelId="filtro-estado"
                label="Estado"
                value={estado}
                onChange={(e) => {
                  setEstado(e.target.value);
                  setPagina(0);
                }}
              >
                <MenuItem value="">Todos</MenuItem>
                <MenuItem value="Pendiente">Pendiente</MenuItem>
                <MenuItem value="Confirmada">Confirmada</MenuItem>
                <MenuItem value="Cancelada">Cancelada</MenuItem>
                <MenuItem value="Completada">Completada</MenuItem>
              </Select>
            </FormControl>

            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => navegar('/reservas/nueva')}
              sx={{ whiteSpace: 'nowrap' }}
            >
              Nueva reserva
            </Button>
          </Stack>
        </CardContent>

        <Box sx={{ height: 3 }}>
          {consulta.isFetching && !consulta.isPending && <LinearProgress />}
        </Box>

        <Divider />

        {consulta.isError ? (
          <Box p={3}>
            <EstadoError mensaje={describirError(consulta.error)} onReintentar={() => consulta.refetch()} />
          </Box>
        ) : (
          <>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <CeldaOrdenable campo="codigo" orden={orden} onOrdenar={alternar}>
                      Código
                    </CeldaOrdenable>
                    <TableCell>Cliente</TableCell>
                    <TableCell>Habitación</TableCell>
                    <CeldaOrdenable campo="fechaEntrada" orden={orden} onOrdenar={alternar}>
                      Entrada
                    </CeldaOrdenable>
                    <CeldaOrdenable campo="fechaSalida" orden={orden} onOrdenar={alternar}>
                      Salida
                    </CeldaOrdenable>
                    <TableCell align="center">Noches</TableCell>
                    <TableCell align="right">Monto</TableCell>
                    <CeldaOrdenable campo="estado" orden={orden} onOrdenar={alternar} align="center">
                      Estado
                    </CeldaOrdenable>
                    <TableCell align="right">Acciones</TableCell>
                  </TableRow>
                </TableHead>

                <TableBody>
                  {consulta.isPending ? (
                    <FilasEsqueleto columnas={COLUMNAS + 1} filas={tamanoPagina > 5 ? 5 : tamanoPagina} />
                  ) : datos && datos.elementos.length > 0 ? (
                    datos.elementos.map((reserva) => (
                      <TableRow
                        key={reserva.id}
                        hover
                        sx={{ '&:hover .acciones': { opacity: 1 } }}
                      >
                        <TableCell>
                          <Typography variant="body2" fontWeight={600}>
                            {reserva.codigo}
                          </Typography>
                          <Typography variant="caption">{etiquetaLegible(reserva.canalOrigen)}</Typography>
                        </TableCell>

                        <TableCell>
                          <Typography variant="body2">{reserva.clienteNombre}</Typography>
                          <Typography variant="caption">{reserva.clienteIdentificacion}</Typography>
                        </TableCell>

                        <TableCell>
                          <Typography variant="body2">{reserva.habitacionNumero}</Typography>
                          <Typography variant="caption">{reserva.tipoHabitacion}</Typography>
                        </TableCell>

                        <TableCell>{formatearFecha(reserva.fechaEntrada)}</TableCell>
                        <TableCell>{formatearFecha(reserva.fechaSalida)}</TableCell>
                        <TableCell align="center">{reserva.noches}</TableCell>

                        <TableCell align="right">
                          <Typography variant="body2" fontWeight={600}>
                            {formatearValor(reserva.montoEstimado, 'moneda')}
                          </Typography>
                        </TableCell>

                        <TableCell align="center">
                          <ChipEstadoReserva estado={reserva.estado} />
                        </TableCell>

                        <TableCell align="right">
                          <Stack
                            className="acciones"
                            direction="row"
                            spacing={0.5}
                            justifyContent="flex-end"
                            sx={{ opacity: { xs: 1, md: 0.35 }, transition: 'opacity 160ms' }}
                          >
                            {reserva.estado === 'Pendiente' && (
                              <Tooltip title="Confirmar reserva">
                                <span>
                                  <IconButton
                                    size="small"
                                    color="success"
                                    disabled={mutacionConfirmar.isPending}
                                    onClick={() => mutacionConfirmar.mutate(reserva.id)}
                                  >
                                    <CheckCircleOutlineIcon fontSize="small" />
                                  </IconButton>
                                </span>
                              </Tooltip>
                            )}

                            {reserva.permiteCheckIn && !reserva.tieneEstadia && (
                              <Tooltip title="Registrar check-in">
                                <IconButton
                                  size="small"
                                  color="primary"
                                  onClick={() => navegar(`/estadias?reserva=${reserva.id}`)}
                                >
                                  <LoginOutlinedIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            )}

                            {(reserva.estado === 'Pendiente' || reserva.estado === 'Confirmada') &&
                              !reserva.tieneEstadia && (
                                <Tooltip title="Cancelar reserva">
                                  <IconButton
                                    size="small"
                                    color="error"
                                    onClick={() => setReservaACancelar(reserva)}
                                  >
                                    <CancelOutlinedIcon fontSize="small" />
                                  </IconButton>
                                </Tooltip>
                              )}
                          </Stack>
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={COLUMNAS + 1} sx={{ border: 0 }}>
                        <EstadoVacio
                          icono={<EventBusyOutlinedIcon />}
                          titulo="Sin reservas"
                          descripcion={
                            textoDiferido || estado
                              ? 'Ninguna reserva coincide con los filtros aplicados.'
                              : 'Todavía no hay reservas registradas en el sistema.'
                          }
                          accion={
                            <Button
                              variant="contained"
                              startIcon={<AddIcon />}
                              onClick={() => navegar('/reservas/nueva')}
                            >
                              Crear la primera reserva
                            </Button>
                          }
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

      {/* Confirmación de cancelación: operación crítica, exige motivo */}
      <Dialog
        open={Boolean(reservaACancelar)}
        onClose={() => setReservaACancelar(null)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Cancelar reserva {reservaACancelar?.codigo}</DialogTitle>

        <DialogContent>
          <DialogContentText sx={{ mb: 2 }}>
            Se liberará la habitación {reservaACancelar?.habitacionNumero} para las fechas
            reservadas. Esta acción no puede deshacerse.
          </DialogContentText>

          <TextField
            label="Motivo de la cancelación"
            fullWidth
            multiline
            rows={3}
            autoFocus
            value={motivoCancelacion}
            onChange={(e) => setMotivoCancelacion(e.target.value)}
            helperText="Queda registrado para la auditoría del hotel."
          />
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button color="inherit" onClick={() => setReservaACancelar(null)}>
            Volver
          </Button>
          <Button
            variant="contained"
            color="error"
            disabled={!motivoCancelacion.trim() || mutacionCancelar.isPending}
            onClick={() =>
              reservaACancelar &&
              mutacionCancelar.mutate({
                id: reservaACancelar.id,
                motivo: motivoCancelacion.trim(),
              })
            }
          >
            {mutacionCancelar.isPending ? 'Cancelando…' : 'Cancelar reserva'}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
