using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.Strategy;

namespace HotelParadiseResort.Domain.Patrones.Decorator;

/// <summary>
/// PATRÓN DECORATOR — Punto único donde la cadena de decoradores se arma.
///
/// Envuelve la cuenta base con un decorador por cada consumo registrado, eligiendo la
/// clase concreta según el tipo de servicio. Concentrar el ensamblaje aquí impide que
/// la construcción de la cadena quede repartida entre los servicios de aplicación.
/// </summary>
public static class EnsambladorCuenta
{
    public static IComponenteCuenta Ensamblar(
        ResultadoTarifa tarifa,
        string descripcionHabitacion,
        IEnumerable<Consumo> consumos)
    {
        IComponenteCuenta cuenta = new CuentaEstadia(tarifa, descripcionHabitacion);

        foreach (var consumo in consumos.OrderBy(c => c.FechaConsumo))
        {
            cuenta = Decorar(cuenta, consumo);
        }

        return cuenta;
    }

    private static IComponenteCuenta Decorar(IComponenteCuenta cuenta, Consumo consumo)
    {
        var tipo = consumo.ServicioAdicional?.Tipo
                   ?? throw new InvalidOperationException(
                       $"El consumo {consumo.Id} no tiene cargado su servicio adicional.");

        return tipo switch
        {
            TipoServicioAdicional.Restaurante => new ConsumoRestaurante(
                cuenta, consumo.Descripcion, consumo.Cantidad, consumo.PrecioUnitario, consumo.FechaConsumo),

            TipoServicioAdicional.Lavanderia => new ConsumoLavanderia(
                cuenta, consumo.Descripcion, consumo.Cantidad, consumo.PrecioUnitario, consumo.FechaConsumo),

            TipoServicioAdicional.Transporte => new ConsumoTransporte(
                cuenta, consumo.Descripcion, consumo.Cantidad, consumo.PrecioUnitario, consumo.FechaConsumo),

            TipoServicioAdicional.ActividadRecreativa => new ConsumoActividad(
                cuenta, consumo.Descripcion, consumo.Cantidad, consumo.PrecioUnitario, consumo.FechaConsumo),

            _ => throw new ArgumentOutOfRangeException(
                nameof(consumo), tipo, "Tipo de servicio adicional no reconocido.")
        };
    }
}
