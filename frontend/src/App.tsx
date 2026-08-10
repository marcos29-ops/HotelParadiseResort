import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import type { ReactElement } from 'react';
import { Box, Button, Stack, Typography } from '@mui/material';
import { useAutenticacion } from './contexto/ContextoAutenticacion';
import { LayoutPrincipal } from './layout/LayoutPrincipal';
import { PantallaLogin } from './pantallas/PantallaLogin';
import { PantallaPanel } from './pantallas/PantallaPanel';
import { PantallaHabitaciones } from './pantallas/PantallaHabitaciones';
import { PantallaUsuarios } from './pantallas/PantallaUsuarios';
import { PantallaReservas } from './pantallas/PantallaReservas';
import { PantallaNuevaReserva } from './pantallas/PantallaNuevaReserva';
import { PantallaEstadias } from './pantallas/PantallaEstadias';
import { PantallaConsumos } from './pantallas/PantallaConsumos';
import { PantallaFacturacion } from './pantallas/PantallaFacturacion';
import { PantallaReportes } from './pantallas/PantallaReportes';
import { PantallaClientes } from './pantallas/PantallaClientes';
import type { RolUsuario } from './tipos/api';

/** Exige sesión activa y, opcionalmente, un rol concreto. */
function RutaProtegida({ children, roles }: { children: ReactElement; roles?: RolUsuario[] }) {
  const { estaAutenticado, usuario } = useAutenticacion();
  const ubicacion = useLocation();

  if (!estaAutenticado) {
    // Se recuerda el destino para volver a él tras autenticarse.
    return <Navigate to="/login" state={{ desde: ubicacion.pathname }} replace />;
  }

  if (roles && usuario && !roles.includes(usuario.rol)) {
    return <SinPermisos />;
  }

  return children;
}

function SinPermisos() {
  return (
    <Stack alignItems="center" spacing={1.5} sx={{ py: 8, textAlign: 'center' }}>
      <Typography variant="h2">Acceso restringido</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 460 }}>
        Esta sección está reservada al personal administrativo. Si necesita consultarla, solicite
        los permisos correspondientes al administrador del sistema.
      </Typography>
      <Button href="/panel" variant="contained" sx={{ mt: 1 }}>
        Volver al panel principal
      </Button>
    </Stack>
  );
}

function NoEncontrado() {
  return (
    <Stack alignItems="center" spacing={1.5} sx={{ py: 8, textAlign: 'center' }}>
      <Typography variant="h1">404</Typography>
      <Typography variant="h4">Página no encontrada</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 420 }}>
        La dirección solicitada no corresponde a ninguna sección del sistema.
      </Typography>
      <Button href="/panel" variant="contained" sx={{ mt: 1 }}>
        Ir al panel principal
      </Button>
    </Stack>
  );
}

export function App() {
  return (
    <Box>
      <Routes>
        <Route path="/login" element={<PantallaLogin />} />

        <Route
          element={
            <RutaProtegida>
              <LayoutPrincipal />
            </RutaProtegida>
          }
        >
          <Route path="/panel" element={<PantallaPanel />} />
          <Route path="/habitaciones" element={<PantallaHabitaciones />} />
          <Route path="/reservas" element={<PantallaReservas />} />
          <Route path="/reservas/nueva" element={<PantallaNuevaReserva />} />
          <Route path="/estadias" element={<PantallaEstadias />} />
          <Route path="/consumos" element={<PantallaConsumos />} />
          <Route path="/facturacion" element={<PantallaFacturacion />} />
          <Route
            path="/reportes"
            element={
              <RutaProtegida roles={['Administrador']}>
                <PantallaReportes />
              </RutaProtegida>
            }
          />
          <Route path="/clientes" element={<PantallaClientes />} />
          <Route
            path="/usuarios"
            element={
              <RutaProtegida roles={['Administrador']}>
                <PantallaUsuarios />
              </RutaProtegida>
            }
          />
          <Route path="*" element={<NoEncontrado />} />
        </Route>

        <Route path="/" element={<Navigate to="/panel" replace />} />
      </Routes>
    </Box>
  );
}
