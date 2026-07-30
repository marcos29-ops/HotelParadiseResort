import { useEffect, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { Box, Card, CardContent, Skeleton, Stack, Typography, alpha } from '@mui/material';
import { CURVA, DURACION } from '../tema/tema';

interface Props {
  titulo: string;
  valor: number;
  formato?: 'entero' | 'moneda' | 'porcentaje' | 'decimal';
  sufijo?: string;
  descripcion?: string;
  icono: ReactNode;
  color: string;
  cargando?: boolean;
  /** Retraso de entrada, para que las tarjetas aparezcan escalonadas. */
  retraso?: number;
}

/**
 * Anima un número desde cero hasta su valor final.
 *
 * El conteo es corto y con desaceleración: aporta la sensación de dato "vivo" que
 * pide un panel operativo sin distraer de la lectura. Se omite por completo si el
 * usuario configuró su sistema para reducir el movimiento.
 */
function useConteoAnimado(objetivo: number, activo: boolean) {
  const [valor, setValor] = useState(0);
  const referenciaAnimacion = useRef<number | undefined>(undefined);

  useEffect(() => {
    if (!activo) return;

    const prefiereMenosMovimiento = window.matchMedia(
      '(prefers-reduced-motion: reduce)',
    ).matches;

    if (prefiereMenosMovimiento) {
      setValor(objetivo);
      return;
    }

    const duracion = 700;
    const inicio = performance.now();
    const desde = 0;

    const paso = (ahora: number) => {
      const progreso = Math.min((ahora - inicio) / duracion, 1);
      // Desaceleración cúbica: rápido al principio, suave al final.
      const suavizado = 1 - Math.pow(1 - progreso, 3);

      setValor(desde + (objetivo - desde) * suavizado);

      if (progreso < 1) {
        referenciaAnimacion.current = requestAnimationFrame(paso);
      }
    };

    referenciaAnimacion.current = requestAnimationFrame(paso);

    return () => {
      if (referenciaAnimacion.current) cancelAnimationFrame(referenciaAnimacion.current);
    };
  }, [objetivo, activo]);

  return valor;
}

/**
 * Los montos usan el formato del dólar con símbolo antepuesto ($255.00), tal como
 * aparece en los wireframes de la Etapa 2. Los conteos se separan con el formato
 * local para que las cifras grandes sigan siendo legibles.
 */
const FORMATO_MONEDA = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export function formatearValor(valor: number, formato: Props['formato'] = 'entero'): string {
  switch (formato) {
    case 'moneda':
      return FORMATO_MONEDA.format(valor);
    case 'porcentaje':
      return `${valor.toFixed(1)} %`;
    case 'decimal':
      return valor.toFixed(2);
    default:
      return Math.round(valor).toLocaleString('es-CR');
  }
}

export function TarjetaKpi({
  titulo,
  valor,
  formato = 'entero',
  sufijo,
  descripcion,
  icono,
  color,
  cargando = false,
  retraso = 0,
}: Props) {
  const valorAnimado = useConteoAnimado(valor, !cargando);

  if (cargando) {
    return (
      <Card sx={{ height: '100%' }}>
        <CardContent>
          <Stack direction="row" spacing={2} alignItems="flex-start">
            <Skeleton variant="rounded" width={44} height={44} />
            <Box flex={1}>
              <Skeleton width="55%" height={30} />
              <Skeleton width="80%" height={18} />
            </Box>
          </Stack>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card
      sx={{
        height: '100%',
        position: 'relative',
        overflow: 'hidden',
        animation: `aparecer ${DURACION.pausada}ms ${CURVA} ${retraso}ms both`,
        '@keyframes aparecer': {
          from: { opacity: 0, transform: 'translateY(10px)' },
          to: { opacity: 1, transform: 'translateY(0)' },
        },
        '&:hover': {
          transform: 'translateY(-3px)',
          boxShadow: '0 10px 24px rgba(26,32,39,0.10)',
          borderColor: alpha(color, 0.4),
        },
        // Franja de color que identifica el indicador de un vistazo.
        '&::before': {
          content: '""',
          position: 'absolute',
          insetInlineStart: 0,
          insetBlock: 0,
          width: 3,
          backgroundColor: color,
        },
      }}
    >
      <CardContent sx={{ pl: 2.75 }}>
        <Stack direction="row" spacing={2} alignItems="flex-start">
          <Box
            sx={{
              width: 44,
              height: 44,
              flexShrink: 0,
              borderRadius: 2,
              display: 'grid',
              placeItems: 'center',
              backgroundColor: alpha(color, 0.1),
              color,
              transition: `transform ${DURACION.normal}ms ${CURVA}`,
              '.MuiCard-root:hover &': { transform: 'scale(1.08)' },
            }}
          >
            {icono}
          </Box>

          <Box minWidth={0}>
            <Stack direction="row" alignItems="baseline" spacing={0.75}>
              <Typography variant="h2" component="p" sx={{ lineHeight: 1.15 }}>
                {formatearValor(valorAnimado, formato)}
              </Typography>
              {sufijo && (
                <Typography variant="subtitle1" color="text.secondary" noWrap>
                  {sufijo}
                </Typography>
              )}
            </Stack>

            <Typography variant="subtitle2" sx={{ mt: 0.25 }}>
              {titulo}
            </Typography>

            {descripcion && (
              <Typography variant="caption" display="block" sx={{ mt: 0.5 }}>
                {descripcion}
              </Typography>
            )}
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}
