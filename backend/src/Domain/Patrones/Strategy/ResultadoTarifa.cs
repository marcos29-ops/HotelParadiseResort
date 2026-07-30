namespace HotelParadiseResort.Domain.Patrones.Strategy;

/// <summary>
/// Desglose del cálculo de hospedaje. Se devuelven las noches y la tarifa aplicada,
/// no solo el total, porque la pantalla de facturación muestra el detalle
/// "3 noches × $85.00 = $255.00".
/// </summary>
public sealed record ResultadoTarifa(
    int Noches,
    decimal TarifaPorNoche,
    decimal MontoTotal,
    string EstrategiaAplicada,
    string Descripcion);
