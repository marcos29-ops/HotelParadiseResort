using HotelParadiseResort.Domain.Comun;
using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Domain.Entidades;

/// <summary>
/// Comprobante que consolida hospedaje y consumos de una estadía.
///
/// Conforme al principio SRP documentado en la Etapa 1, esta clase concentra los datos
/// de cobro y sus cálculos aritméticos; no se ocupa de persistirse ni de presentarse.
/// Su construcción se realiza mediante <see cref="Patrones.Builder.FacturaBuilder"/>.
/// </summary>
public class Factura : EntidadBase
{
    /// <summary>Número consecutivo del comprobante (por ejemplo, FAC-000045).</summary>
    public required string Numero { get; set; }

    public int EstadiaId { get; set; }

    public Estadia? Estadia { get; set; }

    public int ClienteId { get; set; }

    public Cliente? Cliente { get; set; }

    public DateTime FechaEmision { get; set; }

    /// <summary>Noches facturadas.</summary>
    public int Noches { get; set; }

    /// <summary>Tarifa por noche aplicada, ya resuelta por la estrategia de tarifa.</summary>
    public decimal TarifaPorNoche { get; set; }

    /// <summary>Estrategia de tarifa utilizada, para trazabilidad del cobro.</summary>
    public required string EstrategiaTarifa { get; set; }

    public decimal SubtotalHospedaje { get; set; }

    public decimal SubtotalConsumos { get; set; }

    public decimal Descuento { get; set; }

    public string? JustificacionDescuento { get; set; }

    public decimal Total { get; set; }

    public MetodoPago? MetodoPago { get; set; }

    public EstadoPago EstadoPago { get; set; } = EstadoPago.Pendiente;

    public DateTime? FechaPago { get; set; }

    /// <summary>Usuario que emitió el comprobante.</summary>
    public int UsuarioEmisionId { get; set; }

    public Usuario? UsuarioEmision { get; set; }

    public ICollection<DetalleFactura> Detalles { get; set; } = new List<DetalleFactura>();

    /// <summary>
    /// Recalcula el total a partir de sus componentes. La factura es la única
    /// responsable de esta aritmética.
    /// </summary>
    public decimal CalcularTotal()
    {
        var total = SubtotalHospedaje + SubtotalConsumos - Descuento;
        return decimal.Round(total < 0 ? 0 : total, 2, MidpointRounding.AwayFromZero);
    }

    public void RegistrarPago(MetodoPago metodoPago, DateTime momento)
    {
        MetodoPago = metodoPago;
        EstadoPago = EstadoPago.Pagado;
        FechaPago = momento;
        FechaModificacion = momento;
    }
}
