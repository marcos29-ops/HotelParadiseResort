using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Domain.Patrones.State.Reservas;

/// <summary>Resuelve el objeto de estado a partir del valor persistido de la reserva.</summary>
public static class FabricaEstadoReserva
{
    private static readonly IReadOnlyDictionary<TipoEstadoReserva, EstadoReservaBase> Estados =
        new Dictionary<TipoEstadoReserva, EstadoReservaBase>
        {
            [TipoEstadoReserva.Pendiente] = new EstadoPendiente(),
            [TipoEstadoReserva.Confirmada] = new EstadoConfirmada(),
            [TipoEstadoReserva.Cancelada] = new EstadoCancelada(),
            [TipoEstadoReserva.Completada] = new EstadoCompletada()
        };

    public static EstadoReservaBase Crear(TipoEstadoReserva tipo) =>
        Estados.TryGetValue(tipo, out var estado)
            ? estado
            : throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Estado de reserva no reconocido.");
}
