namespace HotelParadiseResort.Domain.Patrones.Decorator;

/// <summary>PATRÓN DECORATOR — Cargo por consumo en el restaurante del hotel.</summary>
public sealed class ConsumoRestaurante : DecoradorConsumoBase
{
    public ConsumoRestaurante(
        IComponenteCuenta componenteEnvuelto,
        string descripcion,
        int cantidad,
        decimal precioUnitario,
        DateTime fechaConsumo)
        : base(componenteEnvuelto, descripcion, cantidad, precioUnitario, fechaConsumo)
    {
    }

    public override string NombreServicio => "Restaurante";
}

/// <summary>PATRÓN DECORATOR — Cargo por servicio de lavandería.</summary>
public sealed class ConsumoLavanderia : DecoradorConsumoBase
{
    public ConsumoLavanderia(
        IComponenteCuenta componenteEnvuelto,
        string descripcion,
        int cantidad,
        decimal precioUnitario,
        DateTime fechaConsumo)
        : base(componenteEnvuelto, descripcion, cantidad, precioUnitario, fechaConsumo)
    {
    }

    public override string NombreServicio => "Lavandería";
}

/// <summary>PATRÓN DECORATOR — Cargo por traslados y transporte.</summary>
public sealed class ConsumoTransporte : DecoradorConsumoBase
{
    public ConsumoTransporte(
        IComponenteCuenta componenteEnvuelto,
        string descripcion,
        int cantidad,
        decimal precioUnitario,
        DateTime fechaConsumo)
        : base(componenteEnvuelto, descripcion, cantidad, precioUnitario, fechaConsumo)
    {
    }

    public override string NombreServicio => "Transporte";
}

/// <summary>PATRÓN DECORATOR — Cargo por actividades recreativas.</summary>
public sealed class ConsumoActividad : DecoradorConsumoBase
{
    public ConsumoActividad(
        IComponenteCuenta componenteEnvuelto,
        string descripcion,
        int cantidad,
        decimal precioUnitario,
        DateTime fechaConsumo)
        : base(componenteEnvuelto, descripcion, cantidad, precioUnitario, fechaConsumo)
    {
    }

    public override string NombreServicio => "Actividad recreativa";
}
