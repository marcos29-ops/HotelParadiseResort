namespace HotelParadiseResort.Domain.Patrones.Decorator;

/// <summary>
/// PATRÓN DECORATOR — Componente de la cuenta de una estadía.
///
/// Tanto la cuenta base (hospedaje) como cada cargo que la envuelve implementan esta
/// misma interfaz. Así, <c>Estadia</c> no necesita conocer de antemano las
/// combinaciones posibles de servicios: la cuenta se arma envolviendo capas.
/// </summary>
public interface IComponenteCuenta
{
    /// <summary>Importe acumulado de este componente y de todo lo que envuelve.</summary>
    decimal ObtenerMonto();

    /// <summary>Texto descriptivo del componente.</summary>
    string ObtenerDescripcion();

    /// <summary>Desglose línea por línea, en orden de aplicación.</summary>
    IReadOnlyList<LineaCuenta> ObtenerDetalle();
}
