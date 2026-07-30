import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Button,
  Card,
  CardContent,
  Divider,
  Fade,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Skeleton,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material';
import { BarChart } from '@mui/x-charts/BarChart';
import type { AxisConfig, ChartsXAxisProps } from '@mui/x-charts';
import type { MakeOptional } from '@mui/x-charts/models/helpers';
import FilterAltOutlinedIcon from '@mui/icons-material/FilterAltOutlined';
import PictureAsPdfOutlinedIcon from '@mui/icons-material/PictureAsPdfOutlined';
import InsightsOutlinedIcon from '@mui/icons-material/InsightsOutlined';
import { apiHabitaciones, apiReportes } from '../api/servicios';
import { describirError } from '../api/clienteHttp';
import { useNotificaciones } from '../contexto/ContextoNotificaciones';
import { EstadoError, EstadoVacio } from '../componentes/Estados';
import { formatearValor } from '../componentes/TarjetaKpi';
import { fechaDesplazadaIso, fechaHoyIso, formatearFecha } from '../utilidades/formato';
import { CURVA, DURACION, tema } from '../tema/tema';
import type { Indicador, Reporte } from '../tipos/api';

type TipoReporte = 'ocupacion' | 'ingresos' | 'temporadas';

const PESTANAS: { valor: TipoReporte; etiqueta: string }[] = [
  { valor: 'ocupacion', etiqueta: 'Ocupación' },
  { valor: 'ingresos', etiqueta: 'Ingresos' },
  { valor: 'temporadas', etiqueta: 'Temporadas' },
];

/**
 * PANTALLA 6 — Reportes administrativos.
 *
 * Pestañas de ocupación, ingresos y temporadas con filtros por período, tarjetas
 * de indicadores y gráfico comparativo, según el wireframe. Solo accesible al rol
 * Administrador.
 */
export function PantallaReportes() {
  const notificar = useNotificaciones();

  const [pestana, setPestana] = useState<TipoReporte>('ocupacion');
  const [desde, setDesde] = useState(fechaDesplazadaIso(-30));
  const [hasta, setHasta] = useState(fechaHoyIso());
  const [tipoHabitacionId, setTipoHabitacionId] = useState<number | ''>('');

  // Los filtros solo se aplican al pulsar el botón: evita recargar el reporte
  // mientras el usuario todavía está ajustando las fechas.
  const [filtros, setFiltros] = useState<{
    desde: string;
    hasta: string;
    tipoHabitacionId: number | '';
  }>({ desde, hasta, tipoHabitacionId });

  const consultaTipos = useQuery({
    queryKey: ['tipos-habitacion'],
    queryFn: apiHabitaciones.tipos,
  });

  const consulta = useQuery({
    queryKey: ['reporte', pestana, filtros],
    queryFn: () => {
      const tipo = filtros.tipoHabitacionId === '' ? null : filtros.tipoHabitacionId;

      switch (pestana) {
        case 'ingresos':
          return apiReportes.ingresos(filtros.desde, filtros.hasta, tipo);
        case 'temporadas':
          return apiReportes.temporadas(filtros.desde, filtros.hasta, tipo);
        default:
          return apiReportes.ocupacion(filtros.desde, filtros.hasta, tipo);
      }
    },
  });

  const reporte = consulta.data;

  return (
    <Stack spacing={2.5}>
      <Card>
        <Tabs
          value={pestana}
          onChange={(_, valor) => setPestana(valor)}
          variant="fullWidth"
          sx={{ borderBottom: '1px solid', borderColor: 'divider' }}
        >
          {PESTANAS.map((p) => (
            <Tab key={p.valor} value={p.valor} label={p.etiqueta} />
          ))}
        </Tabs>

        <CardContent>
          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={2}
            alignItems={{ xs: 'stretch', md: 'center' }}
          >
            <TextField
              label="Desde"
              type="date"
              value={desde}
              onChange={(e) => setDesde(e.target.value)}
              InputLabelProps={{ shrink: true }}
              sx={{ minWidth: 170 }}
            />

            <TextField
              label="Hasta"
              type="date"
              value={hasta}
              onChange={(e) => setHasta(e.target.value)}
              InputLabelProps={{ shrink: true }}
              sx={{ minWidth: 170 }}
            />

            <FormControl sx={{ minWidth: 220 }}>
              <InputLabel id="tipo-reporte">Tipo de habitación</InputLabel>
              <Select
                labelId="tipo-reporte"
                label="Tipo de habitación"
                value={tipoHabitacionId}
                onChange={(e) => {
                  // MUI tipa el valor según el primer MenuItem; se normaliza a la
                  // unión que realmente maneja el filtro.
                  const valor = e.target.value as number | '';
                  setTipoHabitacionId(valor === '' ? '' : Number(valor));
                }}
              >
                <MenuItem value="">Todos</MenuItem>
                {(consultaTipos.data ?? []).map((tipo) => (
                  <MenuItem key={tipo.id} value={tipo.id}>
                    {tipo.nombre}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <Box flex={1} />

            <Button
              variant="contained"
              startIcon={<FilterAltOutlinedIcon />}
              onClick={() => {
                if (hasta < desde) {
                  notificar.advertencia('La fecha final no puede ser anterior a la inicial.');
                  return;
                }
                setFiltros({ desde, hasta, tipoHabitacionId });
              }}
              sx={{ whiteSpace: 'nowrap' }}
            >
              Aplicar filtros
            </Button>
          </Stack>
        </CardContent>
      </Card>

      {consulta.isError ? (
        <EstadoError mensaje={describirError(consulta.error)} onReintentar={() => consulta.refetch()} />
      ) : consulta.isPending ? (
        <EsqueletoReporte />
      ) : reporte ? (
        <Fade in key={`${pestana}-${JSON.stringify(filtros)}`} timeout={DURACION.pausada}>
          <Box>
            {/* Indicadores */}
            <Box
              sx={{
                display: 'grid',
                gap: 2,
                mb: 2.5,
                gridTemplateColumns: {
                  xs: '1fr',
                  sm: 'repeat(2, 1fr)',
                  lg: `repeat(${Math.min(reporte.indicadores.length, 4)}, 1fr)`,
                },
              }}
            >
              {reporte.indicadores.map((indicador, indice) => (
                <TarjetaIndicador key={indicador.nombre} indicador={indicador} retraso={indice * 60} />
              ))}
            </Box>

            {/* Gráfico comparativo */}
            <Card>
              <CardContent>
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  justifyContent="space-between"
                  alignItems={{ xs: 'flex-start', sm: 'center' }}
                  spacing={1}
                  mb={1}
                >
                  <Box>
                    <Typography variant="h4">{tituloGrafico(pestana)}</Typography>
                    <Typography variant="caption">
                      {formatearFecha(reporte.fechaDesde)} al {formatearFecha(reporte.fechaHasta)}
                    </Typography>
                  </Box>

                  <Button
                    variant="outlined"
                    startIcon={<PictureAsPdfOutlinedIcon />}
                    onClick={() => window.print()}
                  >
                    Exportar PDF
                  </Button>
                </Stack>

                <Divider sx={{ mb: 2 }} />

                {reporte.series.length === 0 ? (
                  <EstadoVacio
                    icono={<InsightsOutlinedIcon />}
                    titulo="Sin datos en el período"
                    descripcion="No hay información registrada para el rango de fechas seleccionado."
                  />
                ) : (
                  <Grafico reporte={reporte} tipo={pestana} />
                )}
              </CardContent>
            </Card>
          </Box>
        </Fade>
      ) : null}
    </Stack>
  );
}

function tituloGrafico(tipo: TipoReporte): string {
  switch (tipo) {
    case 'ingresos':
      return 'Ingresos por semana (hospedaje vs. servicios)';
    case 'temporadas':
      return 'Demanda por mes';
    default:
      return 'Noches ocupadas por semana';
  }
}

function Grafico({ reporte, tipo }: { reporte: Reporte; tipo: TipoReporte }) {
  const etiquetas = reporte.series.map((s) => s.etiqueta);
  const esMoneda = tipo === 'ingresos';

  /**
   * Eje de categorías. `categoryGapRatio` y `barGapRatio` solo existen en la escala
   * `band`, pero el tipo del array admite todas las escalas y el compilador no las
   * estrecha; se declara aparte con el tipo concreto para conservar la comprobación.
   */
  const ejeCategorias: MakeOptional<AxisConfig<'band', string, ChartsXAxisProps>, 'id'> = {
    scaleType: 'band',
    data: etiquetas,
    // Separación amplia entre categorías: con pocos períodos evita que una sola
    // barra se estire a todo el ancho del panel.
    categoryGapRatio: 0.62,
    barGapRatio: 0.15,
  };

  const formatearEje = (valor: number | null) =>
    valor === null ? '' : esMoneda ? `$${valor.toLocaleString('en-US')}` : String(valor);

  const formatearValorSerie = (valor: number | null) =>
    valor === null ? '' : formatearValor(valor, esMoneda ? 'moneda' : 'entero');

  const series =
    tipo === 'ingresos'
      ? [
          {
            data: reporte.series.map((s) => s.valor),
            label: 'Hospedaje',
            color: tema.palette.primary.main,
            valueFormatter: formatearValorSerie,
          },
          {
            data: reporte.series.map((s) => s.valorSecundario),
            label: 'Servicios adicionales',
            color: '#7FB3CE',
            valueFormatter: formatearValorSerie,
          },
        ]
      : [
          {
            data: reporte.series.map((s) => s.valor),
            label: tipo === 'temporadas' ? 'Reservas' : 'Noches ocupadas',
            color: tema.palette.primary.main,
            valueFormatter: formatearValorSerie,
          },
        ];

  return (
    <Box sx={{ width: '100%' }}>
      <BarChart
        xAxis={[ejeCategorias]}
        yAxis={[{ valueFormatter: formatearEje }]}
        series={series}
        height={330}
        // El margen superior deja la leyenda por encima del área de trazado en lugar
        // de superponerla a las barras; el izquierdo reserva sitio a las etiquetas
        // del eje, que con importes en dólares son anchas.
        margin={{ top: 46, right: 16, bottom: 36, left: esMoneda ? 76 : 46 }}
        slotProps={{
          legend: {
            direction: 'row',
            position: { vertical: 'top', horizontal: 'right' },
            itemMarkWidth: 11,
            itemMarkHeight: 11,
            markGap: 6,
            itemGap: 18,
          },
        }}
        borderRadius={8}
        grid={{ horizontal: true }}
        sx={{
          '& .MuiChartsAxis-tickLabel': { fontSize: 12, fill: '#5A6774' },
          '& .MuiChartsAxis-line, & .MuiChartsAxis-tick': { stroke: '#D8DEE4' },
          '& .MuiChartsGrid-line': { stroke: '#EDF0F3' },
          '& .MuiChartsLegend-series text': { fontSize: '12px !important' },
        }}
      />
    </Box>
  );
}

function TarjetaIndicador({ indicador, retraso }: { indicador: Indicador; retraso: number }) {
  return (
    <Card
      sx={{
        height: '100%',
        animation: `subir ${DURACION.pausada}ms ${CURVA} ${retraso}ms both`,
        '@keyframes subir': {
          from: { opacity: 0, transform: 'translateY(12px)' },
          to: { opacity: 1, transform: 'translateY(0)' },
        },
        '&:hover': { boxShadow: '0 8px 20px rgba(26,32,39,0.08)' },
      }}
    >
      <CardContent>
        <Typography variant="h2" color="primary.main" sx={{ lineHeight: 1.2 }}>
          {formatearValor(indicador.valor, indicador.formato)}
        </Typography>

        <Typography variant="subtitle2" sx={{ mt: 0.5 }}>
          {indicador.nombre}
        </Typography>

        {indicador.descripcion && (
          <Typography variant="caption" display="block" sx={{ mt: 0.5 }}>
            {indicador.descripcion}
          </Typography>
        )}
      </CardContent>
    </Card>
  );
}

function EsqueletoReporte() {
  return (
    <Stack spacing={2.5}>
      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', lg: 'repeat(4, 1fr)' },
        }}
      >
        {Array.from({ length: 4 }).map((_, indice) => (
          <Skeleton key={indice} variant="rounded" height={116} />
        ))}
      </Box>
      <Skeleton variant="rounded" height={400} />
    </Stack>
  );
}
