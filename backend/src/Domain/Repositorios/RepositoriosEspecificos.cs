using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Shared.Paginacion;

namespace HotelParadiseResort.Domain.Repositorios;

/// <summary>Acceso a los usuarios del sistema (personal del hotel).</summary>
public interface IRepositorioUsuario : IRepositorioBase<Usuario>
{
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion = default);

    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, int? idExcluir = null, CancellationToken cancelacion = default);

    Task<ResultadoPaginado<Usuario>> BuscarAsync(ParametrosPaginacion parametros, CancellationToken cancelacion = default);
}

/// <summary>Acceso a los huéspedes registrados.</summary>
public interface IRepositorioCliente : IRepositorioBase<Cliente>
{
    Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion, CancellationToken cancelacion = default);

    Task<bool> ExisteIdentificacionAsync(string identificacion, int? idExcluir = null, CancellationToken cancelacion = default);

    Task<ResultadoPaginado<Cliente>> BuscarAsync(ParametrosPaginacion parametros, CancellationToken cancelacion = default);

    /// <summary>Cantidad de reservas de un cliente, para la columna del listado.</summary>
    Task<IReadOnlyDictionary<int, int>> ContarReservasPorClienteAsync(
        IEnumerable<int> clienteIds, CancellationToken cancelacion = default);
}

/// <summary>Acceso al inventario de habitaciones y a la consulta de disponibilidad.</summary>
public interface IRepositorioHabitacion : IRepositorioBase<Habitacion>
{
    Task<Habitacion?> ObtenerPorNumeroAsync(string numero, CancellationToken cancelacion = default);

    Task<Habitacion?> ObtenerConTipoAsync(int id, CancellationToken cancelacion = default);

    Task<IReadOnlyList<Habitacion>> ObtenerTodasConTipoAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Habitaciones libres en el rango indicado. Excluye las que ya tienen una reserva
    /// vigente solapada y las que no están operativas.
    /// </summary>
    Task<IReadOnlyList<Habitacion>> ObtenerDisponiblesAsync(
        DateTime fechaEntrada,
        DateTime fechaSalida,
        int? tipoHabitacionId = null,
        int? reservaExcluidaId = null,
        CancellationToken cancelacion = default);

    /// <summary>Verifica si una habitación concreta está libre en el rango indicado.</summary>
    Task<bool> EstaDisponibleAsync(
        int habitacionId,
        DateTime fechaEntrada,
        DateTime fechaSalida,
        int? reservaExcluidaId = null,
        CancellationToken cancelacion = default);

    Task<IReadOnlyDictionary<TipoEstadoHabitacion, int>> ContarPorEstadoAsync(CancellationToken cancelacion = default);

    Task<int> ContarActivasAsync(CancellationToken cancelacion = default);

    Task<bool> ExisteNumeroAsync(string numero, int? idExcluir = null, CancellationToken cancelacion = default);
}

/// <summary>Acceso al catálogo de tipos de habitación.</summary>
public interface IRepositorioTipoHabitacion : IRepositorioBase<TipoHabitacion>
{
    Task<IReadOnlyList<TipoHabitacion>> ObtenerActivosAsync(CancellationToken cancelacion = default);

    Task<bool> TieneHabitacionesAsociadasAsync(int id, CancellationToken cancelacion = default);

    Task<bool> ExisteNombreAsync(string nombre, int? idExcluir = null, CancellationToken cancelacion = default);
}

/// <summary>Acceso a las reservas.</summary>
public interface IRepositorioReserva : IRepositorioBase<Reserva>
{
    Task<Reserva?> ObtenerCompletaAsync(int id, CancellationToken cancelacion = default);

    Task<Reserva?> ObtenerPorCodigoAsync(string codigo, CancellationToken cancelacion = default);

    Task<ResultadoPaginado<Reserva>> BuscarAsync(
        ParametrosPaginacion parametros,
        TipoEstadoReserva? estado = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancelacion = default);

    Task<IReadOnlyList<Reserva>> ObtenerPorClienteAsync(int clienteId, CancellationToken cancelacion = default);

    /// <summary>Reservas confirmadas cuya entrada es la fecha indicada (check-in del día).</summary>
    Task<IReadOnlyList<Reserva>> ObtenerLlegadasDelDiaAsync(DateTime fecha, CancellationToken cancelacion = default);

    Task<int> ContarPorEstadoAsync(TipoEstadoReserva estado, CancellationToken cancelacion = default);

    Task<int> ContarReservasDelDiaAsync(DateTime fecha, CancellationToken cancelacion = default);

    Task<string> GenerarCodigoAsync(CancellationToken cancelacion = default);

    /// <summary>Datos históricos para el reporte de temporadas.</summary>
    Task<IReadOnlyList<Reserva>> ObtenerEnRangoAsync(
        DateTime desde, DateTime hasta, int? tipoHabitacionId = null, CancellationToken cancelacion = default);
}

/// <summary>Acceso a las estadías (check-in y check-out).</summary>
public interface IRepositorioEstadia : IRepositorioBase<Estadia>
{
    Task<Estadia?> ObtenerCompletaAsync(int id, CancellationToken cancelacion = default);

    Task<Estadia?> ObtenerPorReservaAsync(int reservaId, CancellationToken cancelacion = default);

    /// <summary>Estadía en curso de una habitación, si la hay.</summary>
    Task<Estadia?> ObtenerActivaPorHabitacionAsync(int habitacionId, CancellationToken cancelacion = default);

    Task<ResultadoPaginado<Estadia>> BuscarAsync(
        ParametrosPaginacion parametros,
        TipoEstadoEstadia? estado = null,
        CancellationToken cancelacion = default);

    Task<int> ContarCheckOutsDelDiaAsync(DateTime fecha, CancellationToken cancelacion = default);

    Task<int> ContarCheckInsDelDiaAsync(DateTime fecha, CancellationToken cancelacion = default);

    /// <summary>Datos históricos para el reporte de ocupación.</summary>
    Task<IReadOnlyList<Estadia>> ObtenerEnRangoAsync(
        DateTime desde, DateTime hasta, int? tipoHabitacionId = null, CancellationToken cancelacion = default);
}

/// <summary>Acceso a los consumos imputados a las estadías.</summary>
public interface IRepositorioConsumo : IRepositorioBase<Consumo>
{
    Task<IReadOnlyList<Consumo>> ObtenerPorEstadiaAsync(int estadiaId, CancellationToken cancelacion = default);

    Task<Consumo?> ObtenerConServicioAsync(int id, CancellationToken cancelacion = default);

    Task<decimal> ObtenerTotalPorEstadiaAsync(int estadiaId, CancellationToken cancelacion = default);
}

/// <summary>Acceso al catálogo de servicios adicionales.</summary>
public interface IRepositorioServicioAdicional : IRepositorioBase<ServicioAdicional>
{
    Task<IReadOnlyList<ServicioAdicional>> ObtenerActivosAsync(CancellationToken cancelacion = default);

    Task<bool> TieneConsumosAsociadosAsync(int id, CancellationToken cancelacion = default);

    Task<bool> ExisteNombreAsync(string nombre, int? idExcluir = null, CancellationToken cancelacion = default);
}

/// <summary>Acceso a los comprobantes emitidos.</summary>
public interface IRepositorioFactura : IRepositorioBase<Factura>
{
    Task<Factura?> ObtenerCompletaAsync(int id, CancellationToken cancelacion = default);

    Task<Factura?> ObtenerPorEstadiaAsync(int estadiaId, CancellationToken cancelacion = default);

    Task<Factura?> ObtenerPorNumeroAsync(string numero, CancellationToken cancelacion = default);

    Task<ResultadoPaginado<Factura>> BuscarAsync(
        ParametrosPaginacion parametros,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancelacion = default);

    Task<string> GenerarNumeroAsync(CancellationToken cancelacion = default);

    Task<decimal> ObtenerIngresosDelDiaAsync(DateTime fecha, CancellationToken cancelacion = default);

    /// <summary>Datos históricos para el reporte de ingresos.</summary>
    Task<IReadOnlyList<Factura>> ObtenerEnRangoAsync(
        DateTime desde, DateTime hasta, CancellationToken cancelacion = default);
}

/// <summary>Acceso a la bitácora de cambios sobre los clientes.</summary>
public interface IRepositorioHistorialCliente : IRepositorioBase<HistorialCliente>
{
    Task<IReadOnlyList<HistorialCliente>> ObtenerPorClienteAsync(int clienteId, CancellationToken cancelacion = default);
}
