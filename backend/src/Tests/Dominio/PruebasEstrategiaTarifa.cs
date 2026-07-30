using FluentAssertions;
using HotelParadiseResort.Application.Tarifas;
using HotelParadiseResort.Domain.Patrones.Strategy;
using HotelParadiseResort.Shared.Excepciones;

namespace HotelParadiseResort.Tests.Dominio;

/// <summary>
/// PATRÓN STRATEGY — Cálculo de tarifas.
/// El caso de referencia proviene del wireframe de la Etapa 2: 3 noches × $85.00 = $255.00.
/// </summary>
public sealed class PruebasEstrategiaTarifa
{
    private static readonly DateTime EntradaTemporadaNormal = new(2026, 9, 20);
    private static readonly DateTime SalidaTemporadaNormal = new(2026, 9, 23);

    [Fact]
    public void TarifaEstandar_ReproduceElCalculoDelWireframe()
    {
        var estrategia = new TarifaEstandar();

        var resultado = estrategia.Calcular(85.00m, EntradaTemporadaNormal, SalidaTemporadaNormal);

        resultado.Noches.Should().Be(3);
        resultado.TarifaPorNoche.Should().Be(85.00m);
        resultado.MontoTotal.Should().Be(255.00m);
    }

    [Fact]
    public void TarifaTemporadaAlta_AplicaElRecargoDelVeinticincoPorCiento()
    {
        var estrategia = new TarifaTemporadaAlta();

        var resultado = estrategia.Calcular(85.00m, new DateTime(2026, 12, 20), new DateTime(2026, 12, 23));

        resultado.Noches.Should().Be(3);
        resultado.TarifaPorNoche.Should().Be(106.25m);
        resultado.MontoTotal.Should().Be(318.75m);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    public void TemporadaAlta_AplicaEnSusMeses(int mes)
    {
        var estrategia = new TarifaTemporadaAlta();
        var entrada = new DateTime(2026, mes, 10);

        estrategia.AplicaA(entrada, entrada.AddDays(3)).Should().BeTrue();
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(9)]
    [InlineData(11)]
    public void TemporadaAlta_NoAplicaFueraDeSusMeses(int mes)
    {
        var estrategia = new TarifaTemporadaAlta();
        var entrada = new DateTime(2026, mes, 10);

        estrategia.AplicaA(entrada, entrada.AddDays(3)).Should().BeFalse();
    }

    [Fact]
    public void Selector_PrefiereTemporadaAltaCuandoCorresponde()
    {
        var selector = new SelectorEstrategiaTarifa([new TarifaEstandar(), new TarifaTemporadaAlta()]);

        var estrategia = selector.Seleccionar(new DateTime(2026, 12, 20), new DateTime(2026, 12, 23));

        estrategia.Codigo.Should().Be("TEMPORADA_ALTA");
    }

    [Fact]
    public void Selector_RecurreALaEstandarFueraDeTemporadaAlta()
    {
        var selector = new SelectorEstrategiaTarifa([new TarifaEstandar(), new TarifaTemporadaAlta()]);

        var estrategia = selector.Seleccionar(EntradaTemporadaNormal, SalidaTemporadaNormal);

        estrategia.Codigo.Should().Be("ESTANDAR");
    }

    [Fact]
    public void EstadiaDeUnSoloDia_SeCobraComoUnaNoche()
    {
        var noches = CalculadoraNoches.Calcular(new DateTime(2026, 9, 20), new DateTime(2026, 9, 20));

        noches.Should().Be(1);
    }

    [Fact]
    public void SalidaAnteriorALaEntrada_EsRechazada()
    {
        var accion = () => CalculadoraNoches.Calcular(new DateTime(2026, 9, 23), new DateTime(2026, 9, 20));

        accion.Should().Throw<ExcepcionReglaNegocio>();
    }
}
