namespace HotelParadiseResort.Domain.Repositorios;

/// <summary>
/// Coordina la confirmación de los cambios y el manejo transaccional.
///
/// La verificación de disponibilidad y la creación de la reserva deben ocurrir dentro
/// de una misma transacción para impedir la doble asignación de una habitación (RNF03).
/// </summary>
public interface IUnidadDeTrabajo
{
    IRepositorioUsuario Usuarios { get; }

    IRepositorioCliente Clientes { get; }

    IRepositorioHabitacion Habitaciones { get; }

    IRepositorioTipoHabitacion TiposHabitacion { get; }

    IRepositorioReserva Reservas { get; }

    IRepositorioEstadia Estadias { get; }

    IRepositorioConsumo Consumos { get; }

    IRepositorioServicioAdicional ServiciosAdicionales { get; }

    IRepositorioFactura Facturas { get; }

    IRepositorioHistorialCliente HistorialClientes { get; }

    Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Ejecuta la operación dentro de una transacción serializable. Es el mecanismo
    /// que impide la sobre-reserva ante peticiones concurrentes.
    /// </summary>
    Task<TResultado> EjecutarEnTransaccionAsync<TResultado>(
        Func<CancellationToken, Task<TResultado>> operacion,
        CancellationToken cancelacion = default);
}
