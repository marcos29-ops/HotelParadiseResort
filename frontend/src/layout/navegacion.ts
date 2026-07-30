import type { ComponentType } from 'react';
import DashboardOutlinedIcon from '@mui/icons-material/DashboardOutlined';
import EventAvailableOutlinedIcon from '@mui/icons-material/EventAvailableOutlined';
import LoginOutlinedIcon from '@mui/icons-material/LoginOutlined';
import RestaurantOutlinedIcon from '@mui/icons-material/RestaurantOutlined';
import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined';
import InsightsOutlinedIcon from '@mui/icons-material/InsightsOutlined';
import PeopleAltOutlinedIcon from '@mui/icons-material/PeopleAltOutlined';
import type { RolUsuario } from '../tipos/api';

export interface ElementoNavegacion {
  etiqueta: string;
  ruta: string;
  icono: ComponentType;
  /** Roles con acceso. Sin especificar, lo ve todo el personal autenticado. */
  roles?: RolUsuario[];
}

/**
 * Menú lateral, en el mismo orden que los wireframes de la Etapa 2. Es la única
 * fuente del árbol de navegación: de aquí salen también los breadcrumbs y el
 * título de cada pantalla.
 */
export const NAVEGACION: ElementoNavegacion[] = [
  { etiqueta: 'Panel principal', ruta: '/panel', icono: DashboardOutlinedIcon },
  { etiqueta: 'Reservas', ruta: '/reservas', icono: EventAvailableOutlinedIcon },
  { etiqueta: 'Check-in / Check-out', ruta: '/estadias', icono: LoginOutlinedIcon },
  { etiqueta: 'Consumos', ruta: '/consumos', icono: RestaurantOutlinedIcon },
  { etiqueta: 'Facturación', ruta: '/facturacion', icono: ReceiptLongOutlinedIcon },
  {
    etiqueta: 'Reportes',
    ruta: '/reportes',
    icono: InsightsOutlinedIcon,
    roles: ['Administrador'],
  },
  { etiqueta: 'Clientes', ruta: '/clientes', icono: PeopleAltOutlinedIcon },
];

/** Títulos de rutas que no figuran en el menú lateral. */
const TITULOS_ADICIONALES: Record<string, string> = {
  '/reservas/nueva': 'Nueva reserva',
};

export function obtenerTitulo(ruta: string): string {
  const exacto = NAVEGACION.find((e) => e.ruta === ruta);
  if (exacto) return exacto.etiqueta;

  if (TITULOS_ADICIONALES[ruta]) return TITULOS_ADICIONALES[ruta];

  const padre = NAVEGACION.find((e) => ruta.startsWith(`${e.ruta}/`));
  return padre?.etiqueta ?? 'Paradise Resort';
}

/** Migas de pan derivadas de la ruta, para que el usuario sepa siempre dónde está. */
export function construirMigas(ruta: string): { etiqueta: string; ruta?: string }[] {
  const seccion = NAVEGACION.find((e) => ruta === e.ruta || ruta.startsWith(`${e.ruta}/`));

  if (!seccion) return [{ etiqueta: 'Inicio' }];

  if (ruta === seccion.ruta) {
    return [{ etiqueta: seccion.etiqueta }];
  }

  return [
    { etiqueta: seccion.etiqueta, ruta: seccion.ruta },
    { etiqueta: obtenerTitulo(ruta) },
  ];
}
