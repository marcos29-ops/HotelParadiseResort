# Paradise Resort — Sistema de Gestión Hotelera

Memoria del proyecto. **Leer completo antes de cualquier cambio.** Actualizar al cerrar cada fase.

---

## 1. Contexto

Sistema empresarial de gestión hotelera para el hotel **Paradise Resort** (Temática 4 del
enunciado). Proyecto del curso Análisis y Diseño de Sistemas II, Universidad Latina de Costa Rica.
Profesor: Lic. Christopher Seas. Autores: Marcos Vargas Borge, Sebastián José Mora Ortiz,
Josué Andrés Rojas Vásquez, Sebastián José Rojas Quirós, Yesica Madriz Chaves.

Debe parecer software comercial, no un proyecto académico. Se evaluará como si lo auditaran
arquitectos senior.

**Fuente de verdad:** los PDF de `docs/` (Enunciado, Etapa 1, Etapa 2; Etapa 3 pendiente de
entrega). Nunca inventar requerimientos, eliminar funcionalidades ni alterar reglas de negocio.
Prioridad ante contradicciones: **Etapa 2 → Etapa 1 → Enunciado**.

> **Etapas ≠ Fases.** *Etapas* = entregables académicos (documentos). *Fases* = plan de
> construcción del software. La Fase 2 implementa lo especificado en las Etapas 1 **y** 2. La
> Fase 4 producirá los manuales, que son entregables de la Etapa 3.

**Idioma:** todo en español — respuestas, documentación, mensajes de UI e identificadores de
código (el diseño UML está en español: `Reserva`, `Habitacion`, `FacturaBuilder`).

---

## 2. Problema y requerimientos

El hotel opera con hojas de cálculo y registros manuales. Consecuencias: sobre-reservas, sin
visibilidad de disponibilidad real, consumos de servicios que no llegan a la factura, reservas
dispersas en múltiples canales (teléfono, correo, redes sociales).

### Requerimientos funcionales (catálogo canónico, Etapa 1)

| ID | Módulo |
|---|---|
| RF01 | Gestión de clientes — registrar, consultar, actualizar, historial |
| RF02 | Gestión de habitaciones — catálogo, tipos, tarifas, estado en tiempo real |
| RF03 | Consulta de disponibilidad por rango de fechas |
| RF04 | Gestión de reservas — crear, confirmar, modificar, cancelar |
| RF05 | Check-in sobre reserva confirmada |
| RF06 | Check-out y preparación para facturación |
| RF07 | Consumos: restaurante, lavandería, transporte, actividades |
| RF08 | Facturación consolidada (hospedaje + consumos, descuentos, pago) |
| RF09 | Reportes: ocupación, ingresos, temporadas |

**Trazabilidad con Etapa 2:** la Etapa 2 usa rangos RF01–RF28 por componente pero **no los
enumera**. Se conserva el catálogo RF01–RF09 (único explícito) con este mapeo — no inventar los 28:
Clientes RF01-03 → RF01 · Habitaciones RF04-07 → RF02+RF03 · Reservas RF08-12 → RF04 ·
Estadías RF13-20 → RF05+RF06+RF07 · Facturación RF21-24 → RF08 · Reportes RF25-28 → RF09.

### No funcionales

RNF01 seguridad (JWT, roles, hash) · **RNF02 respuesta ≤ 2.5 s** · **RNF03 ≥ 50 usuarios
concurrentes con bloqueo optimista a nivel de tupla** · RNF04 escalabilidad · RNF05
mantenibilidad · RNF06 usabilidad · RNF07 integración futura (móvil, OTA).

> RNF03 es vinculante: `rowversion` en todas las entidades + transacciones serializables en la
> verificación de disponibilidad. Ya implementado.

### Reglas de negocio

1. No confirmar reserva si la habitación no está libre en el rango (objetivo central).
2. Check-in solo sobre reserva **confirmada**; la habitación pasa a `Ocupada`.
3. Los consumos se imputan a la **estadía**, no a la reserva.
4. Factura = hospedaje + consumos − descuento (descuento exige justificación).
5. Hospedaje = noches × tarifa del tipo de habitación.
6. Toda reserva nace `Pendiente` y requiere confirmación explícita.
7. Las reservas registran su canal de origen.
8. Solo `Administrador` accede a gestión de usuarios y reportes.

**Moneda:** dólares (`$`). **Roles:** `Administrador` y `Recepcionista` (ambos especializan al
actor Empleado). El **Cliente no inicia sesión** — es entidad de negocio gestionada.

---

## 3. Arquitectura

Arquitectura **en capas con componentes de negocio** (Etapa 1 descartó monolítica por
acoplamiento y microservicios por sobreingeniería), materializada sobre **Clean Architecture**.

```
Presentación (React) → Servicios (API + Facade) → Negocio (Domain + Application) → Persistencia (EF Core)
```

**Regla de dependencias (vinculante):** `API → Application → Domain`; `Persistence` e
`Infrastructure → Application, Domain`; `Domain` no depende de nadie. Todos pueden usar `Shared`.

### Seis componentes de negocio y sus dependencias

| # | Componente | Depende de | Patrones |
|---|---|---|---|
| 1 | Clientes | — | — |
| 2 | Habitaciones y Disponibilidad | — | Strategy, State |
| 3 | Reservas | 1, 2 | State |
| 4 | Estadías y Consumos | 2, 3 | Decorator |
| 5 | Facturación | 2, 4 | Builder |
| 6 | Reportes | 2, 3, 5 | Template Method |

La Presentación **nunca** llama a un componente directo: solo a `IFachadaServiciosHotel`.

### Patrones GoF (obligatorios, definidos en Etapa 2)

| Patrón | Ubicación | Resuelve |
|---|---|---|
| **Facade** | `Application/Fachada/FachadaServiciosHotel` | Entrada única para web, móvil y OTA. Incluye las operaciones compuestas `CrearReservaAsync` y `GenerarFacturaCheckOutAsync` |
| **Strategy** | `Domain/Patrones/Strategy` — `IEstrategiaTarifa` → `TarifaEstandar`, `TarifaTemporadaAlta` (+25%, meses 12/1/2/7). Selector en `Application/Tarifas` | Tarifa variable sin tocar Facturación |
| **State** | `Domain/Patrones/State/{Habitaciones,Reservas}` con fábricas | Transiciones válidas sin condicionales dispersos |
| **Decorator** | `Domain/Patrones/Decorator` — `IComponenteCuenta` ← `CuentaEstadia` + 4 decoradores. `EnsambladorCuenta` arma la cadena | Acumular consumos sin que `Estadia` conozca las combinaciones |
| **Builder** | `Domain/Patrones/Builder/FacturaBuilder` | Ensamblar factura por pasos |
| **Template Method** | `Domain/Patrones/TemplateMethod` — `ReporteBase.Generar()` es `sealed` | Esqueleto común de los 3 reportes |

**Estados** — Habitación: `Disponible`, `Reservada`, `Ocupada`, `EnLimpieza`, `EnMantenimiento`.
Reserva: `Pendiente`, `Confirmada`, `Cancelada`, `Completada`. Estadía: `EnCurso`, `Finalizada`,
`Facturada`.

---

## 4. Stack y estructura

**Backend:** ASP.NET Core 8 · EF Core 8 · SQL Server · JWT · Swagger · AutoMapper 15.1.1 ·
FluentValidation · Serilog · Asp.Versioning.
**Frontend (Fase 3):** React · TypeScript · Vite · Material UI · Axios · React Router ·
React Hook Form · TanStack Query. **Prohibido:** Bootstrap, Tailwind.

```
raíz/ (= HotelParadiseResort)
├── docs/  backend/  frontend/  database/  manuals/
├── README.md  CLAUDE.md  .gitignore  .editorconfig
└── backend/
    ├── global.json (fija SDK 8)  HotelParadiseResort.sln
    └── src/{API,Application,Domain,Infrastructure,Persistence,Shared,Tests}
```

`Tests/` va dentro de `src/` porque así lo define la estructura oficial. No mover.

---

## 5. Entorno (verificado 27/07/2026)

| | |
|---|---|
| .NET | SDK 8.0.423 + runtime 8.0.29 ✅ (hay 9 y 10 instalados; `global.json` fija el 8) |
| Node | v24.4.1 · npm 11.4.2 ✅ |
| BD | **Azure SQL Edge** 15.0.2000.1574 ARM64, contenedor `azure-sql-edge`, `localhost:1433` |
| Docker | 27.5.1 · 6 CPU · 5 GB RAM |

**Decisión de BD (del equipo, 27/07/2026):** desarrollo local sobre **Azure SQL Edge** por tener
imagen ARM64 nativa en Apple Silicon. Motor **objetivo del entregable: SQL Server**. Mismo
proveedor `Microsoft.EntityFrameworkCore.SqlServer`, así que migraciones y script de `database/`
son válidos para SQL Server. Verificadas 10/10 características necesarias (`rowversion`,
`decimal(18,2)`, índices filtrados, CHECK, SERIALIZABLE, window functions, SEQUENCE, FK).
Limitación aceptada: Edge deriva de SQL Server 2019 y Microsoft lo retiró en sept-2025.
**Riesgo a vigilar: no usar sintaxis exclusiva de SQL Server 2022.**

> La contraseña del contenedor está en la variable `SA_PASSWORD` (nombre antiguo, no
> `MSSQL_SA_PASSWORD`).

**Collation obligatoria:** crear `HotelDB` con `Latin1_General_CI_AI` (insensible a mayúsculas
**y acentos**). Sin esto, buscar "Perez" no encuentra "Pérez" — verificado empíricamente. Usar
`nvarchar` en todo texto.

**Nunca:** SQLite, LocalDB, InMemory (incluso en tests — usar Moq).

---

## 6. Estado de avance

| Fase | Alcance | Estado |
|---|---|---|
| 1 | Estructura, CLAUDE.md, README | ✅ Completada |
| 2 | Backend, BD, patrones, API, JWT | ✅ Completada |
| 3 | Frontend React, 7 pantallas | ✅ Completada |
| 4 | Manuales, pruebas UAT, auditoría | ✅ **Completada** |

**El proyecto está terminado.** 0 errores · 0 advertencias · 211 pruebas automatizadas ·
27/27 casos UAT · 87.0 % de cobertura.

### Fase 2 — completada (0 errores, 0 advertencias, 211/211 pruebas, **cobertura 87.0 %**)

129 archivos `.cs`: Shared 7 · Domain 47 · Persistence 23 · Application 26 · Infrastructure 6 ·
API 13 · Tests 7. **54 endpoints REST** (27 GET, 17 POST, 6 PUT, 2 PATCH, 2 DELETE).

- **Shared** — `Resultado`/`Resultado<T>` + `TipoError` (traduce a HTTP), paginación, roles,
  excepciones.
- **Domain** — 11 entidades, 8 enums, **los 6 patrones GoF**, interfaces de repositorio.
- **Persistence** — `HotelDbContext` con auditoría automática, 9 configuraciones Fluent API,
  10 repositorios, `UnidadDeTrabajo` con transacciones **serializables**, migración inicial,
  `InicializadorBaseDatos` (seed).
- **Application** — DTOs, mapeadores estáticos, 8 servicios, `FachadaServiciosHotel` con las
  operaciones compuestas, validadores FluentValidation, `SelectorEstrategiaTarifa`.
- **Infrastructure** — JWT HMAC-SHA256, PBKDF2-SHA256 (210k iteraciones, comparación en tiempo
  constante), `UsuarioActual`, `ProveedorFechaHora`.
- **API** — 8 controladores, `ControladorBase` que traduce `Resultado` → HTTP, middleware global
  de errores (`ProblemDetails`), Swagger con auth, CORS, versionado por URL, Serilog.
- **BD** — 11 tablas, 34 índices, 12 CHECK, 17 FK, **11 columnas `rowversion`** (RNF03),
  15 columnas con collation acento-insensible. Scripts en `database/`.

### Verificación end-to-end realizada contra la BD real

Flujo completo probado: login → disponibilidad → reserva → confirmación → check-in → 3 consumos
→ cuenta → check-out → factura → pago. Comprobado además: 401 sin token, **409 en sobre-reserva**,
403 de recepcionista sobre reportes, 422 en transición de estado inválida, 409 en doble
facturación, búsqueda acento-insensible en 7 variantes, y las dos estrategias de tarifa
(septiembre $255 = wireframe; febrero/diciembre $318.75 con +25 %).

### Defectos detectados y corregidos en la autorrevisión

1. **Hospedaje mal calculado** — se cobraba por los sellos de tiempo reales de check-in/check-out
   (llegó a mostrar 27 noches) en vez de por las **noches contratadas en la reserva**. Corregido
   en `Estadia.CalcularNoches()` y `ServicioEstadias.ConstruirCuenta()`.
2. **Check-in sin validar fechas** — permitía registrar la llegada de una reserva de meses
   después. Ahora exige que el check-in caiga dentro del rango reservado.
3. **Estado instantáneo bloqueaba reservas futuras** — una habitación "En limpieza" hoy no
   aparecía disponible para septiembre. Solo `EnMantenimiento` retira del inventario reservable;
   el estado real se revalida en el check-in.
4. AutoMapper 12/13/14 arrastraban **CVE-2026-32933** (DoS, CVSS 7.5). Se fijó **15.1.1**
   (parcheada), lo que obligó a alinear `Logging.Abstractions` y `Options` a 10.0.0.

### Plan de pruebas (Etapa 2) — cumplido

211 pruebas en `src/Tests`, estables en corridas repetidas. Cobertura por capa: Domain 95.3 % ·
API 94.2 % · Shared 87.8 % · Persistence 86.2 % · Infrastructure 85.4 % · Application 84.0 %.

| Carpeta | Nivel | Qué cubre |
|---|---|---|
| `Dominio/` (65) | Unitarias | Los 6 patrones GoF: transiciones State, tarifas Strategy, cadena Decorator, Builder, Template Method |
| `Aplicacion/` (16) | Unitarias con **Moq** | Reglas de reservas y de usuarios (incluida la salvaguarda del último administrador) |
| `Seguridad/` (11) | Unitarias | PBKDF2, sal aleatoria, hashes mal formados |
| `Api/` (10) | Unitarias | Middleware de errores: cada excepción → su código HTTP; en producción no filtra trazas |
| `Integracion/` (109) | **Integración** | API ↔ BD real de punta a punta: reservas, estadías, facturación, clientes, catálogos, seguridad y reportes |

> Los conteos incluyen cada fila de `[Theory]`, que es como los cuenta el corredor: 102 unitarias
> + 109 de integración = 211. Al modificar pruebas, recontar con `[Fact]` + filas `[InlineData]`;
> contar solo los métodos da un número que no cuadra con la salida de `dotnet test`.

**Cobertura — medirla siempre con `backend/cobertura.runsettings`:**

```bash
dotnet test backend/HotelParadiseResort.sln --settings backend/cobertura.runsettings --collect:"XPlat Code Coverage"
```

Ese archivo excluye `**/Migrations/*.cs` (código generado por EF en tiempo de diseño; solo el
snapshot son ~1830 líneas que ninguna prueba ejecuta). Sin la exclusión la cifra baja al 79 % y
parece incumplir la meta del 80 %. **No excluir `CompilerGeneratedAttribute`**: descartaría las
máquinas de estado de los métodos `async` —el cuerpo real de casi todo el código— y subiría la
cifra al 95 % artificialmente.

**Las pruebas de integración usan una base real dedicada** (`HotelDB_Pruebas`, creada y
eliminada por la propia suite), nunca InMemory. Requieren la contraseña del motor:

```bash
PRUEBAS_SQL_PASSWORD=<contraseña-sa> dotnet test backend/HotelParadiseResort.sln
```

Sin esa variable, las pruebas de integración fallan al conectar; las unitarias siguen corriendo.
Variables opcionales: `PRUEBAS_SQL_SERVIDOR` (por defecto `localhost,1433`) y `PRUEBAS_SQL_USUARIO`
(por defecto `sa`).

> **Al escribir pruebas nuevas:** no dependan del censo global de usuarios ni muten credenciales
> compartidas. Dos pruebas así provocaron un fallo intermitente; se rediseñaron para que cada una
> cree sus propios datos (`CrearUsuarioDesechableAsync`) o se movieron a unitarias con Moq.

### Cómo levantar y probar

`.claude/launch.json` define el servidor `api-backend` (puerto **5170**). Swagger en
`http://localhost:5170/swagger`, salud en `/salud`. Los secretos están en **user-secrets** del
proyecto API (`ConnectionStrings:HotelDB`, `Jwt:Clave`, `Seed:AdminPassword`), nunca en el repo.
Credenciales sembradas: `admin` / `Paradise2026!` y `recepcion01` / `Recepcion2026`.

> `global.json` está en la **raíz** (no en `backend/`) para que fije el SDK 8 se ejecute desde
> donde se ejecute. Sin eso, `dotnet` toma el SDK 10 instalado.

### Fase 3 — completada (0 errores de tipos, build correcto)

25 archivos en `frontend/src`, todo consumiendo la API real. **Sin mock data.**

```
src/
├── api/          clienteHttp.ts (interceptores JWT + traducción de errores) · servicios.ts
├── componentes/  TarjetaKpi (conteo animado) · Estados (vacío/error/esqueleto/chips)
│                 TablaOrdenable (orden en servidor) · DialogoConfirmacion
├── contexto/     ContextoAutenticacion (sesión) · ContextoNotificaciones (snackbars)
├── layout/       LayoutPrincipal (AppBar + menú lateral + migas) · navegacion.ts
├── pantallas/    las 7 pantallas + Reservas (listado)
├── tema/         tema.ts — paleta, sombras, duraciones y colores de estado
├── tipos/        api.ts — espejo exacto de los DTO del backend
└── utilidades/   formato.ts (fechas, moneda, etiquetas) · hooks.ts (búsqueda diferida)
```

**Decisiones tomadas en esta fase:**

- **MUI 7, no 9.** La v9 rompe la API de `Stack` (`direction`/`spacing`/`alignItems` dejan de ser
  props) y los `styleOverrides` de Button/Alert. Se fijó la 7.3.11, estable y documentada.
- **`strict: true` en `tsconfig.app.json`** — la plantilla de Vite no lo traía y MUI lo necesita
  para inferir sus componentes genéricos.
- **`@mui/x-charts`** para el gráfico de reportes, por coherencia con Material UI.
- El menú lateral es **oscuro** (`AZUL_PROFUNDO`) y el área de trabajo clara: enmarca el
  contenido y da el acabado que pidió el usuario. Los wireframes de la Etapa 2 son de baja
  fidelidad y ella misma indica que *«el estilo visual final se definirá durante la
  construcción»*, así que el aspecto se elevó manteniendo estructura y funcionalidad.
- Animaciones centralizadas en el tema (`DURACION`, `CURVA`, `CURVA_ENTRADA`, `SOMBRA`) y
  respeto de `prefers-reduced-motion` en `index.css`.

**Servidores:** `.claude/launch.json` define `api-backend` (5170) y `web-frontend` (5173).
Ojo con la sintaxis: `npm --prefix frontend run dev` (el `--prefix` va **antes** de `run`).

**Verificado en navegador:** login real → panel con KPI y grid de 20 habitaciones → listado de
reservas → nueva reserva con disponibilidad y tarifas del Strategy → check-in/check-out →
reportes con gráfico. Sin errores de consola.

**Persistencia comprobada contra SQL:** se creó un cliente («Begoña Muñoz Céspedes») desde el
formulario de la interfaz y se leyó luego directamente de la tabla `Cliente`: acentos y ñ
correctos, `FechaCreacion` puesta por la auditoría automática del `DbContext`, y entrada de
`HistorialCliente` generada sola. La búsqueda acento-insensible responde igual con «Munoz»,
«Muñoz», «MUNOZ», «Begona», «Begoña» y «cespedes».

**Auditoría de la Parte 3 del prompt maestro — correcciones aplicadas:**

1. **Ordenamiento de tablas** (faltaba): `componentes/TablaOrdenable.tsx` con `useOrden` +
   `CeldaOrdenable`. Reservas ordena por código/entrada/salida/estado; Clientes por
   identificación y nombre. Se resuelve **en el servidor**, así respeta la paginación.
2. **Confirmación de operaciones críticas** (faltaba en check-out y facturar):
   `componentes/DialogoConfirmacion.tsx`, con resumen de la operación antes de confirmar.
   Cancelar reserva y eliminar consumo ya la tenían.
3. **Formato de moneda** — mostraba `USD 318,75`; ahora `$318.75` como el wireframe.
4. **Etiquetas legibles** — `RedesSociales` → «Redes sociales», `TarjetaCredito` → «Tarjeta de
   crédito», etc. (`utilidades/formato.ts`, función `etiquetaLegible`).
5. **Botón deshabilitado ilegible** — el degradado de `containedPrimary` persistía al
   deshabilitar y el texto atenuado no contrastaba. Se anula el degradado en `Mui-disabled`.
6. **Gráfico de reportes ilegible** — la barra ocupaba todo el ancho y se montaba con la
   leyenda. Corregido en dos frentes: en el **frontend** con `categoryGapRatio`, leyenda por
   encima del área de trazado y valores formateados; y en el **backend**, porque
   `ReporteBase.ConstruirSeries` solo emitía las semanas con datos (aparecía «Sem 5» suelto).
   Ahora emite **todas** las semanas del rango, con 0 donde no hay actividad, y las rotula con
   la fecha de inicio («29 jun», «06 jul»).

*No implementado y por qué:* la Parte 3 menciona «Tendencia (cuando aplique)» en las tarjetas
KPI. El backend no expone comparativa con el período anterior y la documentación oficial no la
pide; añadirla exigiría inventar un endpoint fuera de la especificación.

### Fase 4 — completada

**Entregables en `manuals/`:**

| Archivo | Contenido |
|---|---|
| `Manual_Tecnico.md` / `.pdf` (21 págs.) | Arquitectura, tecnologías justificadas, modelo de BD, backend, frontend, **los 6 patrones GoF con su implementación real**, seguridad, pruebas, instalación, despliegue y mantenimiento |
| `Manual_Usuario.md` / `.pdf` (20 págs.) | Para personal sin conocimientos técnicos, con **9 capturas reales** del sistema funcionando |
| `Plan_Pruebas_UAT.md` | 27 casos de aceptación con su resultado y los 11 defectos detectados/corregidos |
| `imagenes/` | 9 capturas generadas con Chrome headless sobre el sistema real |

**Generación de los PDF.** No hay pandoc ni wkhtmltopdf en el equipo. Se convierte
Markdown → HTML con estilos de impresión → PDF con Chrome headless
(`scratchpad/a_pdf.py`). Los `<div align="center">` necesitan `markdown="1"` para que
python-markdown procese su contenido; sin eso la portada sale con los asteriscos crudos.

**Capturas.** `scratchpad/capturas.py` conduce Chrome por el protocolo DevTools: se autentica
contra la API real y recorre las pantallas. Requiere `--remote-allow-origins=*`. Las pantallas
con buscador necesitan datos existentes: la habitación con estadía en curso y una factura ya
emitida (consultar la BD antes de fijar los términos de búsqueda).

**Defecto corregido en esta fase — zona horaria (Mayor).** El sistema rechazaba reservas para el
día en curso: comparaba contra UTC y en Costa Rica (UTC−6), pasadas las 18:00 locales, el día UTC
ya había avanzado. Se añadió `IProveedorFechaHora.FechaOperativa`, expresada en la zona del hotel
(`Hotel:ZonaHoraria`, por defecto `America/Costa_Rica`), y las reglas que comparan días del
calendario la usan. Las marcas de tiempo siguen en UTC.

### Estado final

| Verificación | Resultado |
|---|---|
| Compilación backend | 0 errores, 0 advertencias |
| Tipos frontend | 0 errores |
| Pruebas automatizadas | 211/211 |
| Cobertura | 87.0 % (excluidas las migraciones generadas) |
| Casos UAT | 27/27 |
| Defectos abiertos | Ninguno |

136 archivos `.cs` · 25 archivos `.ts/.tsx` · 54 endpoints REST · 11 tablas.

---

## 7. Reglas de código

**Nunca:** lógica de negocio en controllers · acceso directo a BD desde controllers · código
duplicado · métodos/clases gigantes · consultas N+1 · `TODO` · placeholders · mock/fake data ·
funciones vacías · secretos en código · contraseñas en texto plano · mostrar errores técnicos al
usuario.

**Siempre:** DTOs · validación de entradas · manejo de excepciones · logging de operaciones
críticas (nunca datos sensibles) · paginación · índices · transacciones donde haya concurrencia.

**Convenciones** — C#: `PascalCase` tipos/métodos/propiedades, `_camelCase` campos privados,
`camelCase` parámetros, interfaces con `I`, sufijo `Async`. Namespaces
`HotelParadiseResort.<Capa>.<Módulo>`. React: `PascalCase.tsx` componentes, `useCamelCase` hooks.
BD: tablas `PascalCase` singular, FK `<Entidad>Id`, índices `IX_<Tabla>_<Columnas>`.

**Errores de negocio previsibles → `Resultado`**, no excepciones. Excepciones solo para lo
verdaderamente excepcional.

**Git (GitFlow):** `main` · `develop` · `feature/*` · `hotfix/*`. Commits convencionales
(`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`).

**Pruebas:** unitarias (cobertura mínima 80 %), integración, UI/UAT. Prioridad: reservas, pagos,
usuarios, seguridad. Defectos por severidad: Bloqueante/Mayor/Menor/Trivial.

---

## 8. Pantallas (Fase 3 — wireframes Etapa 2)

Siete con wireframe, más **Habitaciones** (ver abajo). Menú lateral y barra superior comunes.
No inventar ni eliminar pantallas más allá de la excepción documentada.

1. **Login** — usuario, contraseña, recordarme, olvidó contraseña.
2. **Panel principal** — 4 KPI (ocupadas `42/60`, disponibles, reservadas hoy, mantenimiento),
   filtros, "+ Nueva reserva", grid de habitaciones con color por estado + leyenda.
3. **Nueva reserva** — 3 secciones (cliente buscar/registrar · fechas, huéspedes, tipo, canal ·
   servicios opcionales), verificación de disponibilidad con aviso visual, tarifa preliminar.
4. **Check-in / Check-out** — buscador por habitación o cliente, tarjeta fija del huésped,
   pestañas, observaciones, tabla de consumos.
5. **Facturación** — detalle hospedaje (concepto/cantidad/precio/subtotal), consumos, método y
   estado de pago, totales con descuento, Reimprimir / Emitir.
6. **Reportes** — pestañas Ocupación/Ingresos/Temporadas, filtros, KPI, gráfico por semana,
   Exportar PDF.
7. **Clientes** — tabla (ID, nombre, teléfono/correo, nº reservas), buscador, paginación.

**8. Gestión de habitaciones** *(sin wireframe — añadida tras la Fase 4)*. Dos pestañas:
inventario (número, piso, tipo, tarifa, estado, alta/edición y cambio de estado) y tipos con sus
tarifas. **Por qué existe:** RF02 pide «catálogo, tipos, tarifas y estado en tiempo real», y el
backend ya lo implementaba, pero ninguna de las siete pantallas lo exponía. El inicializador solo
siembra el administrador, los tipos y los servicios: **no crea habitaciones**. Una instalación
nueva se quedaba sin nada que reservar y solo podía cargarse por Swagger. No es una pantalla
inventada: cierra un requerimiento del catálogo que estaba implementado pero inaccesible.
Ver y cambiar estado corresponde a todo el personal; crear y editar solo a `Administrador`,
igual que en `ControladorHabitaciones`.

**9. Usuarios** *(sin wireframe — añadida tras la Fase 4, solo `Administrador`)*. Listado
paginado, alta, edición, restablecimiento de contraseña y desactivación. **Por qué existe:** la
regla de negocio 8 reserva la gestión de usuarios al administrador, `ControladorUsuarios` expone
6 endpoints y **ninguna pantalla los consumía**. Las cuentas del personal solo podían crearse por
Swagger, y el aviso del login —«solicite el restablecimiento al administrador»— remitía a una
operación que la interfaz no ofrecía. No se puede desactivar la propia cuenta.

Se conectó además **«Cambiar contraseña»** al menú de la cuenta
(`componentes/DialogoCambiarContrasena`): `apiAutenticacion.cambiarContrasena` existía desde la
Fase 2 y ninguna pantalla lo invocaba — era código muerto.

> Único endpoint sin interfaz: `GET /autenticacion/perfil`. No es un hueco — los datos del usuario
> llegan en la respuesta del inicio de sesión y viven en `ContextoAutenticacion`.

> Los desplegables de MUI dentro de formularios van con `Controller`, no con `register`: MUI monta
> el `Select` sin valor y React Hook Form se lo asigna después, lo que dispara el aviso de React
> «uncontrolled input to be controlled».

> **El inicializador no siembra `recepcion01`.** Solo crea el `admin` (con `Seed:AdminPassword`),
> los tipos de habitación y los servicios adicionales. Una instalación nueva no tiene habitaciones
> ni clientes ni reservas: hay que cargarlas desde las pantallas de Habitaciones y Usuarios.

**UX (Etapa 2):** consistencia · visibilidad del estado · prevención de errores ·
retroalimentación inmediata · minimizar carga de memoria · jerarquía visual.
**Prohibido:** mock data — todo viene de la API.

---

## 9. Flujo de trabajo

Trabajar **por fases**, sin avanzar sin autorización explícita del usuario. Al cerrar cada fase:
verificar que compila y funciona, autorrevisar como revisor independiente, corregir, actualizar
`CLAUDE.md` y `README.md`, resumir y esperar aprobación.

Checklist por tarea: ☑ compila ☑ funciona ☑ no rompe lo existente ☑ sigue la arquitectura
☑ validaciones ☑ manejo de errores ☑ buena UX ☑ documentado.
