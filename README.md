<div align="center">

# 🏝️ Paradise Resort — Sistema de Gestión Hotelera

**Plataforma empresarial para la administración integral de reservas, habitaciones, estadías, consumos y facturación**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![Material UI](https://img.shields.io/badge/Material_UI-007FFF?logo=mui&logoColor=white)](https://mui.com/)

*Universidad Latina de Costa Rica · Ingeniería en Sistemas Computacionales*
*Análisis y Diseño de Sistemas II*

</div>

---

## Tabla de contenidos

- [Introducción](#introducción)
- [Estado del proyecto](#estado-del-proyecto)
- [Funcionalidades](#funcionalidades)
- [Tecnologías](#tecnologías)
- [Arquitectura](#arquitectura)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Requisitos previos](#requisitos-previos)
- [Instalación](#instalación)
- [Configuración](#configuración)
- [Variables de entorno](#variables-de-entorno)
- [Base de datos y migraciones](#base-de-datos-y-migraciones)
- [Compilación](#compilación)
- [Ejecución](#ejecución)
- [Usuarios y roles](#usuarios-y-roles)
- [Credenciales de ejemplo](#credenciales-de-ejemplo)
- [Pruebas](#pruebas)
- [Despliegue](#despliegue)
- [Documentación](#documentación)
- [Flujo de trabajo](#flujo-de-trabajo)
- [Licencia](#licencia)
- [Autores](#autores)

---

## Introducción

El hotel **Paradise Resort** administra actualmente sus reservaciones y el control de
habitaciones mediante hojas electrónicas y registros manuales. Esta situación provoca errores
frecuentes en reservas, problemas de facturación y poca visibilidad sobre la disponibilidad real
de habitaciones. Además, los clientes reservan por canales heterogéneos —llamadas telefónicas,
correo electrónico y redes sociales— sin una plataforma centralizada que gestione las
solicitudes, y los consumos de servicios complementarios (restaurante, lavandería, transporte y
actividades recreativas) con frecuencia no se reflejan en la factura final del huésped.

Este sistema centraliza la operación del hotel en una única plataforma que permite:

- **Eliminar las sobre-reservas** mediante verificación transaccional de disponibilidad.
- **Erradicar los registros manuales** con un inventario de habitaciones actualizado en tiempo
  real.
- **Cerrar las fugas de capital** vinculando automáticamente los consumos de la estadía con la
  factura consolidada.
- **Agilizar el check-out** con cálculo automático del total a pagar.
- **Garantizar la confidencialidad** mediante control de acceso estricto por rol.
- **Proteger la inversión tecnológica** con una arquitectura modular preparada para integrarse a
  futuro con aplicaciones móviles y plataformas externas de reservación (OTA).

> El análisis, diseño y especificación completos se encuentran en los documentos oficiales de la
> carpeta [`docs/`](docs/), que constituyen la única fuente de verdad del proyecto.

---

## Estado del proyecto

El desarrollo avanza estrictamente por fases. Ninguna fase se da por terminada sin validación.

| Fase | Alcance | Estado |
|:---:|---|:---:|
| **1** | Configuración del proyecto: estructura del workspace, memoria técnica (`CLAUDE.md`), README | ✅ **Completada** |
| **2** | Arquitectura, modelo relacional, Entity Framework, patrones GoF, API REST, seguridad JWT | ✅ **Completada** |
| **3** | Frontend React, las 7 pantallas del diseño, UX/UI e integración con la API | ✅ **Completada** |
| **4** | Manuales técnico y de usuario, pruebas UAT, auditoría y entrega final | ✅ **Completada** |

**El proyecto está terminado.**

**Backend:** compila sin errores ni advertencias · **211 pruebas en verde con 87,0 % de
cobertura** · **54 endpoints REST** operativos · base de datos con 11 tablas, 34 índices, 17 claves
foráneas y 12 restricciones CHECK · flujo completo verificado de extremo a extremo contra la base
real.

**Frontend:** las 7 pantallas del diseño más la de gestión de habitaciones, verificadas en
navegador contra la API real · 0 errores de tipos · build de producción correcto · sin datos
simulados.

**Documentación:** manual técnico (21 págs.), manual de usuario con capturas reales (20 págs.) y
plan de pruebas (8 págs.), los tres en PDF dentro de [`manuals/`](manuals/) · **27 de 27 casos de
aceptación superados** · 11 defectos detectados durante el desarrollo, todos corregidos.

---

## Funcionalidades

Alcance funcional definido en la Etapa 1 y detallado por componentes en la Etapa 2.

| ID | Módulo | Descripción |
|:---:|---|---|
| **RF01** | Gestión de clientes | Registro, consulta, actualización e historial de huéspedes |
| **RF02** | Gestión de habitaciones | Catálogo, tipos, tarifas y estado en tiempo real |
| **RF03** | Consulta de disponibilidad | Habitaciones libres según fechas de entrada y salida |
| **RF04** | Gestión de reservas | Creación, confirmación, modificación y cancelación |
| **RF05** | Check-in | Registro de llegada asociado a una reserva confirmada |
| **RF06** | Check-out | Cierre de estadía y preparación de la información de facturación |
| **RF07** | Servicios adicionales | Consumos de restaurante, lavandería, transporte y actividades |
| **RF08** | Facturación consolidada | Hospedaje + consumos, descuentos, método y estado de pago |
| **RF09** | Reportes administrativos | Ocupación, ingresos y temporadas de mayor demanda |

### Pantallas del sistema

Siete pantallas definidas por los wireframes oficiales de la Etapa 2:

`Inicio de sesión` · `Panel principal` · `Nueva reserva` · `Check-in / Check-out` ·
`Facturación` · `Reportes administrativos` · `Clientes`

A ellas se suma **`Gestión de habitaciones`**, que no tiene wireframe pero sí requerimiento:
RF02 exige el catálogo con sus tipos, tarifas y estado en tiempo real. El backend ya lo
implementaba y ninguna pantalla lo exponía, de modo que una instalación nueva —que arranca sin
habitaciones— no tenía nada que reservar y solo podía cargarse desde Swagger.

### Requerimientos no funcionales

| ID | Requerimiento | Meta |
|:---:|---|---|
| RNF01 | Seguridad | Autenticación JWT y autorización por rol |
| RNF02 | Rendimiento | Respuesta ≤ **2.5 s** en disponibilidad, registro y check-out |
| RNF03 | Concurrencia | ≥ **50 usuarios simultáneos** con bloqueo optimista a nivel de tupla |
| RNF04 | Escalabilidad | Preparado para nuevos canales digitales |
| RNF05 | Mantenibilidad | Módulos independientes y fácilmente modificables |
| RNF06 | Usabilidad | Interfaces claras para el personal del hotel |
| RNF07 | Integración | Comunicación futura con apps móviles y plataformas OTA |

---

## Tecnologías

### Backend

| Tecnología | Propósito |
|---|---|
| **ASP.NET Core 8** | Framework de la API REST; rendimiento, soporte LTS y ecosistema empresarial maduro |
| **C# 12** | Lenguaje fuertemente tipado que favorece un diseño orientado a objetos limpio |
| **Entity Framework Core** | ORM con migraciones versionadas, Fluent API y concurrencia optimista (RNF03) |
| **SQL Server** | Motor relacional transaccional exigido por la documentación oficial |
| **JWT (Bearer)** | Autenticación sin estado, requisito de RNF01 y de la escalabilidad horizontal |
| **Swagger / OpenAPI** | Documentación viva y contrato explícito de la API |
| **AutoMapper** | Mapeo entre entidades de dominio y DTOs sin acoplar capas |
| **FluentValidation** | Validación declarativa y desacoplada de los modelos de entrada |
| **Serilog** | Logging estructurado de errores, advertencias y operaciones críticas |

### Frontend

| Tecnología | Propósito |
|---|---|
| **React** | Librería de UI basada en componentes reutilizables |
| **TypeScript** | Tipado estático extremo a extremo; contratos alineados con los DTO del backend |
| **Vite** | Build y servidor de desarrollo de arranque inmediato |
| **Material UI** | Sistema de diseño consistente y accesible; base de la identidad visual empresarial |
| **Axios** | Cliente HTTP con interceptores para el token JWT y el manejo global de errores |
| **React Router** | Enrutamiento declarativo con rutas protegidas por rol |
| **React Hook Form** | Formularios de alto rendimiento con validación en tiempo real |
| **TanStack Query** | Caché, sincronización y estados de carga del servidor |

> No se utilizan Bootstrap ni Tailwind. Toda la interfaz se mantiene consistente con Material UI.

---

## Arquitectura

La Etapa 1 evaluó tres alternativas —monolítica, microservicios y capas con componentes— y
seleccionó una **arquitectura en capas con componentes de negocio**, por distribuir el software
en niveles de responsabilidad delimitada, permitir modificar componentes internos sin afectar al
resto y facilitar la integración futura con sistemas externos.

```
┌──────────────────────────────────────────────────────────────┐
│  PRESENTACIÓN     React SPA · Material UI                    │
│                   (a futuro: app móvil · plataformas OTA)    │
└───────────────────────────┬──────────────────────────────────┘
                            │  API REST / HTTPS (JSON + JWT)
┌───────────────────────────▼──────────────────────────────────┐
│  SERVICIOS        FachadaServiciosHotel  «Facade»            │
└───────────────────────────┬──────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────┐
│  NEGOCIO                                                     │
│  ┌──────────┐ ┌──────────────┐ ┌──────────┐                  │
│  │ Clientes │ │ Habitaciones │ │ Reservas │                  │
│  └──────────┘ └──────────────┘ └──────────┘                  │
│  ┌──────────────────────┐ ┌─────────────┐ ┌──────────┐       │
│  │ Estadías y Consumos  │ │ Facturación │ │ Reportes │       │
│  └──────────────────────┘ └─────────────┘ └──────────┘       │
└───────────────────────────┬──────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────┐
│  PERSISTENCIA     EF Core · Repositorios · SQL Server        │
└──────────────────────────────────────────────────────────────┘
```

Estas cuatro capas se materializan sobre **Clean Architecture**, con la regla de dependencias
apuntando siempre hacia el dominio:

```
API  ──▶  Application  ──▶  Domain  ◀──  Persistence
                              ▲
                              └──────────  Infrastructure
```

`Domain` no depende de ninguna otra capa. La inversión de dependencias se aplica mediante
interfaces declaradas en el dominio e implementadas en las capas externas.

### Patrones de diseño

Seis patrones GoF, cada uno resolviendo un problema concreto del diseño (Etapa 2):

| Patrón | Aplicación | Justificación |
|---|---|---|
| **Facade** | `FachadaServiciosHotel` | Punto de entrada único para la web, la futura app móvil y las OTA, en lugar de exponer los seis componentes por separado |
| **Strategy** | `EstrategiaTarifa` | El cálculo de tarifas varía por tipo de habitación y, a futuro, por temporada o promoción, sin modificar Facturación |
| **State** | `EstadoHabitacion`, `EstadoReserva` | Cada estado conoce sus transiciones válidas, evitando condicionales dispersos e inconsistencias con acceso concurrente |
| **Decorator** | `ComponenteCuenta` + consumos | Los consumos se acumulan sobre la cuenta base sin que `Estadia` conozca todas las combinaciones posibles |
| **Builder** | `FacturaBuilder` | Ensambla paso a paso hospedaje, consumos, descuentos y pago en una factura consolidada |
| **Template Method** | `ReporteBase` | Fija el esqueleto común de los reportes; cada subclase implementa solo su cálculo específico |

### Componentes de negocio

| Componente | Responsabilidad | Depende de |
|---|---|---|
| Gestión de Clientes | Ciclo de vida e historial de huéspedes | — |
| Gestión de Habitaciones y Disponibilidad | Catálogo, tipos, tarifas y estado en tiempo real | — |
| Gestión de Reservas | Ciclo completo de la reserva y verificación centralizada de disponibilidad | Habitaciones, Clientes |
| Gestión de Estadías y Consumos | Check-in, check-out y acumulación de consumos | Habitaciones, Reservas |
| Facturación | Consolidación, descuentos y registro de pago | Estadías, Habitaciones |
| Reportes Administrativos | Ocupación, ingresos y temporadas | Reservas, Habitaciones, Facturación |

---

## Estructura del repositorio

```
HotelParadiseResort/
├── docs/                       # Documentación oficial (fuente de verdad)
├── backend/                    # Solución ASP.NET Core 8
│   ├── src/
│   │   ├── API/                # Controllers, middleware, Swagger, autenticación
│   │   ├── Application/        # Casos de uso, fachada, DTOs, validaciones
│   │   ├── Domain/             # Entidades, reglas de negocio, patrones, interfaces
│   │   ├── Infrastructure/     # JWT, hashing, logging, servicios externos
│   │   ├── Persistence/        # DbContext, configuraciones, migraciones, repositorios
│   │   ├── Shared/             # Constantes, resultados, excepciones, utilidades
│   │   └── Tests/              # Pruebas unitarias, de integración y de validación
│   └── HotelParadiseResort.sln
├── frontend/                   # SPA React + TypeScript + Vite
├── database/                   # Scripts SQL de creación y recreación
├── manuals/                    # Manual_Tecnico.pdf · Manual_Usuario.pdf · Plan_Pruebas_UAT.pdf
├── README.md
└── CLAUDE.md                   # Memoria técnica permanente del proyecto
```

---

## Requisitos previos

| Herramienta | Versión mínima | Verificación |
|---|---|---|
| .NET SDK | **8.0** | `dotnet --version` |
| Node.js | 20 LTS | `node --version` |
| npm | 10 | `npm --version` |
| SQL Server | 2019 / 2022 | — |
| EF Core Tools | 8.0 | `dotnet ef --version` |
| Git | 2.40 | `git --version` |

Instalación de las herramientas de Entity Framework:

```bash
dotnet tool install --global dotnet-ef --version 8.*
```

### Motor de base de datos

El **motor objetivo** del sistema es SQL Server. Para el **desarrollo local en macOS con Apple
Silicon** se utiliza *Azure SQL Edge*, que ofrece imagen ARM64 nativa:

```bash
docker run -d --name azure-sql-edge -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=TuPasswordSeguro" -p 1433:1433 mcr.microsoft.com/azure-sql-edge
```

En Windows, Linux o servidores de despliegue se usa SQL Server directamente:

```bash
docker run --platform linux/amd64 -d --name paradise-sqlserver -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=TuPasswordSeguro" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

Ambos comparten el proveedor `Microsoft.EntityFrameworkCore.SqlServer`, por lo que las
migraciones y los scripts de [`database/`](database/) son idénticos y válidos para SQL Server.

> **Collation.** La base `HotelDB` se crea con `Latin1_General_CI_AI`, insensible a mayúsculas y
> **a acentos**. Sin esta configuración, buscar *"Perez"* no encontraría a *"Pérez"* en el módulo
> de clientes. Todos los campos de texto usan `nvarchar`.

> No sustituir por SQLite, LocalDB ni InMemory Database: la documentación oficial lo prohíbe
> expresamente y el modelo depende de características transaccionales del motor.

---

## Instalación

```bash
git clone <url-del-repositorio>
```

```bash
cd <carpeta-del-repositorio>
```

**Backend** — restaurar dependencias:

```bash
dotnet restore backend/HotelParadiseResort.sln
```

**Frontend** — instalar dependencias:

```bash
npm install --prefix frontend
```

---

## Configuración

La configuración del backend se define en `backend/src/API/appsettings.json`. El archivo
versionado contiene **únicamente plantillas**; los valores reales se inyectan por variables de
entorno o mediante *user secrets*.

```json
{
  "ConnectionStrings": {
    "HotelDB": "Server=YOUR_SERVER;Database=HotelDB;User Id=USER;Password=PASSWORD;"
  },
  "Jwt": {
    "Issuer": "ParadiseResort.API",
    "Audience": "ParadiseResort.Client",
    "ExpirationMinutes": 60
  }
}
```

Para el entorno local se recomienda **user secrets**, que mantiene las credenciales fuera del
repositorio:

```bash
dotnet user-secrets set "ConnectionStrings:HotelDB" "Server=localhost,1433;Database=HotelDB;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;" --project backend/src/API
```

> ⛔ **Nunca** colocar credenciales reales en archivos versionados. La Etapa 2 prohíbe
> expresamente dejar contraseñas, llaves de API o cadenas de conexión escritas en el código.

---

## Variables de entorno

### Backend

| Variable | Descripción | Obligatoria |
|---|---|:---:|
| `ConnectionStrings__HotelDB` | Cadena de conexión a SQL Server | ✅ |
| `Jwt__Key` | Clave de firma de los tokens (mínimo 32 caracteres) | ✅ |
| `Jwt__Issuer` | Emisor del token | ✅ |
| `Jwt__Audience` | Audiencia del token | ✅ |
| `Jwt__ExpirationMinutes` | Vigencia del token en minutos | — |
| `Seed__AdminPassword` | Contraseña inicial del administrador (solo primer arranque) | ✅ |
| `ASPNETCORE_ENVIRONMENT` | `Development` · `Staging` · `Production` | — |

### Frontend

| Variable | Descripción | Obligatoria |
|---|---|:---:|
| `VITE_API_BASE_URL` | URL base de la API REST | ✅ |

Ambos proyectos incluyen un archivo `.env.example` con la lista de variables y valores de
plantilla. Los archivos `.env` reales están excluidos del control de versiones.

---

## Base de datos y migraciones

El esquema se gestiona exclusivamente con **migraciones de Entity Framework Core**. No se editan
tablas manualmente.

Crear una migración:

```bash
dotnet ef migrations add NombreDeLaMigracion --project backend/src/Persistence --startup-project backend/src/API
```

Aplicar las migraciones a la base de datos:

```bash
dotnet ef database update --project backend/src/Persistence --startup-project backend/src/API
```

Generar el script SQL idempotente para `database/`:

```bash
dotnet ef migrations script --idempotent --project backend/src/Persistence --startup-project backend/src/API --output database/schema.sql
```

Revertir la última migración:

```bash
dotnet ef migrations remove --project backend/src/Persistence --startup-project backend/src/API
```

La carpeta [`database/`](database/) mantiene el script de creación del esquema y el script de
recreación completa de la base de datos.

---

## Compilación

Compilar la solución del backend en configuración de publicación:

```bash
dotnet build backend/HotelParadiseResort.sln --configuration Release
```

Generar el build de producción del frontend:

```bash
npm run build --prefix frontend
```

---

## Ejecución

Iniciar la API (puerto por defecto `http://localhost:5170`):

```bash
dotnet run --project backend/src/API
```

Iniciar el frontend en modo desarrollo (puerto por defecto `http://localhost:5173`):

```bash
npm run dev --prefix frontend
```

Con la API en ejecución, la documentación interactiva de Swagger queda disponible en
`http://localhost:5170/swagger`.

---

## Usuarios y roles

El sistema define dos roles, conforme al diagrama de casos de uso de la Etapa 1. Ambos heredan
del actor abstracto **Empleado**, que concentra el caso de uso *Iniciar sesión*.

| Rol | Permisos |
|---|---|
| **Recepcionista** | Gestionar reservas · gestionar habitaciones · registrar check-in y check-out · registrar consumos · gestionar clientes · emitir facturas |
| **Administrador** | Todo lo del recepcionista **más** gestionar usuarios y generar reportes administrativos |

> El **Cliente** es una entidad de negocio gestionada por el sistema, no un usuario que inicia
> sesión. Los **sistemas externos** (plataformas OTA) corresponden a una integración prevista por
> la arquitectura pero fuera del alcance actual.

La segregación de permisos aplica el principio ISP documentado en la Etapa 1: la interfaz de
Recepción no depende de los métodos de auditoría financiera ni de reportería macro, que
pertenecen estrictamente a Administración.

---

## Credenciales de ejemplo

El primer arranque crea un **usuario administrador inicial**, imprescindible para poder
autenticarse en un sistema donde todos los endpoints están protegidos.

| Rol | Usuario | Contraseña |
|---|---|---|
| Administrador | `admin` | Definida por la variable `Seed__AdminPassword` |

Los usuarios de recepción se crean desde el módulo de gestión de usuarios, accesible únicamente
con el rol Administrador.

> 🔐 La contraseña **no está escrita en el código ni en el repositorio**: se toma de una variable
> de entorno obligatoria en el primer arranque y se almacena con PBKDF2-HMAC-SHA256 (210 000
> iteraciones y sal aleatoria por credencial). Cambiarla tras el primer inicio de sesión.

En el entorno de desarrollo local los secretos se configuran con **user-secrets**, fuera del
repositorio:

```bash
dotnet user-secrets set "Seed:AdminPassword" "TuContrasenaSegura" --project backend/src/API
```

---

## Pruebas

Plan de pruebas definido en la Etapa 2, organizado en tres niveles:

| Nivel | Cantidad | Alcance | Estado |
|---|:---:|---|:---:|
| **Unitarias** | 102 | Patrones GoF, reglas de negocio con Moq, hashing, middleware de errores | ✅ |
| **Integración** | 109 | API ↔ base de datos real: reservas, estadías, facturación, clientes, seguridad y reportes | ✅ |
| **UI / UAT** | 27 | Flujos basados en los wireframes, sobre el sistema en funcionamiento | ✅ |

Los conteos incluyen cada fila de los casos parametrizados (`[Theory]`), que es como los ejecuta
el corredor: 102 + 109 = 211. Los resultados detallados y los defectos detectados están en
[`manuals/Plan_Pruebas_UAT.pdf`](manuals/Plan_Pruebas_UAT.pdf).

**Cobertura alcanzada: 87,0 %** (meta 80 %). Por capa: Domain 95,3 % · API 94,2 % · Shared 87,8 % ·
Persistence 86,2 % · Infrastructure 85,4 % · Application 84,0 %.

Las pruebas de integración se ejecutan contra una base de datos **real y dedicada**
(`HotelDB_Pruebas`), que la propia suite crea y elimina. No se emplea InMemory: ocultaría
justamente lo que estas pruebas deben verificar —índices únicos, restricciones CHECK, claves
foráneas, concurrencia optimista y *collation*—.

```bash
PRUEBAS_SQL_PASSWORD=<contraseña-del-motor> dotnet test backend/HotelParadiseResort.sln
```

Con reporte de cobertura:

```bash
PRUEBAS_SQL_PASSWORD=<contraseña-del-motor> dotnet test backend/HotelParadiseResort.sln --settings backend/cobertura.runsettings --collect:"XPlat Code Coverage"
```

El archivo `backend/cobertura.runsettings` excluye las migraciones de Entity Framework, que son
código generado por la herramienta en tiempo de diseño y que ninguna prueba ejecuta. Es la única
exclusión aplicada.

Los defectos se registran clasificados por severidad: **Bloqueante · Mayor · Menor · Trivial**.
Una versión se considera lista para desplegarse cuando supera el 100 % de las pruebas críticas de
integración, no presenta fallos graves de seguridad en el análisis estático y cumple los tiempos
de respuesta establecidos en RNF02.

---

## Despliegue

El diagrama de despliegue de la Etapa 2 define la siguiente topología:

| Nodo | Contenido | Comunicación |
|---|---|---|
| **Estación de trabajo** | Navegador web (SPA) — Recepción y Administración | HTTPS |
| **Servidor Web** | Nginx + SPA compilada | HTTPS/REST (JSON) |
| **Servidor de Aplicaciones** | API REST — capas de Servicios y Negocio (fachada + 6 componentes) | TCP/IP (JDBC/ODBC) |
| **Servidor de Base de Datos** | Motor relacional (persistencia) | — |
| **Ambiente de Staging** | Réplica para validación de versiones candidatas | Promoción a producción |
| *Dispositivo móvil · Plataformas OTA* | *Integraciones futuras previstas por la arquitectura* | *Fuera del alcance actual* |

Ningún cambio llega directamente a producción: el pipeline de integración continua despliega cada
versión candidata en **Staging**, donde se valida la comunicación entre módulos, base de datos y
servicios externos antes del lanzamiento final.

### Publicación

```bash
dotnet publish backend/src/API --configuration Release --output ./publish
```

El build del frontend genera los archivos estáticos en `frontend/dist/`, que se sirven desde el
servidor web.

---

## Documentación

| Documento | Ubicación | Contenido |
|---|---|---|
| Documentación oficial | [`docs/`](docs/) | Enunciado, Etapa 1 y Etapa 2 — fuente de verdad |
| Memoria técnica | [`CLAUDE.md`](CLAUDE.md) | Arquitectura, convenciones, reglas y trazabilidad |
| Manual técnico | [`manuals/Manual_Tecnico.pdf`](manuals/Manual_Tecnico.pdf) | Arquitectura, capas, BD, backend, frontend, patrones, despliegue (21 págs.) |
| Manual de usuario | [`manuals/Manual_Usuario.pdf`](manuals/Manual_Usuario.pdf) | Guía operativa para personal sin conocimientos técnicos, con capturas reales (20 págs.) |
| Plan de pruebas | [`manuals/Plan_Pruebas_UAT.pdf`](manuals/Plan_Pruebas_UAT.pdf) | Resultados de los tres niveles de prueba y registro de defectos (8 págs.) |
| API | `http://localhost:5170/swagger` | Contrato interactivo de todos los endpoints |

---

## Flujo de trabajo

El proyecto sigue **GitFlow**, según la estrategia de administración de configuración de la
Etapa 2:

| Rama | Propósito |
|---|---|
| `main` | Código de producción estable |
| `develop` | Integración de todo lo desarrollado |
| `feature/*` | Tareas específicas; se eliminan tras integrarse |
| `hotfix/*` | Correcciones críticas sobre producción |

Cada Pull Request hacia `develop` dispara un flujo automatizado que compila el código, ejecuta
las pruebas unitarias y verifica la calidad mediante análisis estático.

Los mensajes de commit siguen el formato convencional:

```
feat: add reservation module
fix: correct room availability calculation
docs: update technical manual
refactor: improve repository layer
```

---

## Licencia

Proyecto académico desarrollado para el curso **Análisis y Diseño de Sistemas II** de la
Universidad Latina de Costa Rica. Su uso se limita a fines educativos y de evaluación. No se
distribuye bajo una licencia de software libre ni comercial.

---

## Autores

**Grupo de trabajo**

| Integrante |
|---|
| Marcos Vargas Borge |
| Sebastián José Mora Ortiz |
| Josué Andrés Rojas Vásquez |
| Sebastián José Rojas Quirós |
| Yesica Madriz Chaves |

**Docente:** Lic. Christopher Seas
**Curso:** Análisis y Diseño de Sistemas II
**Institución:** Universidad Latina de Costa Rica — Ingeniería en Sistemas Computacionales

---

<div align="center">

*Paradise Resort Hotel Management System*

</div>
