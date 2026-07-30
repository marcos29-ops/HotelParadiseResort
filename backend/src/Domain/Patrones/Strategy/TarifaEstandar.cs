namespace HotelParadiseResort.Domain.Patrones.Strategy;

/// <summary>
/// PATRÓN STRATEGY — Implementación por defecto: noches × tarifa base del tipo de
/// habitación, sin recargos. Aplica a cualquier período que no caiga en temporada alta.
/// </summary>
public sealed class TarifaEstandar : IEstrategiaTarifa
{
    public string Codigo => "ESTANDAR";

    public string Descripcion => "Tarifa estándar";

    /// <summary>Es la estrategia de referencia: acepta cualquier período.</summary>
    public bool AplicaA(DateTime fechaEntrada, DateTime fechaSalida) => true;

    public ResultadoTarifa Calcular(decimal tarifaBasePorNoche, DateTime fechaEntrada, DateTime fechaSalida)
    {
        var noches = CalculadoraNoches.Calcular(fechaEntrada, fechaSalida);
        var monto = decimal.Round(tarifaBasePorNoche * noches, 2, MidpointRounding.AwayFromZero);

        return new ResultadoTarifa(noches, tarifaBasePorNoche, monto, Codigo, Descripcion);
    }
}
