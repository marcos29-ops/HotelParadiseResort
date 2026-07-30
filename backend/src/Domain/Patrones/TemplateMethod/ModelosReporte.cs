using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Domain.Patrones.TemplateMethod;

/// <summary>Filtros del reporte, tomados de la pantalla de reportes administrativos.</summary>
public sealed class ParametrosReporte
{
    public required DateTime FechaDesde { get; init; }

    public required DateTime FechaHasta { get; init; }

    /// <summary>Acota a un tipo de habitación concreto. Nulo significa "Todos".</summary>
    public int? TipoHabitacionId { get; init; }

    /// <summary>Total de habitaciones activas, necesario para el porcentaje de ocupación.</summary>
    public int TotalHabitaciones { get; init; }
}

/// <summary>
/// Registro normalizado sobre el que operan los tres reportes. Homogeneizar la entrada
/// permite que el método plantilla filtre y agrupe sin conocer el origen de los datos.
/// </summary>
public sealed record RegistroReporte(
    DateTime Fecha,
    decimal Valor,
    decimal ValorSecundario,
    int? TipoHabitacionId,
    string? Etiqueta);

/// <summary>Indicador numérico destacado del reporte (las tarjetas KPI de la pantalla).</summary>
public sealed record IndicadorReporte(
    string Nombre,
    decimal Valor,
    string Formato,
    string? Descripcion);

/// <summary>Punto del gráfico comparativo por período.</summary>
public sealed record PuntoSerie(
    string Etiqueta,
    decimal Valor,
    decimal ValorSecundario);

/// <summary>Salida uniforme de cualquier reporte generado.</summary>
public sealed record ResultadoReporte(
    TipoReporte Tipo,
    string Titulo,
    DateTime FechaDesde,
    DateTime FechaHasta,
    IReadOnlyList<IndicadorReporte> Indicadores,
    IReadOnlyList<PuntoSerie> Series,
    DateTime FechaGeneracion);
