using System.Globalization;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Shared.Excepciones;

namespace HotelParadiseResort.Domain.Patrones.TemplateMethod;

/// <summary>
/// PATRÓN TEMPLATE METHOD — Esqueleto común de los reportes administrativos.
///
/// La Etapa 2 observa que ocupación, ingresos y temporada siguen la misma secuencia
/// —traer datos, filtrar, calcular, dar formato— pero calculan cosas distintas.
/// <see cref="Generar"/> fija ese orden y es <c>sealed</c>: las subclases aportan
/// únicamente su parte específica.
/// </summary>
public abstract class ReporteBase
{
    /// <summary>Cultura de los rótulos del gráfico y de los nombres de mes.</summary>
    protected static readonly CultureInfo CulturaEspanol = new("es-CR");

    public abstract TipoReporte Tipo { get; }

    public abstract string Titulo { get; }

    /// <summary>
    /// MÉTODO PLANTILLA. Define la secuencia invariable de generación. No se
    /// sobrescribe: garantiza que todo reporte valide su rango y produzca la misma
    /// estructura de salida.
    /// </summary>
    public ResultadoReporte Generar(ParametrosReporte parametros)
    {
        ValidarRango(parametros);

        var datos = ObtenerDatos(parametros);
        var datosFiltrados = Filtrar(datos, parametros);
        var indicadores = Calcular(datosFiltrados, parametros);
        var series = ConstruirSeries(datosFiltrados, parametros);

        return DarFormato(indicadores, series, parametros);
    }

    /// <summary>Paso 1 — Obtención de la información base. Lo aporta cada reporte.</summary>
    protected abstract IReadOnlyList<RegistroReporte> ObtenerDatos(ParametrosReporte parametros);

    /// <summary>Paso 3 — Indicadores propios del reporte.</summary>
    protected abstract IReadOnlyList<IndicadorReporte> Calcular(
        IReadOnlyList<RegistroReporte> datos, ParametrosReporte parametros);

    /// <summary>
    /// Paso 2 — Filtrado. La implementación por defecto acota al rango de fechas, que
    /// es lo que necesitan los tres reportes; puede refinarse si un reporte lo requiere.
    /// </summary>
    protected virtual IReadOnlyList<RegistroReporte> Filtrar(
        IReadOnlyList<RegistroReporte> datos, ParametrosReporte parametros) =>
        datos.Where(d => d.Fecha.Date >= parametros.FechaDesde.Date &&
                         d.Fecha.Date <= parametros.FechaHasta.Date)
             .ToList();

    /// <summary>
    /// Paso 4 — Serie temporal para el gráfico. Agrupa por semana, como muestra el
    /// wireframe de reportes.
    ///
    /// Se emiten **todas** las semanas del rango, incluidas las que no registran
    /// actividad: un gráfico que salta directamente a "Sem 5" sin mostrar las
    /// anteriores no comunica la evolución del período. Cada columna se rotula con la
    /// fecha en que arranca la semana, que es más legible que un número correlativo.
    /// </summary>
    protected virtual IReadOnlyList<PuntoSerie> ConstruirSeries(
        IReadOnlyList<RegistroReporte> datos, ParametrosReporte parametros)
    {
        var agrupados = datos
            .GroupBy(d => ObtenerNumeroSemana(d.Fecha, parametros.FechaDesde))
            .ToDictionary(
                g => g.Key,
                g => (
                    Valor: decimal.Round(g.Sum(d => d.Valor), 2, MidpointRounding.AwayFromZero),
                    Secundario: decimal.Round(g.Sum(d => d.ValorSecundario), 2, MidpointRounding.AwayFromZero)));

        var dias = (parametros.FechaHasta.Date - parametros.FechaDesde.Date).Days;
        var totalSemanas = Math.Max(1, (dias / 7) + 1);

        var series = new List<PuntoSerie>(totalSemanas);

        for (var semana = 1; semana <= totalSemanas; semana++)
        {
            var inicioSemana = parametros.FechaDesde.Date.AddDays((semana - 1) * 7);
            var totales = agrupados.GetValueOrDefault(semana);

            series.Add(new PuntoSerie(
                Etiqueta: EtiquetaSemana(inicioSemana),
                Valor: totales.Valor,
                ValorSecundario: totales.Secundario));
        }

        return series;
    }

    private static string EtiquetaSemana(DateTime inicio) =>
        inicio.ToString("dd MMM", CulturaEspanol);

    /// <summary>Paso 5 — Empaquetado final del resultado.</summary>
    protected virtual ResultadoReporte DarFormato(
        IReadOnlyList<IndicadorReporte> indicadores,
        IReadOnlyList<PuntoSerie> series,
        ParametrosReporte parametros) =>
        new(Tipo, Titulo, parametros.FechaDesde, parametros.FechaHasta, indicadores, series, DateTime.UtcNow);

    private static void ValidarRango(ParametrosReporte parametros)
    {
        if (parametros.FechaHasta.Date < parametros.FechaDesde.Date)
        {
            throw new ExcepcionReglaNegocio(
                "La fecha final del reporte no puede ser anterior a la inicial.");
        }
    }

    private static int ObtenerNumeroSemana(DateTime fecha, DateTime inicio) =>
        ((fecha.Date - inicio.Date).Days / 7) + 1;
}
