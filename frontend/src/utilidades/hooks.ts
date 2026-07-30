import { useEffect, useState } from 'react';

/**
 * Difiere el término de búsqueda para no lanzar una petición por cada tecla.
 * El retardo mantiene la interfaz fluida sin saturar la API (RNF02).
 */
export function useBusquedaDiferida(retardoMs = 350) {
  const [texto, setTexto] = useState('');
  const [textoDiferido, setTextoDiferido] = useState('');

  useEffect(() => {
    const temporizador = setTimeout(() => setTextoDiferido(texto.trim()), retardoMs);
    return () => clearTimeout(temporizador);
  }, [texto, retardoMs]);

  return { texto, textoDiferido, setTexto };
}
