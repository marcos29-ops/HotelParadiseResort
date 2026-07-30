namespace HotelParadiseResort.Domain.Patrones.Decorator;

/// <summary>
/// Renglón del desglose de la cuenta. Alimenta directamente las tablas de la pantalla
/// de facturación y las líneas del comprobante emitido.
/// </summary>
public sealed record LineaCuenta(
    string Concepto,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal,
    bool EsHospedaje,
    DateTime? Fecha);
