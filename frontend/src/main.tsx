import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CssBaseline, ThemeProvider } from '@mui/material';
import { App } from './App';
import { ProveedorAutenticacion } from './contexto/ContextoAutenticacion';
import { ProveedorNotificaciones } from './contexto/ContextoNotificaciones';
import { tema } from './tema/tema';
import './index.css';

const clienteConsultas = new QueryClient({
  defaultOptions: {
    queries: {
      // Los datos operativos cambian con frecuencia, pero no tanto como para
      // recargarlos en cada foco de ventana.
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider theme={tema}>
      <CssBaseline />
      <QueryClientProvider client={clienteConsultas}>
        <BrowserRouter>
          <ProveedorAutenticacion>
            <ProveedorNotificaciones>
              <App />
            </ProveedorNotificaciones>
          </ProveedorAutenticacion>
        </BrowserRouter>
      </QueryClientProvider>
    </ThemeProvider>
  </StrictMode>,
);
