using HotelParadiseResort.Domain.Comun;
using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Domain.Entidades;

/// <summary>
/// Catálogo de servicios complementarios que el hotel ofrece y factura: restaurante,
/// lavandería, transporte y actividades recreativas.
///
/// La Etapa 1 apoya el principio OCP sobre esta abstracción: incorporar "Spa" o
/// "Alquiler de vehículos" es dar de alta un registro, sin tocar la lógica de facturación.
/// </summary>
public class ServicioAdicional : EntidadBase
{
    public required string Nombre { get; set; }

    public TipoServicioAdicional Tipo { get; set; }

    public string? Descripcion { get; set; }

    /// <summary>Precio de referencia. El monto final se registra en cada consumo.</summary>
    public decimal PrecioBase { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Consumo> Consumos { get; set; } = new List<Consumo>();
}
