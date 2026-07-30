using HotelParadiseResort.Domain.Comun;

namespace HotelParadiseResort.Domain.Entidades;

/// <summary>
/// Categoría comercial de habitación (por ejemplo, "Doble – Vista al mar"). Define la
/// tarifa base sobre la que opera la estrategia de cálculo de tarifas.
/// </summary>
public class TipoHabitacion : EntidadBase
{
    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    /// <summary>Tarifa base por noche, en dólares.</summary>
    public decimal TarifaBasePorNoche { get; set; }

    public int CapacidadMaxima { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Habitacion> Habitaciones { get; set; } = new List<Habitacion>();
}
