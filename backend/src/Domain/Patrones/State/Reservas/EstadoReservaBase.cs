using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Shared.Excepciones;

namespace HotelParadiseResort.Domain.Patrones.State.Reservas;

/// <summary>
/// PATRÓN STATE — Contexto de estado de una reserva.
///
/// El ciclo pendiente → confirmada → completada, con cancelación posible antes del
/// check-in, queda expresado como un grafo de transiciones explícito en lugar de
/// repartirse en validaciones dispersas por los servicios.
/// </summary>
public abstract class EstadoReservaBase
{
    public abstract TipoEstadoReserva Tipo { get; }

    public abstract string Nombre { get; }

    /// <summary>Indica si la reserva admite modificaciones de fechas o habitación.</summary>
    public abstract bool PermiteModificacion { get; }

    /// <summary>Indica si sobre esta reserva puede registrarse un check-in.</summary>
    public abstract bool PermiteCheckIn { get; }

    /// <summary>Indica si la reserva mantiene comprometida la habitación asignada.</summary>
    public abstract bool ComprometeHabitacion { get; }

    protected abstract IReadOnlySet<TipoEstadoReserva> TransicionesPermitidas { get; }

    public bool PuedeTransicionarA(TipoEstadoReserva destino) => TransicionesPermitidas.Contains(destino);

    public EstadoReservaBase TransicionarA(TipoEstadoReserva destino)
    {
        if (!PuedeTransicionarA(destino))
        {
            throw new ExcepcionTransicionEstadoInvalida("reserva", Nombre, destino.ToString());
        }

        return FabricaEstadoReserva.Crear(destino);
    }
}
