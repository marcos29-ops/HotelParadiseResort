namespace HotelParadiseResort.Shared.Excepciones;

/// <summary>
/// Se lanza cuando se intenta llevar una habitación o una reserva a un estado que
/// su estado actual no admite. Es la salvaguarda del patrón State.
/// </summary>
public sealed class ExcepcionTransicionEstadoInvalida : ExcepcionReglaNegocio
{
    public ExcepcionTransicionEstadoInvalida(string entidad, string estadoActual, string estadoDestino)
        : base($"La {entidad} en estado '{estadoActual}' no puede pasar a '{estadoDestino}'.")
    {
        Entidad = entidad;
        EstadoActual = estadoActual;
        EstadoDestino = estadoDestino;
    }

    public string Entidad { get; }

    public string EstadoActual { get; }

    public string EstadoDestino { get; }
}
