using HotelParadiseResort.Domain.Patrones.Strategy;

namespace HotelParadiseResort.Application.Tarifas;

/// <summary>
/// Elige la estrategia de tarifa aplicable a un período.
///
/// PATRÓN STRATEGY — Este selector es el único punto que conoce el conjunto de
/// estrategias disponibles. Incorporar una tarifa nueva consiste en registrar su
/// implementación en el contenedor de dependencias, sin tocar Facturación ni Reservas.
/// </summary>
public interface ISelectorEstrategiaTarifa
{
    IEstrategiaTarifa Seleccionar(DateTime fechaEntrada, DateTime fechaSalida);

    ResultadoTarifa Calcular(decimal tarifaBasePorNoche, DateTime fechaEntrada, DateTime fechaSalida);
}

/// <inheritdoc cref="ISelectorEstrategiaTarifa"/>
public sealed class SelectorEstrategiaTarifa : ISelectorEstrategiaTarifa
{
    private readonly IReadOnlyList<IEstrategiaTarifa> _estrategiasEspeciales;
    private readonly IEstrategiaTarifa _estrategiaPorDefecto;

    public SelectorEstrategiaTarifa(IEnumerable<IEstrategiaTarifa> estrategias)
    {
        var todas = estrategias.ToList();

        _estrategiaPorDefecto = todas.OfType<TarifaEstandar>().FirstOrDefault()
                                ?? new TarifaEstandar();

        // Las estrategias especiales se evalúan primero; la estándar es el respaldo.
        _estrategiasEspeciales = todas.Where(e => e is not TarifaEstandar).ToList();
    }

    public IEstrategiaTarifa Seleccionar(DateTime fechaEntrada, DateTime fechaSalida) =>
        _estrategiasEspeciales.FirstOrDefault(e => e.AplicaA(fechaEntrada, fechaSalida))
        ?? _estrategiaPorDefecto;

    public ResultadoTarifa Calcular(decimal tarifaBasePorNoche, DateTime fechaEntrada, DateTime fechaSalida) =>
        Seleccionar(fechaEntrada, fechaSalida).Calcular(tarifaBasePorNoche, fechaEntrada, fechaSalida);
}
