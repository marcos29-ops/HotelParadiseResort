using FluentAssertions;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.TemplateMethod;
using HotelParadiseResort.Shared.Excepciones;

namespace HotelParadiseResort.Tests.Dominio;

/// <summary>
/// PATRÓN TEMPLATE METHOD — Generación de reportes.
/// Comprueba que los tres reportes compartan el esqueleto y difieran solo en su cálculo.
/// </summary>
public sealed class PruebasReportes
{
    private static readonly ParametrosReporte ParametrosJulio = new()
    {
        FechaDesde = new DateTime(2026, 7, 1),
        FechaHasta = new DateTime(2026, 7, 31),
        TotalHabitaciones = 60
    };

    [Fact]
    public void ReporteOcupacion_CalculaElPorcentajeSobreElInventario()
    {
        // 3 estadías de 10 noches = 30 noches sobre 60 habitaciones × 31 días = 1860.
        var registros = Enumerable.Range(0, 3)
            .Select(i => new RegistroReporte(new DateTime(2026, 7, 5 + i), 10, 2, null, $"EST-{i}"))
            .ToList();

        var resultado = new ReporteOcupacion(registros).Generar(ParametrosJulio);

        resultado.Tipo.Should().Be(TipoReporte.Ocupacion);
        var porcentaje = resultado.Indicadores.First(i => i.Nombre == "Porcentaje de ocupación");
        porcentaje.Valor.Should().BeApproximately(1.61m, 0.01m);
        resultado.Indicadores.First(i => i.Nombre == "Noches ocupadas").Valor.Should().Be(30);
    }

    [Fact]
    public void ReporteIngresos_SeparaHospedajeDeServicios()
    {
        var registros = new List<RegistroReporte>
        {
            new(new DateTime(2026, 7, 5), 255.00m, 52.50m, null, "FAC-000001"),
            new(new DateTime(2026, 7, 12), 170.00m, 20.00m, null, "FAC-000002")
        };

        var resultado = new ReporteIngresos(registros).Generar(ParametrosJulio);

        resultado.Indicadores.First(i => i.Nombre == "Ingresos por hospedaje").Valor.Should().Be(425.00m);
        resultado.Indicadores.First(i => i.Nombre == "Ingresos por servicios").Valor.Should().Be(72.50m);
        resultado.Indicadores.First(i => i.Nombre == "Ingresos totales del período").Valor.Should().Be(497.50m);
    }

    [Fact]
    public void ReporteTemporada_AgrupaPorMesYNoPorSemana()
    {
        var registros = new List<RegistroReporte>
        {
            new(new DateTime(2026, 7, 5), 1, 255m, null, "RES-1"),
            new(new DateTime(2026, 7, 20), 1, 255m, null, "RES-2"),
            new(new DateTime(2026, 8, 3), 1, 300m, null, "RES-3")
        };

        var parametros = new ParametrosReporte
        {
            FechaDesde = new DateTime(2026, 7, 1),
            FechaHasta = new DateTime(2026, 8, 31)
        };

        var resultado = new ReporteTemporada(registros).Generar(parametros);

        // Dos meses con actividad, no nueve semanas.
        resultado.Series.Should().HaveCount(2);
        resultado.Series.First().Etiqueta.Should().Contain("2026");
        resultado.Indicadores.First(i => i.Nombre == "Total de reservas").Valor.Should().Be(3);
    }

    [Fact]
    public void MetodoPlantilla_FiltraLosRegistrosFueraDelRango()
    {
        var registros = new List<RegistroReporte>
        {
            new(new DateTime(2026, 7, 15), 5, 0, null, "dentro"),
            new(new DateTime(2026, 6, 15), 5, 0, null, "fuera-antes"),
            new(new DateTime(2026, 8, 15), 5, 0, null, "fuera-despues")
        };

        var resultado = new ReporteOcupacion(registros).Generar(ParametrosJulio);

        resultado.Indicadores.First(i => i.Nombre == "Noches ocupadas").Valor.Should().Be(5);
    }

    [Fact]
    public void MetodoPlantilla_RechazaUnRangoInvertido()
    {
        var parametros = new ParametrosReporte
        {
            FechaDesde = new DateTime(2026, 7, 31),
            FechaHasta = new DateTime(2026, 7, 1)
        };

        var accion = () => new ReporteOcupacion([]).Generar(parametros);

        accion.Should().Throw<ExcepcionReglaNegocio>();
    }

    [Fact]
    public void LosTresReportes_CompartenLaEstructuraDeSalida()
    {
        ReporteBase[] reportes = [new ReporteOcupacion([]), new ReporteIngresos([]), new ReporteTemporada([])];

        foreach (var reporte in reportes)
        {
            var resultado = reporte.Generar(ParametrosJulio);

            resultado.Titulo.Should().NotBeNullOrWhiteSpace();
            resultado.Indicadores.Should().NotBeEmpty();
            resultado.FechaDesde.Should().Be(ParametrosJulio.FechaDesde);
            resultado.FechaHasta.Should().Be(ParametrosJulio.FechaHasta);
        }
    }
}
