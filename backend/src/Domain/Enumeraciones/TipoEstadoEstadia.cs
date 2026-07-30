namespace HotelParadiseResort.Domain.Enumeraciones;

/// <summary>
/// Situación de una estadía: se abre con el check-in y se cierra con el check-out,
/// momento a partir del cual queda lista para facturarse.
/// </summary>
public enum TipoEstadoEstadia
{
    EnCurso = 1,
    Finalizada = 2,
    Facturada = 3
}
