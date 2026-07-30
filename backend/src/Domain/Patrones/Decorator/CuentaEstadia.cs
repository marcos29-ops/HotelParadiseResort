using HotelParadiseResort.Domain.Patrones.Strategy;

namespace HotelParadiseResort.Domain.Patrones.Decorator;

/// <summary>
/// PATRÓN DECORATOR — Componente concreto (la base que los decoradores envuelven).
///
/// Contiene únicamente el hospedaje, ya calculado por la estrategia de tarifa vigente.
/// Sobre esta cuenta se van apilando los consumos de la estadía.
/// </summary>
public sealed class CuentaEstadia : IComponenteCuenta
{
    private readonly ResultadoTarifa _tarifa;
    private readonly string _descripcionHabitacion;

    public CuentaEstadia(ResultadoTarifa tarifa, string descripcionHabitacion)
    {
        _tarifa = tarifa;
        _descripcionHabitacion = descripcionHabitacion;
    }

    public decimal ObtenerMonto() => _tarifa.MontoTotal;

    public string ObtenerDescripcion() =>
        $"{_descripcionHabitacion} — {_tarifa.Noches} noche(s) × {_tarifa.TarifaPorNoche:C2}";

    public IReadOnlyList<LineaCuenta> ObtenerDetalle() =>
    [
        new LineaCuenta(
            Concepto: _descripcionHabitacion,
            Cantidad: _tarifa.Noches,
            PrecioUnitario: _tarifa.TarifaPorNoche,
            Subtotal: _tarifa.MontoTotal,
            EsHospedaje: true,
            Fecha: null)
    ];
}
