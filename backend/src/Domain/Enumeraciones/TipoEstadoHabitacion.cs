namespace HotelParadiseResort.Domain.Enumeraciones;

/// <summary>
/// Estados de una habitación según la Etapa 2: disponible, ocupada, reservada,
/// en limpieza y en mantenimiento.
/// </summary>
public enum TipoEstadoHabitacion
{
    Disponible = 1,
    Reservada = 2,
    Ocupada = 3,
    EnLimpieza = 4,
    EnMantenimiento = 5
}
