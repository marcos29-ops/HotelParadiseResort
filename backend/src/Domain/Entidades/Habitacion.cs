using HotelParadiseResort.Domain.Comun;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.State.Habitaciones;

namespace HotelParadiseResort.Domain.Entidades;

/// <summary>
/// Unidad de hospedaje del inventario del hotel. El control de su estado en tiempo
/// real es el punto que el enunciado señala como origen de la falta de visibilidad
/// sobre la disponibilidad.
/// </summary>
public class Habitacion : EntidadBase
{
    /// <summary>Número visible de la habitación (por ejemplo, 204).</summary>
    public required string Numero { get; set; }

    public int Piso { get; set; }

    public int TipoHabitacionId { get; set; }

    public TipoHabitacion? TipoHabitacion { get; set; }

    /// <summary>Estado persistido. Las transiciones se gobiernan con el patrón State.</summary>
    public TipoEstadoHabitacion Estado { get; set; } = TipoEstadoHabitacion.Disponible;

    public string? Observaciones { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

    public ICollection<Estadia> Estadias { get; set; } = new List<Estadia>();

    /// <summary>Objeto de estado correspondiente al valor persistido.</summary>
    public EstadoHabitacionBase ObtenerEstado() => FabricaEstadoHabitacion.Crear(Estado);

    /// <summary>
    /// Aplica una transición de estado validada por el patrón State. Lanza
    /// <see cref="Shared.Excepciones.ExcepcionTransicionEstadoInvalida"/> si no procede.
    /// </summary>
    public void CambiarEstado(TipoEstadoHabitacion nuevoEstado)
    {
        Estado = ObtenerEstado().TransicionarA(nuevoEstado).Tipo;
        FechaModificacion = DateTime.UtcNow;
    }

    public bool PuedeCambiarA(TipoEstadoHabitacion nuevoEstado) =>
        ObtenerEstado().PuedeTransicionarA(nuevoEstado);

    /// <summary>Indica si la habitación admite ser asignada a una reserva nueva.</summary>
    public bool EstaDisponibleParaReservar() => Activo && ObtenerEstado().PermiteReservar;
}
