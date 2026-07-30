using HotelParadiseResort.Application.DTOs.Autenticacion;
using HotelParadiseResort.Application.DTOs.Clientes;
using HotelParadiseResort.Application.DTOs.Estadias;
using HotelParadiseResort.Application.DTOs.Facturacion;
using HotelParadiseResort.Application.DTOs.Habitaciones;
using HotelParadiseResort.Application.DTOs.Reportes;
using HotelParadiseResort.Application.DTOs.Reservas;
using HotelParadiseResort.Application.DTOs.Usuarios;
using HotelParadiseResort.Shared.Paginacion;
using HotelParadiseResort.Shared.Resultados;

namespace HotelParadiseResort.Application.Servicios.Interfaces;

/// <summary>Autenticación y gestión del personal del sistema (RNF01).</summary>
public interface IServicioAutenticacion
{
    Task<Resultado<RespuestaInicioSesionDto>> IniciarSesionAsync(
        SolicitudInicioSesionDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado> CambiarContrasenaAsync(
        int usuarioId, CambioContrasenaDto solicitud, CancellationToken cancelacion = default);
}

/// <summary>Administración de los usuarios del sistema. Exclusiva del Administrador.</summary>
public interface IServicioUsuarios
{
    Task<Resultado<ResultadoPaginado<UsuarioDto>>> ListarAsync(
        ParametrosPaginacion parametros, CancellationToken cancelacion = default);

    Task<Resultado<UsuarioDto>> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default);

    Task<Resultado<UsuarioDto>> CrearAsync(CrearUsuarioDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<UsuarioDto>> ActualizarAsync(
        int id, ActualizarUsuarioDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado> RestablecerContrasenaAsync(
        int id, RestablecerContrasenaDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado> DesactivarAsync(int id, CancellationToken cancelacion = default);
}

/// <summary>COMPONENTE 1 — Gestión de Clientes (RF01).</summary>
public interface IServicioClientes
{
    Task<Resultado<ResultadoPaginado<ClienteDto>>> ListarAsync(
        ParametrosPaginacion parametros, CancellationToken cancelacion = default);

    Task<Resultado<ClienteDto>> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default);

    /// <summary>Búsqueda por documento: es como recepción localiza a un huésped.</summary>
    Task<Resultado<ClienteDto>> BuscarPorIdentificacionAsync(
        string identificacion, CancellationToken cancelacion = default);

    Task<Resultado<ClienteDto>> RegistrarAsync(
        CrearClienteDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<ClienteDto>> ActualizarAsync(
        int id, ActualizarClienteDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<IReadOnlyList<HistorialClienteDto>>> ConsultarHistorialAsync(
        int clienteId, CancellationToken cancelacion = default);

    Task<Resultado<IReadOnlyList<ReservaDto>>> ConsultarReservasAsync(
        int clienteId, CancellationToken cancelacion = default);
}

/// <summary>COMPONENTE 2 — Gestión de Habitaciones y Disponibilidad (RF02, RF03).</summary>
public interface IServicioHabitaciones
{
    Task<Resultado<IReadOnlyList<HabitacionDto>>> ListarAsync(CancellationToken cancelacion = default);

    Task<Resultado<HabitacionDto>> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default);

    Task<Resultado<HabitacionDto>> CrearAsync(
        CrearHabitacionDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<HabitacionDto>> ActualizarAsync(
        int id, ActualizarHabitacionDto solicitud, CancellationToken cancelacion = default);

    /// <summary>Aplica una transición validada por el patrón State.</summary>
    Task<Resultado<HabitacionDto>> CambiarEstadoAsync(
        int id, CambiarEstadoHabitacionDto solicitud, CancellationToken cancelacion = default);

    /// <summary>Consulta de disponibilidad con la tarifa ya calculada (RF03).</summary>
    Task<Resultado<IReadOnlyList<HabitacionDisponibleDto>>> ConsultarDisponibilidadAsync(
        ConsultaDisponibilidadDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<IReadOnlyList<TipoHabitacionDto>>> ListarTiposAsync(
        CancellationToken cancelacion = default);

    Task<Resultado<TipoHabitacionDto>> CrearTipoAsync(
        CrearTipoHabitacionDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<TipoHabitacionDto>> ActualizarTipoAsync(
        int id, ActualizarTipoHabitacionDto solicitud, CancellationToken cancelacion = default);
}

/// <summary>COMPONENTE 3 — Gestión de Reservas (RF04).</summary>
public interface IServicioReservas
{
    Task<Resultado<ResultadoPaginado<ReservaDto>>> ListarAsync(
        ParametrosPaginacion parametros,
        string? estado = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancelacion = default);

    Task<Resultado<ReservaDto>> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default);

    /// <summary>Crea la reserva verificando la disponibilidad de forma transaccional (RNF03).</summary>
    Task<Resultado<ReservaDto>> CrearAsync(
        CrearReservaDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<ReservaDto>> ActualizarAsync(
        int id, ActualizarReservaDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<ReservaDto>> ConfirmarAsync(int id, CancellationToken cancelacion = default);

    Task<Resultado<ReservaDto>> CancelarAsync(
        int id, CancelarReservaDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<IReadOnlyList<ReservaDto>>> ConsultarLlegadasDelDiaAsync(
        DateTime? fecha = null, CancellationToken cancelacion = default);
}

/// <summary>COMPONENTE 4 — Gestión de Estadías y Consumos (RF05, RF06, RF07).</summary>
public interface IServicioEstadias
{
    Task<Resultado<ResultadoPaginado<EstadiaDto>>> ListarAsync(
        ParametrosPaginacion parametros, string? estado = null, CancellationToken cancelacion = default);

    Task<Resultado<EstadiaDto>> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default);

    /// <summary>Localiza la estadía en curso de una habitación (buscador de check-in/out).</summary>
    Task<Resultado<EstadiaDto>> BuscarActivaPorHabitacionAsync(
        string numeroHabitacion, CancellationToken cancelacion = default);

    Task<Resultado<EstadiaDto>> RegistrarCheckInAsync(
        RegistrarCheckInDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<EstadiaDto>> RegistrarCheckOutAsync(
        int estadiaId, RegistrarCheckOutDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<ConsumoDto>> RegistrarConsumoAsync(
        int estadiaId, RegistrarConsumoDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado> EliminarConsumoAsync(
        int estadiaId, int consumoId, CancellationToken cancelacion = default);

    /// <summary>Cuenta consolidada producida por la cadena de decoradores.</summary>
    Task<Resultado<CuentaEstadiaDto>> ConsultarCuentaAsync(
        int estadiaId, CancellationToken cancelacion = default);

    Task<Resultado<IReadOnlyList<ServicioAdicionalDto>>> ListarServiciosAsync(
        CancellationToken cancelacion = default);

    Task<Resultado<ServicioAdicionalDto>> CrearServicioAsync(
        CrearServicioAdicionalDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<ServicioAdicionalDto>> ActualizarServicioAsync(
        int id, ActualizarServicioAdicionalDto solicitud, CancellationToken cancelacion = default);
}

/// <summary>COMPONENTE 5 — Facturación (RF08).</summary>
public interface IServicioFacturacion
{
    Task<Resultado<ResultadoPaginado<FacturaDto>>> ListarAsync(
        ParametrosPaginacion parametros,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancelacion = default);

    Task<Resultado<FacturaDto>> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default);

    Task<Resultado<FacturaDto>> ObtenerPorEstadiaAsync(int estadiaId, CancellationToken cancelacion = default);

    /// <summary>Emite el comprobante consolidado mediante el patrón Builder.</summary>
    Task<Resultado<FacturaDto>> GenerarAsync(
        GenerarFacturaDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<FacturaDto>> AplicarDescuentoAsync(
        int id, AplicarDescuentoDto solicitud, CancellationToken cancelacion = default);

    Task<Resultado<FacturaDto>> RegistrarPagoAsync(
        int id, RegistrarPagoDto solicitud, CancellationToken cancelacion = default);
}

/// <summary>COMPONENTE 6 — Reportes Administrativos (RF09).</summary>
public interface IServicioReportes
{
    Task<Resultado<ReporteDto>> GenerarReporteOcupacionAsync(
        FiltroReporteDto filtro, CancellationToken cancelacion = default);

    Task<Resultado<ReporteDto>> GenerarReporteIngresosAsync(
        FiltroReporteDto filtro, CancellationToken cancelacion = default);

    Task<Resultado<ReporteDto>> GenerarReporteTemporadaAsync(
        FiltroReporteDto filtro, CancellationToken cancelacion = default);

    /// <summary>Indicadores del panel principal.</summary>
    Task<Resultado<ResumenPanelDto>> ObtenerResumenPanelAsync(CancellationToken cancelacion = default);
}
