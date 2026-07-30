namespace HotelParadiseResort.Domain.Enumeraciones;

/// <summary>
/// Estados de una reserva según la Etapa 2: pendiente, confirmada, cancelada
/// y completada. Toda reserva nace en <see cref="Pendiente"/>.
/// </summary>
public enum TipoEstadoReserva
{
    Pendiente = 1,
    Confirmada = 2,
    Cancelada = 3,
    Completada = 4
}
