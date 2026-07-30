import { useState } from 'react';
import { TableCell, TableSortLabel } from '@mui/material';
import type { TableCellProps } from '@mui/material';

export interface OrdenTabla {
  campo: string;
  descendente: boolean;
}

/**
 * Estado de ordenamiento de una tabla. Al cambiar de columna se arranca siempre en
 * ascendente; pulsar de nuevo sobre la misma invierte el sentido.
 */
export function useOrden(campoInicial: string, descendenteInicial = false) {
  const [orden, setOrden] = useState<OrdenTabla>({
    campo: campoInicial,
    descendente: descendenteInicial,
  });

  const alternar = (campo: string) =>
    setOrden((actual) =>
      actual.campo === campo
        ? { campo, descendente: !actual.descendente }
        : { campo, descendente: false },
    );

  return { orden, alternar };
}

/**
 * Encabezado de columna ordenable. El ordenamiento se resuelve en el servidor, de
 * modo que respeta la paginación: ordenar no reordena solo la página visible.
 */
export function CeldaOrdenable({
  campo,
  orden,
  onOrdenar,
  children,
  ...propsCelda
}: {
  campo: string;
  orden: OrdenTabla;
  onOrdenar: (campo: string) => void;
  children: React.ReactNode;
} & TableCellProps) {
  const activa = orden.campo === campo;

  return (
    <TableCell {...propsCelda} sortDirection={activa ? (orden.descendente ? 'desc' : 'asc') : false}>
      <TableSortLabel
        active={activa}
        direction={activa && orden.descendente ? 'desc' : 'asc'}
        onClick={() => onOrdenar(campo)}
      >
        {children}
      </TableSortLabel>
    </TableCell>
  );
}
