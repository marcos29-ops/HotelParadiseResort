using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Domain.Patrones.TemplateMethod;

/// <summary>
/// PATRÓN TEMPLATE METHOD — Reporte de ocupación.
///
/// Sobre el esqueleto de <see cref="ReporteBase"/> aporta su cálculo específico: el
/// porcentaje de ocupación del hotel y el promedio de noches por estadía en el período.
/// Cada registro representa una noche-habitación ocupada.
/// </summary>
public sealed class ReporteOcupacion : ReporteBase
{
    private readonly IReadOnlyList<RegistroReporte> _origen;

    public ReporteOcupacion(IReadOnlyList<RegistroReporte> origen) => _origen = origen;

    public override TipoReporte Tipo => TipoReporte.Ocupacion;

    public override string Titulo => "Reporte de ocupación";

    protected override IReadOnlyList<RegistroReporte> ObtenerDatos(ParametrosReporte parametros) => _origen;

    protected override IReadOnlyList<IndicadorReporte> Calcular(
        IReadOnlyList<RegistroReporte> datos, ParametrosReporte parametros)
    {
        var nochesOcupadas = datos.Sum(d => d.Valor);
        var diasPeriodo = (parametros.FechaHasta.Date - parametros.FechaDesde.Date).Days + 1;
        var nochesDisponibles = (decimal)parametros.TotalHabitaciones * diasPeriodo;

        var porcentajeOcupacion = nochesDisponibles > 0
            ? decimal.Round(nochesOcupadas / nochesDisponibles * 100, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var estadias = datos.Select(d => d.Etiqueta).Distinct().Count();
        var promedioNoches = estadias > 0
            ? decimal.Round(nochesOcupadas / estadias, 2, MidpointRounding.AwayFromZero)
            : 0m;

        return
        [
            new IndicadorReporte("Porcentaje de ocupación", porcentajeOcupacion, "porcentaje",
                $"Sobre {parametros.TotalHabitaciones} habitaciones en {diasPeriodo} día(s)"),
            new IndicadorReporte("Noches ocupadas", nochesOcupadas, "entero",
                "Total de noches-habitación vendidas"),
            new IndicadorReporte("Estadías registradas", estadias, "entero",
                "Cantidad de estadías del período"),
            new IndicadorReporte("Promedio de noches", promedioNoches, "decimal",
                "Duración media de la estadía")
        ];
    }
}
