using HotelParadiseResort.Domain.Comun;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.State.Reservas;
using HotelParadiseResort.Domain.Patrones.Strategy;

namespace HotelParadiseResort.Domain.Entidades;

/// <summary>
/// Compromiso de hospedaje de un cliente sobre una habitación en un rango de fechas.
/// Es el núcleo del problema que el sistema resuelve: evitar la duplicidad de reservas
/// sobre la misma habitación.
/// </summary>
public class Reserva : EntidadBase
{
    /// <summary>Código legible para el personal (por ejemplo, RES-00123).</summary>
    public required string Codigo { get; set; }

    public int ClienteId { get; set; }

    public Cliente? Cliente { get; set; }

    public int HabitacionId { get; set; }

    public Habitacion? Habitacion { get; set; }

    /// <summary>Usuario que registró la reserva (Usuario 1 → 0..* Reserva).</summary>
    public int UsuarioRegistroId { get; set; }

    public Usuario? UsuarioRegistro { get; set; }

    public DateTime FechaEntrada { get; set; }

    public DateTime FechaSalida { get; set; }

    public int CantidadHuespedes { get; set; }

    public TipoEstadoReserva Estado { get; set; } = TipoEstadoReserva.Pendiente;

    public CanalOrigenReserva CanalOrigen { get; set; }

    /// <summary>Monto de hospedaje estimado al momento de crear la reserva.</summary>
    public decimal MontoEstimado { get; set; }

    public string? Observaciones { get; set; }

    public DateTime? FechaCancelacion { get; set; }

    public string? MotivoCancelacion { get; set; }

    public Estadia? Estadia { get; set; }

    public EstadoReservaBase ObtenerEstado() => FabricaEstadoReserva.Crear(Estado);

    public void CambiarEstado(TipoEstadoReserva nuevoEstado)
    {
        Estado = ObtenerEstado().TransicionarA(nuevoEstado).Tipo;
        FechaModificacion = DateTime.UtcNow;
    }

    public bool PuedeCambiarA(TipoEstadoReserva nuevoEstado) =>
        ObtenerEstado().PuedeTransicionarA(nuevoEstado);

    /// <summary>Noches comprometidas por la reserva.</summary>
    public int CalcularNoches() => CalculadoraNoches.Calcular(FechaEntrada, FechaSalida);

    /// <summary>
    /// Determina si esta reserva se solapa con el rango indicado. Dos estadías que se
    /// tocan en un extremo no se solapan: el huésped que sale libera la habitación el
    /// mismo día en que entra el siguiente.
    /// </summary>
    public bool SeSolapaCon(DateTime entrada, DateTime salida) =>
        FechaEntrada.Date < salida.Date && entrada.Date < FechaSalida.Date;
}
