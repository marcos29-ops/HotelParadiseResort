using HotelParadiseResort.Domain.Comun;

namespace HotelParadiseResort.Domain.Entidades;

/// <summary>
/// Bitácora de cambios sobre la ficha de un cliente. La Etapa 2 asigna al componente
/// de Gestión de Clientes la responsabilidad de mantener el historial (RF01-RF03), y
/// la Etapa 1 exige trazabilidad para la auditoría del hotel.
/// </summary>
public class HistorialCliente : EntidadBase
{
    public int ClienteId { get; set; }

    public Cliente? Cliente { get; set; }

    /// <summary>Acción registrada: creación, actualización o desactivación.</summary>
    public required string Accion { get; set; }

    public required string Detalle { get; set; }

    public int UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    public DateTime FechaRegistro { get; set; }
}
