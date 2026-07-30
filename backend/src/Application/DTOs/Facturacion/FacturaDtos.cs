namespace HotelParadiseResort.Application.DTOs.Facturacion;

/// <summary>Comprobante emitido, con el desglose que muestra la pantalla de facturación.</summary>
public sealed record FacturaDto(
    int Id,
    string Numero,
    int EstadiaId,
    int ClienteId,
    string ClienteNombre,
    string ClienteIdentificacion,
    string HabitacionNumero,
    string TipoHabitacion,
    string ReservaCodigo,
    DateTime FechaEntrada,
    DateTime FechaSalida,
    DateTime FechaEmision,
    int Noches,
    decimal TarifaPorNoche,
    string EstrategiaTarifa,
    decimal SubtotalHospedaje,
    decimal SubtotalConsumos,
    decimal Descuento,
    string? JustificacionDescuento,
    decimal Total,
    string? MetodoPago,
    string EstadoPago,
    DateTime? FechaPago,
    string EmitidaPor,
    IReadOnlyList<DetalleFacturaDto> Detalles);

public sealed record DetalleFacturaDto(
    int Id,
    string Concepto,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal,
    bool EsHospedaje,
    DateTime? FechaConsumo);

/// <summary>
/// Emisión de la factura de una estadía cerrada. El descuento es opcional; cuando se
/// aplica, su justificación es obligatoria.
/// </summary>
public sealed record GenerarFacturaDto(
    int EstadiaId,
    decimal? Descuento,
    string? JustificacionDescuento,
    string? MetodoPago,
    bool RegistrarPagoInmediato);

/// <summary>Descuento aplicado sobre una factura ya emitida y todavía pendiente de pago.</summary>
public sealed record AplicarDescuentoDto(decimal Monto, string Justificacion);

/// <summary>Registro del cobro de una factura.</summary>
public sealed record RegistrarPagoDto(string MetodoPago);
