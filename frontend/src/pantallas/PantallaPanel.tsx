import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Button,
  Card,
  CardContent,
  Divider,
  FormControl,
  Grow,
  InputLabel,
  MenuItem,
  Select,
  Skeleton,
  Stack,
  TextField,
  Tooltip,
  Typography,
  alpha,
} from '@mui/material';
import HotelOutlinedIcon from '@mui/icons-material/HotelOutlined';
import MeetingRoomOutlinedIcon from '@mui/icons-material/MeetingRoomOutlined';
import EventAvailableOutlinedIcon from '@mui/icons-material/EventAvailableOutlined';
import BuildOutlinedIcon from '@mui/icons-material/BuildOutlined';
import AddIcon from '@mui/icons-material/Add';
import { apiHabitaciones, apiReportes } from '../api/servicios';
import { describirError } from '../api/clienteHttp';
import { TarjetaKpi } from '../componentes/TarjetaKpi';
import { ChipEstadoHabitacion, EstadoError, EstadoVacio } from '../componentes/Estados';
import { COLOR_ESTADO_HABITACION, CURVA, DURACION } from '../tema/tema';
import type { EstadoHabitacion, Habitacion } from '../tipos/api';

const hoy = () => new Date().toISOString().slice(0, 10);

/**
 * PANTALLA 2 — Panel principal.
 *
 * Reproduce el wireframe: cuatro indicadores, filtros, acceso a nueva reserva y el
 * grid de habitaciones coloreado por estado. Ataca directamente el problema de
 * falta de visibilidad de la disponibilidad descrito en el enunciado.
 */
export function PantallaPanel() {
  const navegar = useNavigate();

  const [tipoFiltro, setTipoFiltro] = useState<number | 'todos'>('todos');
  const [fechaEntrada, setFechaEntrada] = useState(hoy());
  const [fechaSalida, setFechaSalida] = useState(() => {
    const fecha = new Date();
    fecha.setDate(fecha.getDate() + 2);
    return fecha.toISOString().slice(0, 10);
  });

  const consultaResumen = useQuery({
    queryKey: ['panel', 'resumen'],
    queryFn: apiReportes.panel,
    refetchInterval: 60000,
  });

  const consultaHabitaciones = useQuery({
    queryKey: ['habitaciones'],
    queryFn: apiHabitaciones.listar,
  });

  const consultaTipos = useQuery({
    queryKey: ['tipos-habitacion'],
    queryFn: apiHabitaciones.tipos,
  });

  const resumen = consultaResumen.data;

  const habitacionesFiltradas = useMemo(() => {
    const habitaciones = consultaHabitaciones.data ?? [];
    return tipoFiltro === 'todos'
      ? habitaciones
      : habitaciones.filter((h) => h.tipoHabitacionId === tipoFiltro);
  }, [consultaHabitaciones.data, tipoFiltro]);

  const porPiso = useMemo(() => {
    const mapa = new Map<number, Habitacion[]>();

    for (const habitacion of habitacionesFiltradas) {
      const grupo = mapa.get(habitacion.piso) ?? [];
      grupo.push(habitacion);
      mapa.set(habitacion.piso, grupo);
    }

    return [...mapa.entries()]
      .sort(([a], [b]) => a - b)
      .map(([piso, habitaciones]) => ({
        piso,
        habitaciones: habitaciones.sort((a, b) => a.numero.localeCompare(b.numero)),
      }));
  }, [habitacionesFiltradas]);

  const cargandoResumen = consultaResumen.isPending;

  return (
    <Stack spacing={3}>
      {/* --- Indicadores del wireframe --- */}
      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', lg: 'repeat(4, 1fr)' },
        }}
      >
        <TarjetaKpi
          titulo="Habitaciones ocupadas"
          valor={resumen?.habitacionesOcupadas ?? 0}
          sufijo={resumen ? `/ ${resumen.totalHabitaciones}` : undefined}
          descripcion={resumen ? `${resumen.porcentajeOcupacion.toFixed(1)} % de ocupación` : undefined}
          icono={<HotelOutlinedIcon />}
          color={COLOR_ESTADO_HABITACION.Ocupada.principal}
          cargando={cargandoResumen}
          retraso={0}
        />
        <TarjetaKpi
          titulo="Disponibles"
          valor={resumen?.habitacionesDisponibles ?? 0}
          descripcion="Listas para asignar"
          icono={<MeetingRoomOutlinedIcon />}
          color={COLOR_ESTADO_HABITACION.Disponible.principal}
          cargando={cargandoResumen}
          retraso={70}
        />
        <TarjetaKpi
          titulo="Reservadas hoy"
          valor={resumen?.llegadasHoy ?? 0}
          descripcion={resumen ? `${resumen.checkInsHoy} check-in · ${resumen.checkOutsHoy} check-out` : undefined}
          icono={<EventAvailableOutlinedIcon />}
          color={COLOR_ESTADO_HABITACION.Reservada.principal}
          cargando={cargandoResumen}
          retraso={140}
        />
        <TarjetaKpi
          titulo="En mantenimiento"
          valor={resumen?.habitacionesEnMantenimiento ?? 0}
          descripcion="Fuera de servicio"
          icono={<BuildOutlinedIcon />}
          color={COLOR_ESTADO_HABITACION.EnMantenimiento.principal}
          cargando={cargandoResumen}
          retraso={210}
        />
      </Box>

      {/* --- Filtros y acción principal --- */}
      <Card>
        <CardContent>
          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={2}
            alignItems={{ xs: 'stretch', md: 'flex-end' }}
          >
            <FormControl sx={{ minWidth: 210 }}>
              <InputLabel id="etiqueta-tipo">Tipo de habitación</InputLabel>
              <Select
                labelId="etiqueta-tipo"
                label="Tipo de habitación"
                value={tipoFiltro}
                onChange={(e) =>
                  setTipoFiltro(e.target.value === 'todos' ? 'todos' : Number(e.target.value))
                }
              >
                <MenuItem value="todos">Todos</MenuItem>
                {(consultaTipos.data ?? []).map((tipo) => (
                  <MenuItem key={tipo.id} value={tipo.id}>
                    {tipo.nombre}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <TextField
              label="Fecha entrada"
              type="date"
              value={fechaEntrada}
              onChange={(e) => setFechaEntrada(e.target.value)}
              InputLabelProps={{ shrink: true }}
              sx={{ minWidth: 170 }}
            />

            <TextField
              label="Fecha salida"
              type="date"
              value={fechaSalida}
              onChange={(e) => setFechaSalida(e.target.value)}
              InputLabelProps={{ shrink: true }}
              sx={{ minWidth: 170 }}
            />

            <Box flex={1} />

            <Button
              variant="contained"
              size="large"
              startIcon={<AddIcon />}
              onClick={() =>
                navegar('/reservas/nueva', {
                  state: { fechaEntrada, fechaSalida, tipoHabitacionId: tipoFiltro },
                })
              }
              sx={{ whiteSpace: 'nowrap' }}
            >
              Nueva reserva
            </Button>
          </Stack>
        </CardContent>
      </Card>

      {/* --- Grid de habitaciones por estado --- */}
      <Card>
        <CardContent>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            justifyContent="space-between"
            alignItems={{ xs: 'flex-start', sm: 'center' }}
            spacing={1.5}
            mb={2.5}
          >
            <Box>
              <Typography variant="h4">Estado de habitaciones</Typography>
              <Typography variant="caption">
                {habitacionesFiltradas.length} habitación(es) en el inventario
              </Typography>
            </Box>

            <Leyenda />
          </Stack>

          <Divider sx={{ mb: 2.5 }} />

          {consultaHabitaciones.isError ? (
            <EstadoError
              mensaje={describirError(consultaHabitaciones.error)}
              onReintentar={() => consultaHabitaciones.refetch()}
            />
          ) : consultaHabitaciones.isPending ? (
            <GridEsqueleto />
          ) : porPiso.length === 0 ? (
            <EstadoVacio
              icono={<MeetingRoomOutlinedIcon />}
              titulo="Sin habitaciones registradas"
              descripcion="Todavía no hay habitaciones en el inventario para el filtro seleccionado."
            />
          ) : (
            <Stack spacing={3}>
              {porPiso.map(({ piso, habitaciones }) => (
                <Box key={piso}>
                  <Typography variant="subtitle2" sx={{ mb: 1.25 }}>
                    Piso {piso}
                  </Typography>

                  <Box
                    sx={{
                      display: 'grid',
                      gap: 1.25,
                      gridTemplateColumns: 'repeat(auto-fill, minmax(112px, 1fr))',
                    }}
                  >
                    {habitaciones.map((habitacion, indice) => (
                      <TarjetaHabitacion
                        key={habitacion.id}
                        habitacion={habitacion}
                        retraso={indice * 22}
                        onClick={() => navegar(`/estadias?habitacion=${habitacion.numero}`)}
                      />
                    ))}
                  </Box>
                </Box>
              ))}
            </Stack>
          )}
        </CardContent>
      </Card>
    </Stack>
  );
}

function Leyenda() {
  const estados: EstadoHabitacion[] = [
    'Disponible',
    'Reservada',
    'Ocupada',
    'EnLimpieza',
    'EnMantenimiento',
  ];

  return (
    <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
      {estados.map((estado) => (
        <ChipEstadoHabitacion key={estado} estado={estado} />
      ))}
    </Stack>
  );
}

function TarjetaHabitacion({
  habitacion,
  retraso,
  onClick,
}: {
  habitacion: Habitacion;
  retraso: number;
  onClick: () => void;
}) {
  const colores = COLOR_ESTADO_HABITACION[habitacion.estado];

  return (
    <Grow in timeout={DURACION.pausada} style={{ transitionDelay: `${retraso}ms` }}>
      <Tooltip
        title={`${habitacion.tipoHabitacion} · ${colores.etiqueta}`}
        placement="top"
      >
        <Box
          role="button"
          tabIndex={0}
          onClick={onClick}
          onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && onClick()}
          sx={{
            p: 1.5,
            borderRadius: 2,
            cursor: 'pointer',
            textAlign: 'center',
            backgroundColor: colores.fondo,
            border: `1px solid ${colores.borde}`,
            transition: `transform ${DURACION.rapida}ms ${CURVA}, box-shadow ${DURACION.rapida}ms ${CURVA}`,
            '&:hover': {
              transform: 'translateY(-3px)',
              boxShadow: `0 8px 18px ${alpha(colores.principal, 0.22)}`,
            },
            '&:active': { transform: 'translateY(-1px)' },
          }}
        >
          <Typography variant="h4" sx={{ color: colores.principal, lineHeight: 1.2 }}>
            {habitacion.numero}
          </Typography>
          <Typography variant="caption" sx={{ color: colores.principal, opacity: 0.85 }}>
            {colores.etiqueta}
          </Typography>
        </Box>
      </Tooltip>
    </Grow>
  );
}

function GridEsqueleto() {
  return (
    <Box
      sx={{
        display: 'grid',
        gap: 1.25,
        gridTemplateColumns: 'repeat(auto-fill, minmax(112px, 1fr))',
      }}
    >
      {Array.from({ length: 12 }).map((_, indice) => (
        <Skeleton key={indice} variant="rounded" height={72} />
      ))}
    </Box>
  );
}
