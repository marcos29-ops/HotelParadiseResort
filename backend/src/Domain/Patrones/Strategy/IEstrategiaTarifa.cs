namespace HotelParadiseResort.Domain.Patrones.Strategy;

/// <summary>
/// PATRÓN STRATEGY — Abstracción del cálculo del monto de hospedaje.
///
/// La Etapa 2 anticipa que la tarifa variará según temporada y promociones. Al
/// depender de esta interfaz, el componente de Facturación queda cerrado a
/// modificación y abierto a extensión: incorporar una tarifa nueva solo requiere una
/// implementación adicional.
/// </summary>
public interface IEstrategiaTarifa
{
    /// <summary>Identificador con el que se selecciona la estrategia.</summary>
    string Codigo { get; }

    /// <summary>Descripción que acompaña al detalle de la factura.</summary>
    string Descripcion { get; }

    /// <summary>Determina si la estrategia aplica al período indicado.</summary>
    bool AplicaA(DateTime fechaEntrada, DateTime fechaSalida);

    /// <summary>Calcula el monto de hospedaje para el período y la tarifa base dados.</summary>
    ResultadoTarifa Calcular(decimal tarifaBasePorNoche, DateTime fechaEntrada, DateTime fechaSalida);
}
