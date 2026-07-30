using HotelParadiseResort.Application.Servicios.Interfaces;

namespace HotelParadiseResort.Application.Fachada;

/// <summary>
/// PATRÓN FACADE — Punto de entrada único de la capa de Servicios.
///
/// Según el diagrama de componentes, la capa de Presentación no invoca directamente a
/// los seis componentes de negocio: consume esta fachada. Así, la interfaz web actual
/// y las integraciones previstas —app móvil y plataformas OTA— dependen de un solo
/// contrato en lugar de seis, lo que responde a RNF05 (mantenibilidad) y RNF07
/// (integración).
/// </summary>
public interface IFachadaServiciosHotel
{
    /// <summary>Autenticación del personal (RNF01).</summary>
    IServicioAutenticacion Autenticacion { get; }

    /// <summary>Administración de usuarios del sistema.</summary>
    IServicioUsuarios Usuarios { get; }

    /// <summary>COMPONENTE 1 — Gestión de Clientes (RF01).</summary>
    IServicioClientes Clientes { get; }

    /// <summary>COMPONENTE 2 — Gestión de Habitaciones y Disponibilidad (RF02, RF03).</summary>
    IServicioHabitaciones Habitaciones { get; }

    /// <summary>COMPONENTE 3 — Gestión de Reservas (RF04).</summary>
    IServicioReservas Reservas { get; }

    /// <summary>COMPONENTE 4 — Gestión de Estadías y Consumos (RF05, RF06, RF07).</summary>
    IServicioEstadias Estadias { get; }

    /// <summary>COMPONENTE 5 — Facturación (RF08).</summary>
    IServicioFacturacion Facturacion { get; }

    /// <summary>COMPONENTE 6 — Reportes Administrativos (RF09).</summary>
    IServicioReportes Reportes { get; }
}
