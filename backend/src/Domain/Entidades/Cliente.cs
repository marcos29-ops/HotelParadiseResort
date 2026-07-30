using HotelParadiseResort.Domain.Comun;

namespace HotelParadiseResort.Domain.Entidades;

/// <summary>
/// Huésped del hotel. Es una entidad de negocio administrada por el personal: no
/// inicia sesión en el sistema (ver diagrama de casos de uso).
/// </summary>
public class Cliente : EntidadBase
{
    /// <summary>Documento de identidad. Es la clave con la que recepción lo busca.</summary>
    public required string Identificacion { get; set; }

    public required string Nombre { get; set; }

    public required string Apellidos { get; set; }

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? Nacionalidad { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    public bool Activo { get; set; } = true;

    /// <summary>Reservas del cliente (Cliente 1 → 0..* Reserva).</summary>
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

    public string NombreCompleto => $"{Nombre} {Apellidos}".Trim();
}
