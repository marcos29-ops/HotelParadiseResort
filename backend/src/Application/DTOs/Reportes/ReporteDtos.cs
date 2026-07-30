namespace HotelParadiseResort.Application.DTOs.Reportes;

/// <summary>Filtros de la pantalla de reportes administrativos.</summary>
public sealed record FiltroReporteDto(
    DateTime FechaDesde,
    DateTime FechaHasta,
    int? TipoHabitacionId);

/// <summary>Salida uniforme de cualquiera de los tres reportes.</summary>
public sealed record ReporteDto(
    string Tipo,
    string Titulo,
    DateTime FechaDesde,
    DateTime FechaHasta,
    DateTime FechaGeneracion,
    IReadOnlyList<IndicadorDto> Indicadores,
    IReadOnlyList<PuntoSerieDto> Series);

/// <summary>Tarjeta KPI del reporte.</summary>
public sealed record IndicadorDto(
    string Nombre,
    decimal Valor,
    string Formato,
    string? Descripcion);

/// <summary>Punto del gráfico comparativo.</summary>
public sealed record PuntoSerieDto(
    string Etiqueta,
    decimal Valor,
    decimal ValorSecundario);

/// <summary>
/// Indicadores del panel principal. Reproduce las tarjetas del wireframe: ocupadas
/// sobre el total, disponibles, reservadas hoy y en mantenimiento.
/// </summary>
public sealed record ResumenPanelDto(
    int TotalHabitaciones,
    int HabitacionesOcupadas,
    int HabitacionesDisponibles,
    int HabitacionesReservadas,
    int HabitacionesEnLimpieza,
    int HabitacionesEnMantenimiento,
    int ReservasActivas,
    int LlegadasHoy,
    int CheckInsHoy,
    int CheckOutsHoy,
    decimal IngresosHoy,
    decimal PorcentajeOcupacion);
