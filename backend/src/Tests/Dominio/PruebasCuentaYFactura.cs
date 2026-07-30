using FluentAssertions;
using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.Builder;
using HotelParadiseResort.Domain.Patrones.Decorator;
using HotelParadiseResort.Domain.Patrones.Strategy;
using HotelParadiseResort.Shared.Excepciones;

namespace HotelParadiseResort.Tests.Dominio;

/// <summary>
/// PATRONES DECORATOR y BUILDER — Consolidación de la cuenta y armado de la factura.
///
/// El escenario reproduce el wireframe de facturación de la Etapa 2: hospedaje de
/// 3 noches × $85.00 más consumos de restaurante ($28.50), lavandería ($6.00) y
/// transporte ($18.00), con un descuento de $10.00 → total $297.50.
/// </summary>
public sealed class PruebasCuentaYFactura
{
    private static readonly ResultadoTarifa TarifaTresNoches =
        new(3, 85.00m, 255.00m, "ESTANDAR", "Tarifa estándar");

    [Fact]
    public void CuentaSinConsumos_SoloIncluyeElHospedaje()
    {
        var cuenta = new CuentaEstadia(TarifaTresNoches, "Habitación 204");

        cuenta.ObtenerMonto().Should().Be(255.00m);
        cuenta.ObtenerDetalle().Should().ContainSingle().Which.EsHospedaje.Should().BeTrue();
    }

    [Fact]
    public void CadenaDeDecoradores_AcumulaLosConsumosDelWireframe()
    {
        var cuenta = EnsambladorCuenta.Ensamblar(
            TarifaTresNoches, "Habitación 204 — Doble – Vista al mar", ConsumosDelWireframe());

        cuenta.ObtenerMonto().Should().Be(307.50m);

        var detalle = cuenta.ObtenerDetalle();
        detalle.Should().HaveCount(4);
        detalle.Count(l => l.EsHospedaje).Should().Be(1);
        detalle.Where(l => !l.EsHospedaje).Sum(l => l.Subtotal).Should().Be(52.50m);
    }

    [Fact]
    public void DecoradorConCantidad_MultiplicaPorElPrecioUnitario()
    {
        var baseCuenta = new CuentaEstadia(TarifaTresNoches, "Habitación 204");

        var conLavanderia = new ConsumoLavanderia(baseCuenta, "2 prendas", 2, 3.00m, DateTime.UtcNow);

        conLavanderia.MontoPropio.Should().Be(6.00m);
        conLavanderia.ObtenerMonto().Should().Be(261.00m);
    }

    [Fact]
    public void Builder_EnsamblaLaFacturaDelWireframe()
    {
        var cuenta = EnsambladorCuenta.Ensamblar(
            TarifaTresNoches, "Habitación 204 — Doble – Vista al mar", ConsumosDelWireframe());

        var factura = new FacturaBuilder()
            .ConNumero("FAC-000001")
            .ParaEstadia(estadiaId: 1, clienteId: 1)
            .EmitidaPor(usuarioId: 1, DateTime.UtcNow)
            .ConMontoHospedaje(TarifaTresNoches, "Habitación 204 — Doble – Vista al mar")
            .ConConsumos(cuenta)
            .ConDescuento(10.00m, "Cortesía por demora en el check-in")
            .ConPago(MetodoPago.TarjetaCredito, EstadoPago.Pagado)
            .Construir();

        factura.SubtotalHospedaje.Should().Be(255.00m);
        factura.SubtotalConsumos.Should().Be(52.50m);
        factura.Descuento.Should().Be(10.00m);
        factura.Total.Should().Be(297.50m);
        factura.Detalles.Should().HaveCount(4);
        factura.EstadoPago.Should().Be(EstadoPago.Pagado);
    }

    [Fact]
    public void Builder_ExigeJustificacionAlAplicarDescuento()
    {
        var constructor = new FacturaBuilder();

        var accion = () => constructor.ConDescuento(10.00m, string.Empty);

        accion.Should().Throw<ExcepcionReglaNegocio>();
    }

    [Fact]
    public void Builder_RechazaDescuentoSuperiorAlMontoFacturado()
    {
        var constructor = new FacturaBuilder()
            .ConNumero("FAC-000002")
            .ParaEstadia(1, 1)
            .ConMontoHospedaje(TarifaTresNoches, "Habitación 204")
            .ConDescuento(500.00m, "Descuento excesivo");

        var accion = () => constructor.Construir();

        accion.Should().Throw<ExcepcionReglaNegocio>();
    }

    [Fact]
    public void Builder_ExigeElDetalleDeHospedaje()
    {
        var constructor = new FacturaBuilder()
            .ConNumero("FAC-000003")
            .ParaEstadia(1, 1);

        var accion = () => constructor.Construir();

        accion.Should().Throw<ExcepcionReglaNegocio>();
    }

    [Fact]
    public void Builder_ExigeNumeroDeComprobante()
    {
        var constructor = new FacturaBuilder()
            .ParaEstadia(1, 1)
            .ConMontoHospedaje(TarifaTresNoches, "Habitación 204");

        var accion = () => constructor.Construir();

        accion.Should().Throw<ExcepcionReglaNegocio>();
    }

    /// <summary>Consumos exactos que muestra el wireframe de facturación.</summary>
    private static List<Consumo> ConsumosDelWireframe() =>
    [
        CrearConsumo(TipoServicioAdicional.Restaurante, "Cena - mesa 4", 1, 28.50m, 20),
        CrearConsumo(TipoServicioAdicional.Lavanderia, "2 prendas", 2, 3.00m, 21),
        CrearConsumo(TipoServicioAdicional.Transporte, "Aeropuerto - hotel", 1, 18.00m, 20)
    ];

    private static Consumo CrearConsumo(
        TipoServicioAdicional tipo, string descripcion, int cantidad, decimal precio, int dia) => new()
    {
        Descripcion = descripcion,
        Cantidad = cantidad,
        PrecioUnitario = precio,
        FechaConsumo = new DateTime(2026, 9, dia),
        ServicioAdicional = new ServicioAdicional { Nombre = tipo.ToString(), Tipo = tipo }
    };
}
