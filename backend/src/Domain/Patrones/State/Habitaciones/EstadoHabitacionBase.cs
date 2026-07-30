using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Shared.Excepciones;

namespace HotelParadiseResort.Domain.Patrones.State.Habitaciones;

/// <summary>
/// PATRÓN STATE — Contexto de estado de una habitación.
///
/// Cada estado concreto declara a qué otros estados puede pasar y si la habitación
/// admite ser reservada u ocupada mientras se encuentra en él. Concentrar ese
/// conocimiento en clases de estado evita la maraña de condicionales que, según la
/// Etapa 2, es una fuente de inconsistencias cuando varios usuarios trabajan a la vez.
/// </summary>
public abstract class EstadoHabitacionBase
{
    /// <summary>Estado que esta instancia representa.</summary>
    public abstract TipoEstadoHabitacion Tipo { get; }

    /// <summary>Nombre legible para la interfaz y los mensajes de error.</summary>
    public abstract string Nombre { get; }

    /// <summary>Indica si la habitación puede asignarse a una reserva nueva.</summary>
    public abstract bool PermiteReservar { get; }

    /// <summary>Indica si la habitación admite un check-in en este estado.</summary>
    public abstract bool PermiteCheckIn { get; }

    /// <summary>Estados a los que este estado puede transicionar.</summary>
    protected abstract IReadOnlySet<TipoEstadoHabitacion> TransicionesPermitidas { get; }

    public bool PuedeTransicionarA(TipoEstadoHabitacion destino) => TransicionesPermitidas.Contains(destino);

    /// <summary>
    /// Valida la transición y devuelve el estado destino. Lanza
    /// <see cref="ExcepcionTransicionEstadoInvalida"/> si la transición no está permitida.
    /// </summary>
    public EstadoHabitacionBase TransicionarA(TipoEstadoHabitacion destino)
    {
        if (!PuedeTransicionarA(destino))
        {
            throw new ExcepcionTransicionEstadoInvalida("habitación", Nombre, destino.ToString());
        }

        return FabricaEstadoHabitacion.Crear(destino);
    }
}
