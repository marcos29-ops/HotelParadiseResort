namespace HotelParadiseResort.Domain.Patrones.Decorator;

/// <summary>
/// PATRÓN DECORATOR — Decorador abstracto.
///
/// Mantiene la referencia al componente envuelto y delega en él, sumando el importe
/// del cargo que representa. Cada tipo de servicio del hotel extiende esta clase.
/// </summary>
public abstract class DecoradorConsumoBase : IComponenteCuenta
{
    protected DecoradorConsumoBase(
        IComponenteCuenta componenteEnvuelto,
        string descripcion,
        int cantidad,
        decimal precioUnitario,
        DateTime fechaConsumo)
    {
        ComponenteEnvuelto = componenteEnvuelto;
        Descripcion = descripcion;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
        FechaConsumo = fechaConsumo;
    }

    protected IComponenteCuenta ComponenteEnvuelto { get; }

    protected string Descripcion { get; }

    protected int Cantidad { get; }

    protected decimal PrecioUnitario { get; }

    protected DateTime FechaConsumo { get; }

    /// <summary>Nombre del servicio que aparece en la columna "Servicio" de la factura.</summary>
    public abstract string NombreServicio { get; }

    /// <summary>Importe propio de este cargo, sin lo que envuelve.</summary>
    public decimal MontoPropio =>
        decimal.Round(PrecioUnitario * Cantidad, 2, MidpointRounding.AwayFromZero);

    public decimal ObtenerMonto() =>
        decimal.Round(ComponenteEnvuelto.ObtenerMonto() + MontoPropio, 2, MidpointRounding.AwayFromZero);

    public string ObtenerDescripcion() =>
        $"{ComponenteEnvuelto.ObtenerDescripcion()} + {NombreServicio}";

    public IReadOnlyList<LineaCuenta> ObtenerDetalle()
    {
        var lineas = new List<LineaCuenta>(ComponenteEnvuelto.ObtenerDetalle())
        {
            new(
                Concepto: $"{NombreServicio} — {Descripcion}",
                Cantidad: Cantidad,
                PrecioUnitario: PrecioUnitario,
                Subtotal: MontoPropio,
                EsHospedaje: false,
                Fecha: FechaConsumo)
        };

        return lineas;
    }
}
