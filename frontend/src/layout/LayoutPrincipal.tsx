import { useState } from 'react';
import { Link as EnlaceRouter, Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  AppBar,
  Avatar,
  Box,
  Breadcrumbs,
  Divider,
  Drawer,
  Fade,
  IconButton,
  Link,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Stack,
  Toolbar,
  Tooltip,
  Typography,
  alpha,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import MenuOpenIcon from '@mui/icons-material/MenuOpen';
import LogoutIcon from '@mui/icons-material/Logout';
import NavigateNextIcon from '@mui/icons-material/NavigateNext';
import HotelIcon from '@mui/icons-material/Hotel';
import { useAutenticacion } from '../contexto/ContextoAutenticacion';
import { AZUL_PROFUNDO, CURVA, CURVA_ENTRADA, DURACION, SOMBRA } from '../tema/tema';
import { NAVEGACION, construirMigas, obtenerTitulo } from './navegacion';

const ANCHO_MENU = 248;
const ANCHO_MENU_COLAPSADO = 68;

/**
 * Estructura común a todas las pantallas: barra superior, menú lateral colapsable,
 * migas de pan y área de contenido.
 *
 * La Etapa 2 lo pide explícitamente por consistencia: el recepcionista no debería
 * reaprender la navegación al cambiar de módulo.
 */
export function LayoutPrincipal() {
  const tema = useTheme();
  const esEscritorio = useMediaQuery(tema.breakpoints.up('md'));
  const navegar = useNavigate();
  const { pathname } = useLocation();
  const { usuario, cerrarSesion } = useAutenticacion();

  const [menuAbierto, setMenuAbierto] = useState(true);
  const [cajonMovil, setCajonMovil] = useState(false);
  const [anclaPerfil, setAnclaPerfil] = useState<HTMLElement | null>(null);

  const expandido = esEscritorio ? menuAbierto : true;
  const anchoActual = esEscritorio && !menuAbierto ? ANCHO_MENU_COLAPSADO : ANCHO_MENU;

  const migas = construirMigas(pathname);
  const titulo = obtenerTitulo(pathname);

  const visibles = NAVEGACION.filter(
    (elemento) => !elemento.roles || (usuario && elemento.roles.includes(usuario.rol)),
  );

  const contenidoMenu = (
    <>
      <Toolbar
        sx={{
          px: 2,
          gap: 1.25,
          minHeight: 64,
          justifyContent: expandido ? 'flex-start' : 'center',
        }}
      >
        <Box
          sx={{
            width: 36,
            height: 36,
            flexShrink: 0,
            borderRadius: 2,
            display: 'grid',
            placeItems: 'center',
            color: '#FFFFFF',
            background: `linear-gradient(140deg, ${alpha('#FFFFFF', 0.22)}, ${alpha('#FFFFFF', 0.06)})`,
            border: `1px solid ${alpha('#FFFFFF', 0.14)}`,
          }}
        >
          <HotelIcon fontSize="small" />
        </Box>

        <Fade in={expandido} timeout={DURACION.normal}>
          <Box sx={{ minWidth: 0, display: expandido ? 'block' : 'none' }}>
            <Typography
              variant="h6"
              noWrap
              sx={{ lineHeight: 1.2, color: '#FFFFFF', letterSpacing: '0.01em' }}
            >
              Paradise Resort
            </Typography>
            <Typography variant="caption" noWrap sx={{ color: alpha('#FFFFFF', 0.55) }}>
              Gestión hotelera
            </Typography>
          </Box>
        </Fade>
      </Toolbar>

      <Divider sx={{ borderColor: alpha('#FFFFFF', 0.08) }} />

      <List sx={{ px: 1.25, py: 1.75, flex: 1 }}>
        {visibles.map((elemento, indice) => {
          const Icono = elemento.icono;
          const activo = pathname === elemento.ruta || pathname.startsWith(`${elemento.ruta}/`);

          const boton = (
            <ListItemButton
              key={elemento.ruta}
              selected={activo}
              onClick={() => {
                navegar(elemento.ruta);
                if (!esEscritorio) setCajonMovil(false);
              }}
              sx={{
                borderRadius: 2,
                mb: 0.5,
                minHeight: 44,
                px: expandido ? 1.5 : 0,
                justifyContent: expandido ? 'flex-start' : 'center',
                position: 'relative',
                overflow: 'hidden',
                color: alpha('#FFFFFF', 0.72),
                transition: `background-color ${DURACION.rapida}ms ${CURVA}, color ${DURACION.rapida}ms ${CURVA}, padding ${DURACION.normal}ms ${CURVA}`,
                animation: `entrarMenu ${DURACION.lenta}ms ${CURVA_ENTRADA} ${indice * 45}ms both`,
                '@keyframes entrarMenu': {
                  from: { opacity: 0, transform: 'translateX(-14px)' },
                  to: { opacity: 1, transform: 'translateX(0)' },
                },
                '&:hover': {
                  backgroundColor: alpha('#FFFFFF', 0.07),
                  color: '#FFFFFF',
                },
                '&.Mui-selected': {
                  backgroundColor: alpha('#FFFFFF', 0.12),
                  color: '#FFFFFF',
                  '&:hover': { backgroundColor: alpha('#FFFFFF', 0.16) },
                  // Marca lateral luminosa que señala la sección activa.
                  '&::before': {
                    content: '""',
                    position: 'absolute',
                    insetInlineStart: 0,
                    insetBlock: 9,
                    width: 3,
                    borderRadius: 3,
                    backgroundColor: '#6FB3D2',
                    boxShadow: '0 0 10px rgba(111,179,210,0.7)',
                  },
                },
              }}
            >
              <ListItemIcon
                sx={{
                  minWidth: expandido ? 38 : 'auto',
                  color: 'inherit',
                  transition: `transform ${DURACION.normal}ms ${CURVA}`,
                  '.MuiListItemButton-root:hover &': { transform: 'scale(1.1)' },
                }}
              >
                <Icono />
              </ListItemIcon>

              {expandido && (
                <ListItemText
                  primary={elemento.etiqueta}
                  primaryTypographyProps={{
                    fontSize: '0.875rem',
                    fontWeight: activo ? 600 : 500,
                    noWrap: true,
                  }}
                />
              )}
            </ListItemButton>
          );

          return expandido ? (
            boton
          ) : (
            <Tooltip key={elemento.ruta} title={elemento.etiqueta} placement="right">
              <span>{boton}</span>
            </Tooltip>
          );
        })}
      </List>

      {/* Pie discreto con la identificación del sistema. */}
      {expandido && (
        <Box sx={{ p: 2, pt: 0 }}>
          <Divider sx={{ mb: 1.5, borderColor: alpha('#FFFFFF', 0.08) }} />
          <Typography
            variant="caption"
            display="block"
            sx={{ lineHeight: 1.5, color: alpha('#FFFFFF', 0.45) }}
          >
            Paradise Resort · v1.0
          </Typography>
          <Typography variant="caption" display="block" sx={{ color: alpha('#FFFFFF', 0.32) }}>
            Universidad Latina de Costa Rica
          </Typography>
        </Box>
      )}
    </>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', backgroundColor: 'background.default' }}>
      <AppBar
        position="fixed"
        elevation={0}
        sx={{
          width: { md: `calc(100% - ${anchoActual}px)` },
          ml: { md: `${anchoActual}px` },
          backgroundColor: alpha('#FFFFFF', 0.85),
          backdropFilter: 'blur(10px)',
          color: 'text.primary',
          borderBottom: '1px solid',
          borderColor: 'divider',
          boxShadow: SOMBRA.sutil,
          transition: `width ${DURACION.normal}ms ${CURVA}, margin ${DURACION.normal}ms ${CURVA}`,
        }}
      >
        <Toolbar sx={{ gap: 1.5, minHeight: 64 }}>
          <IconButton
            edge="start"
            onClick={() => (esEscritorio ? setMenuAbierto((v) => !v) : setCajonMovil(true))}
            aria-label={menuAbierto ? 'Contraer menú' : 'Expandir menú'}
          >
            {esEscritorio && !menuAbierto ? <MenuIcon /> : <MenuOpenIcon />}
          </IconButton>

          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Typography variant="h4" noWrap sx={{ lineHeight: 1.25 }}>
              {titulo}
            </Typography>

            <Breadcrumbs
              separator={<NavigateNextIcon sx={{ fontSize: 14 }} />}
              sx={{ '& .MuiBreadcrumbs-ol': { flexWrap: 'nowrap' } }}
            >
              <Link
                component={EnlaceRouter}
                to="/panel"
                underline="hover"
                variant="caption"
                color="text.secondary"
              >
                Inicio
              </Link>

              {migas.map((miga) =>
                miga.ruta ? (
                  <Link
                    key={miga.etiqueta}
                    component={EnlaceRouter}
                    to={miga.ruta}
                    underline="hover"
                    variant="caption"
                    color="text.secondary"
                  >
                    {miga.etiqueta}
                  </Link>
                ) : (
                  <Typography key={miga.etiqueta} variant="caption" color="text.primary">
                    {miga.etiqueta}
                  </Typography>
                ),
              )}
            </Breadcrumbs>
          </Box>

          <Stack direction="row" alignItems="center" spacing={1.5}>
            <Box sx={{ textAlign: 'right', display: { xs: 'none', sm: 'block' } }}>
              <Typography variant="subtitle2" color="text.primary" noWrap>
                {usuario?.nombre}
              </Typography>
              <Typography variant="caption" noWrap>
                {usuario?.rol}
              </Typography>
            </Box>

            <Tooltip title="Opciones de la cuenta">
              <IconButton onClick={(e) => setAnclaPerfil(e.currentTarget)} size="small">
                <Avatar
                  sx={{
                    width: 36,
                    height: 36,
                    fontSize: '0.85rem',
                    fontWeight: 600,
                    backgroundColor: 'primary.main',
                  }}
                >
                  {usuario?.nombre.charAt(0).toUpperCase()}
                </Avatar>
              </IconButton>
            </Tooltip>
          </Stack>

          <Menu
            anchorEl={anclaPerfil}
            open={Boolean(anclaPerfil)}
            onClose={() => setAnclaPerfil(null)}
            anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
            transformOrigin={{ vertical: 'top', horizontal: 'right' }}
            slotProps={{ paper: { sx: { minWidth: 210, mt: 0.5 } } }}
          >
            <Box sx={{ px: 2, py: 1.25 }}>
              <Typography variant="subtitle2" color="text.primary">
                {usuario?.nombre}
              </Typography>
              <Typography variant="caption">{usuario?.correo}</Typography>
            </Box>

            <Divider />

            <MenuItem
              onClick={() => {
                setAnclaPerfil(null);
                cerrarSesion();
              }}
              sx={{ mt: 0.5, color: 'error.main' }}
            >
              <ListItemIcon sx={{ color: 'error.main' }}>
                <LogoutIcon fontSize="small" />
              </ListItemIcon>
              Cerrar sesión
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>

      {/* Menú permanente en escritorio; cajón temporal en pantallas pequeñas. */}
      <Drawer
        variant={esEscritorio ? 'permanent' : 'temporary'}
        open={esEscritorio ? true : cajonMovil}
        onClose={() => setCajonMovil(false)}
        ModalProps={{ keepMounted: true }}
        sx={{
          width: anchoActual,
          flexShrink: 0,
          '& .MuiDrawer-paper': {
            width: anchoActual,
            boxSizing: 'border-box',
            border: 'none',
            display: 'flex',
            flexDirection: 'column',
            overflowX: 'hidden',
            // Degradado sutil de arriba abajo: da profundidad al panel sin ruido.
            background: `linear-gradient(180deg, ${AZUL_PROFUNDO} 0%, #0E2634 100%)`,
            transition: `width ${DURACION.normal}ms ${CURVA}`,
          },
        }}
      >
        {contenidoMenu}
      </Drawer>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          width: { md: `calc(100% - ${anchoActual}px)` },
          transition: `width ${DURACION.normal}ms ${CURVA}`,
        }}
      >
        <Toolbar sx={{ minHeight: 64 }} />

        <Box sx={{ p: { xs: 2, md: 3 }, maxWidth: 1500, mx: 'auto' }}>
          {/* La clave por ruta reinicia la transición en cada navegación. */}
          <Box
            key={pathname}
            sx={{
              animation: `entrarPagina ${DURACION.pausada}ms ${CURVA_ENTRADA} both`,
              '@keyframes entrarPagina': {
                from: { opacity: 0, transform: 'translateY(10px)' },
                to: { opacity: 1, transform: 'translateY(0)' },
              },
            }}
          >
            <Outlet />
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
