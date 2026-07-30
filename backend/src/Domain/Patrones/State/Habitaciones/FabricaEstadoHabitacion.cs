using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Domain.Patrones.State.Habitaciones;

/// <summary>
/// Traduce el valor persistido en la base de datos al objeto de estado correspondiente.
/// Las instancias no guardan datos mutables, por lo que se comparten en lugar de
/// crearse en cada llamada.
/// </summary>
public static class FabricaEstadoHabitacion
{
    private static readonly IReadOnlyDictionary<TipoEstadoHabitacion, EstadoHabitacionBase> Estados =
        new Dictionary<TipoEstadoHabitacion, EstadoHabitacionBase>
        {
            [TipoEstadoHabitacion.Disponible] = new EstadoDisponible(),
            [TipoEstadoHabitacion.Reservada] = new EstadoReservada(),
            [TipoEstadoHabitacion.Ocupada] = new EstadoOcupada(),
            [TipoEstadoHabitacion.EnLimpieza] = new EstadoEnLimpieza(),
            [TipoEstadoHabitacion.EnMantenimiento] = new EstadoEnMantenimiento()
        };

    public static EstadoHabitacionBase Crear(TipoEstadoHabitacion tipo) =>
        Estados.TryGetValue(tipo, out var estado)
            ? estado
            : throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Estado de habitación no reconocido.");
}
