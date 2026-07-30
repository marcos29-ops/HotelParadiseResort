namespace HotelParadiseResort.Domain.Comun;

/// <summary>
/// Raíz común de las entidades persistentes. Incorpora el token de concurrencia
/// optimista a nivel de tupla exigido por RNF03, que impide asignar dos veces la
/// misma habitación cuando dos recepcionistas operan simultáneamente.
/// </summary>
public abstract class EntidadBase
{
    public int Id { get; set; }

    /// <summary>Token de concurrencia gestionado por el motor (columna rowversion).</summary>
    public byte[]? VersionFila { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }
}
