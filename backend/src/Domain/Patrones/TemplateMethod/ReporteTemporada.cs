using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Domain.Patrones.TemplateMethod;

/// <summary>
/// PATRÓN TEMPLATE METHOD — Reporte de temporadas de mayor demanda.
///
/// Responde a la necesidad del enunciado de identificar los períodos de mayor
/// afluencia. Redefine <see cref="ConstruirSeries"/> porque, a diferencia de los otros
/// dos reportes, agrupa por mes y no por semana: una temporada no se aprecia en una
/// ventana semanal. Cada registro es una reserva del período.
/// </summary>
public sealed class ReporteTemporada : ReporteBase
{
    private readonly IReadOnlyList<RegistroReporte> _origen;

    public ReporteTemporada(IReadOnlyList<RegistroReporte> origen) => _origen = origen;

    public override TipoReporte Tipo => TipoReporte.Temporada;

    public override string Titulo => "Reporte de temporadas de mayor demanda";

    protected override IReadOnlyList<RegistroReporte> ObtenerDatos(ParametrosReporte parametros) => _origen;

    protected override IReadOnlyList<IndicadorReporte> Calcular(
        IReadOnlyList<RegistroReporte> datos, ParametrosReporte parametros)
    {
        var porMes = datos
            .GroupBy(d => new { d.Fecha.Year, d.Fecha.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Reservas = g.Sum(d => d.Valor),
                Ingresos = g.Sum(d => d.ValorSecundario)
            })
            .OrderByDescending(x => x.Reservas)
            .ToList();

        var mesPico = porMes.FirstOrDefault();
        var mesValle = porMes.LastOrDefault();
        var totalReservas = datos.Sum(d => d.Valor);
        var promedioMensual = porMes.Count > 0
            ? decimal.Round(totalReservas / porMes.Count, 2, MidpointRounding.AwayFromZero)
            : 0m;

        return
        [
            new IndicadorReporte("Total de reservas", totalReservas, "entero",
                "Reservas registradas en el período"),
            new IndicadorReporte("Mes de mayor demanda",
                mesPico?.Reservas ?? 0, "entero",
                mesPico is null ? "Sin datos" : NombreMes(mesPico.Year, mesPico.Month)),
            new IndicadorReporte("Mes de menor demanda",
                mesValle?.Reservas ?? 0, "entero",
                mesValle is null ? "Sin datos" : NombreMes(mesValle.Year, mesValle.Month)),
            new IndicadorReporte("Promedio mensual", promedioMensual, "decimal",
                $"Sobre {porMes.Count} mes(es) con actividad")
        ];
    }

    /// <summary>Agrupa por mes: la estacionalidad no se observa en una serie semanal.</summary>
    protected override IReadOnlyList<PuntoSerie> ConstruirSeries(
        IReadOnlyList<RegistroReporte> datos, ParametrosReporte parametros) =>
        datos.GroupBy(d => new { d.Fecha.Year, d.Fecha.Month })
             .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
             .Select(g => new PuntoSerie(
                 Etiqueta: NombreMes(g.Key.Year, g.Key.Month),
                 Valor: decimal.Round(g.Sum(d => d.Valor), 2, MidpointRounding.AwayFromZero),
                 ValorSecundario: decimal.Round(g.Sum(d => d.ValorSecundario), 2, MidpointRounding.AwayFromZero)))
             .ToList();

    private static string NombreMes(int anio, int mes)
    {
        var nombre = CulturaEspanol.DateTimeFormat.GetMonthName(mes);
        return $"{CulturaEspanol.TextInfo.ToTitleCase(nombre)} {anio}";
    }
}
