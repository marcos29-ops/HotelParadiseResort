using HotelParadiseResort.Shared.Excepciones;

namespace HotelParadiseResort.Domain.Patrones.Strategy;

/// <summary>
/// Regla única de conversión entre un rango de fechas y noches facturables, para que
/// todas las estrategias de tarifa cuenten igual.
/// </summary>
public static class CalculadoraNoches
{
    /// <summary>
    /// Noches entre la entrada y la salida. Una estadía del 20 al 23 son 3 noches.
    /// Una entrada y salida el mismo día se cobra como una noche.
    /// </summary>
    public static int Calcular(DateTime fechaEntrada, DateTime fechaSalida)
    {
        if (fechaSalida.Date < fechaEntrada.Date)
        {
            throw new ExcepcionReglaNegocio(
                "La fecha de salida no puede ser anterior a la fecha de entrada.");
        }

        var noches = (fechaSalida.Date - fechaEntrada.Date).Days;
        return noches == 0 ? 1 : noches;
    }
}
