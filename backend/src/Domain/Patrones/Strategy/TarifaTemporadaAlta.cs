namespace HotelParadiseResort.Domain.Patrones.Strategy;

/// <summary>
/// PATRÓN STRATEGY — Tarifa con recargo por temporada alta.
///
/// El enunciado identifica las "temporadas de mayor demanda" como información que la
/// administración necesita controlar. Esta estrategia aplica el recargo cuando la
/// estadía toca alguno de los meses de temporada alta del resort.
/// </summary>
public sealed class TarifaTemporadaAlta : IEstrategiaTarifa
{
    /// <summary>Recargo aplicado sobre la tarifa base durante la temporada alta.</summary>
    public const decimal PorcentajeRecargo = 0.25m;

    private static readonly IReadOnlySet<int> MesesTemporadaAlta =
        new HashSet<int> { 12, 1, 2, 7 };

    public string Codigo => "TEMPORADA_ALTA";

    public string Descripcion => $"Tarifa de temporada alta (+{PorcentajeRecargo:P0})";

    public bool AplicaA(DateTime fechaEntrada, DateTime fechaSalida)
    {
        for (var fecha = fechaEntrada.Date; fecha < fechaSalida.Date; fecha = fecha.AddDays(1))
        {
            if (MesesTemporadaAlta.Contains(fecha.Month))
            {
                return true;
            }
        }

        return false;
    }

    public ResultadoTarifa Calcular(decimal tarifaBasePorNoche, DateTime fechaEntrada, DateTime fechaSalida)
    {
        var noches = CalculadoraNoches.Calcular(fechaEntrada, fechaSalida);
        var tarifaConRecargo = decimal.Round(
            tarifaBasePorNoche * (1 + PorcentajeRecargo), 2, MidpointRounding.AwayFromZero);
        var monto = decimal.Round(tarifaConRecargo * noches, 2, MidpointRounding.AwayFromZero);

        return new ResultadoTarifa(noches, tarifaConRecargo, monto, Codigo, Descripcion);
    }
}
