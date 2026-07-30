using HotelParadiseResort.Domain.Comun;

namespace HotelParadiseResort.Domain.Entidades;

/// <summary>
/// Línea del comprobante. Congela el detalle en el momento de la emisión —concepto,
/// cantidad, precio unitario y subtotal— tal como lo muestra la pantalla de
/// facturación, de modo que reimprimir una factura antigua no dependa de precios
/// que pudieron cambiar después.
/// </summary>
public class DetalleFactura : EntidadBase
{
    public int FacturaId { get; set; }

    public Factura? Factura { get; set; }

    public required string Concepto { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    /// <summary>Distingue el renglón de hospedaje de los renglones de consumo.</summary>
    public bool EsHospedaje { get; set; }

    public DateTime? FechaConsumo { get; set; }
}
