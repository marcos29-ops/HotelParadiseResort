import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import {
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
import PersonAddAltIcon from '@mui/icons-material/PersonAddAlt';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import HistoryOutlinedIcon from '@mui/icons-material/HistoryOutlined';
import PeopleOutlineIcon from '@mui/icons-material/PeopleOutline';
import { apiClientes } from '../api/servicios';
import { describirError } from '../api/clienteHttp';
import { useNotificaciones } from '../contexto/ContextoNotificaciones';
import { EstadoError, EstadoVacio, FilasEsqueleto } from '../componentes/Estados';
import { CeldaOrdenable, useOrden } from '../componentes/TablaOrdenable';
import { useBusquedaDiferida } from '../utilidades/hooks';
import { formatearFechaHora } from '../utilidades/formato';
import type { Cliente } from '../tipos/api';

interface FormularioCliente {
  identificacion: string;
  nombre: string;
  apellidos: string;
  telefono: string;
  correo: string;
  nacionalidad: string;
}

const COLUMNAS = 5;

/**
 * PANTALLA 7 — Clientes.
 *
 * Tabla con las columnas del wireframe (ID, nombre, teléfono/correo y número de
 * reservas), buscador insensible a acentos y paginación en servidor.
 */
export function PantallaClientes() {
  const notificar = useNotificaciones();
  const clienteConsultas = useQueryClient();

  const [pagina, setPagina] = useState(0);
  const [tamanoPagina, setTamanoPagina] = useState(10);
  const { texto, textoDiferido, setTexto } = useBusquedaDiferida();
  const { orden, alternar } = useOrden('apellidos');

  const [dialogoAbierto, setDialogoAbierto] = useState(false);
  const [clienteEditando, setClienteEditando] = useState<Cliente | null>(null);
  const [clienteHistorial, setClienteHistorial] = useState<Cliente | null>(null);

  const consulta = useQuery({
    queryKey: ['clientes', pagina, tamanoPagina, textoDiferido, orden],
    queryFn: () =>
      apiClientes.listar({
        pagina: pagina + 1,
        tamanoPagina,
        busqueda: textoDiferido || undefined,
        ordenarPor: orden.campo,
        descendente: orden.descendente,
      }),
  });

  const consultaHistorial = useQuery({
    queryKey: ['historial-cliente', clienteHistorial?.id],
    queryFn: () => apiClientes.historial(clienteHistorial!.id),
    enabled: Boolean(clienteHistorial),
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormularioCliente>();

  const abrirDialogo = (cliente: Cliente | null) => {
    setClienteEditando(cliente);
    reset({
      identificacion: cliente?.identificacion ?? '',
      nombre: cliente?.nombre ?? '',
      apellidos: cliente?.apellidos ?? '',
      telefono: cliente?.telefono ?? '',
      correo: cliente?.correo ?? '',
      nacionalidad: cliente?.nacionalidad ?? '',
    });
    setDialogoAbierto(true);
  };

  const mutacionGuardar = useMutation({
    mutationFn: (datos: FormularioCliente) => {
      const comunes = {
        nombre: datos.nombre.trim(),
        apellidos: datos.apellidos.trim(),
        telefono: datos.telefono.trim() || null,
        correo: datos.correo.trim() || null,
        nacionalidad: datos.nacionalidad.trim() || null,
        fechaNacimiento: null,
      };

      return clienteEditando
        ? apiClientes.actualizar(clienteEditando.id, { ...comunes, activo: true })
        : apiClientes.registrar({ ...comunes, identificacion: datos.identificacion.trim() });
    },
    onSuccess: (cliente) => {
      clienteConsultas.invalidateQueries({ queryKey: ['clientes'] });
      setDialogoAbierto(false);
      notificar.exito(
        clienteEditando
          ? `Datos de ${cliente.nombreCompleto} actualizados.`
          : `Cliente ${cliente.nombreCompleto} registrado.`,
      );
    },
    onError: (error) => notificar.error(describirError(error)),
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
              <Typography variant="h4">Clientes registrados</Typography>
              <Typography variant="caption">
                {datos ? `${datos.totalRegistros} cliente(s) en el sistema` : 'Cargando…'}
              </Typography>
            </Box>

            <TextField
              placeholder="Buscar cliente por nombre o ID"
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
              sx={{ minWidth: 280 }}
            />

            <Button
              variant="contained"
              startIcon={<PersonAddAltIcon />}
              onClick={() => abrirDialogo(null)}
              sx={{ whiteSpace: 'nowrap' }}
            >
              Nuevo cliente
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
                    <CeldaOrdenable campo="identificacion" orden={orden} onOrdenar={alternar}>
                      ID
                    </CeldaOrdenable>
                    <CeldaOrdenable campo="apellidos" orden={orden} onOrdenar={alternar}>
                      Nombre
                    </CeldaOrdenable>
                    <TableCell>Teléfono / Correo</TableCell>
                    <TableCell align="center">Reservas</TableCell>
                    <TableCell align="right">Acciones</TableCell>
                  </TableRow>
                </TableHead>

                <TableBody>
                  {consulta.isPending ? (
                    <FilasEsqueleto columnas={COLUMNAS} filas={5} />
                  ) : datos && datos.elementos.length > 0 ? (
                    datos.elementos.map((cliente) => (
                      <TableRow key={cliente.id} hover sx={{ '&:hover .acciones': { opacity: 1 } }}>
                        <TableCell>
                          <Typography variant="body2" fontWeight={600}>
                            {cliente.identificacion}
                          </Typography>
                        </TableCell>

                        <TableCell>
                          <Typography variant="body2">{cliente.nombreCompleto}</Typography>
                          {cliente.nacionalidad && (
                            <Typography variant="caption">{cliente.nacionalidad}</Typography>
                          )}
                        </TableCell>

                        <TableCell>
                          <Typography variant="body2">{cliente.telefono ?? '—'}</Typography>
                          <Typography variant="caption">{cliente.correo ?? ''}</Typography>
                        </TableCell>

                        <TableCell align="center">
                          <Chip
                            size="small"
                            label={cliente.cantidadReservas}
                            color={cliente.cantidadReservas > 0 ? 'primary' : 'default'}
                            variant={cliente.cantidadReservas > 0 ? 'filled' : 'outlined'}
                          />
                        </TableCell>

                        <TableCell align="right">
                          <Stack
                            className="acciones"
                            direction="row"
                            spacing={0.5}
                            justifyContent="flex-end"
                            sx={{ opacity: { xs: 1, md: 0.35 }, transition: 'opacity 160ms' }}
                          >
                            <Tooltip title="Ver historial">
                              <IconButton size="small" onClick={() => setClienteHistorial(cliente)}>
                                <HistoryOutlinedIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>

                            <Tooltip title="Editar datos">
                              <IconButton
                                size="small"
                                color="primary"
                                onClick={() => abrirDialogo(cliente)}
                              >
                                <EditOutlinedIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          </Stack>
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={COLUMNAS} sx={{ border: 0 }}>
                        <EstadoVacio
                          icono={<PeopleOutlineIcon />}
                          titulo="Sin clientes"
                          descripcion={
                            textoDiferido
                              ? `Ningún cliente coincide con «${textoDiferido}».`
                              : 'Todavía no hay huéspedes registrados en el sistema.'
                          }
                          accion={
                            <Button
                              variant="contained"
                              startIcon={<PersonAddAltIcon />}
                              onClick={() => abrirDialogo(null)}
                            >
                              Registrar el primer cliente
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

      {/* Alta y edición de clientes */}
      <Dialog
        open={dialogoAbierto}
        onClose={() => setDialogoAbierto(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>{clienteEditando ? 'Editar cliente' : 'Nuevo cliente'}</DialogTitle>

        <Box component="form" onSubmit={handleSubmit((d) => mutacionGuardar.mutate(d))} noValidate>
          <DialogContent dividers>
            <Stack spacing={2.25}>
              <TextField
                label="Identificación"
                fullWidth
                autoFocus={!clienteEditando}
                disabled={Boolean(clienteEditando)}
                error={Boolean(errors.identificacion)}
                helperText={
                  errors.identificacion?.message ??
                  (clienteEditando ? 'La identificación no puede modificarse.' : undefined)
                }
                {...register('identificacion', { required: 'La identificación es obligatoria.' })}
              />

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField
                  label="Nombre"
                  fullWidth
                  error={Boolean(errors.nombre)}
                  helperText={errors.nombre?.message}
                  {...register('nombre', { required: 'El nombre es obligatorio.' })}
                />
                <TextField
                  label="Apellidos"
                  fullWidth
                  error={Boolean(errors.apellidos)}
                  helperText={errors.apellidos?.message}
                  {...register('apellidos', { required: 'Los apellidos son obligatorios.' })}
                />
              </Stack>

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField label="Teléfono" fullWidth {...register('telefono')} />
                <TextField
                  label="Correo electrónico"
                  type="email"
                  fullWidth
                  error={Boolean(errors.correo)}
                  helperText={errors.correo?.message}
                  {...register('correo', {
                    pattern: {
                      value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                      message: 'El correo no tiene un formato válido.',
                    },
                  })}
                />
              </Stack>

              <TextField label="Nacionalidad" fullWidth {...register('nacionalidad')} />
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
              {clienteEditando ? 'Guardar cambios' : 'Registrar cliente'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* Historial de cambios del cliente */}
      <Dialog
        open={Boolean(clienteHistorial)}
        onClose={() => setClienteHistorial(null)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          Historial · {clienteHistorial?.nombreCompleto}
          <Typography variant="caption" display="block">
            {clienteHistorial?.identificacion}
          </Typography>
        </DialogTitle>

        <DialogContent dividers>
          {consultaHistorial.isPending ? (
            <Stack alignItems="center" py={3}>
              <CircularProgress size={26} />
            </Stack>
          ) : (consultaHistorial.data ?? []).length === 0 ? (
            <Typography variant="body2" color="text.secondary" py={2}>
              Sin movimientos registrados.
            </Typography>
          ) : (
            <Stack spacing={1.5}>
              {(consultaHistorial.data ?? []).map((entrada) => (
                <Box key={entrada.id}>
                  <Stack direction="row" justifyContent="space-between" alignItems="baseline">
                    <Chip size="small" label={entrada.accion} variant="outlined" />
                    <Typography variant="caption">
                      {formatearFechaHora(entrada.fechaRegistro)}
                    </Typography>
                  </Stack>
                  <Typography variant="body2" sx={{ mt: 0.75 }}>
                    {entrada.detalle}
                  </Typography>
                  <Typography variant="caption">Por {entrada.usuario}</Typography>
                  <Divider sx={{ mt: 1.5 }} />
                </Box>
              ))}
            </Stack>
          )}
        </DialogContent>

        <DialogActions sx={{ px: 3, py: 2 }}>
          <Button onClick={() => setClienteHistorial(null)}>Cerrar</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
