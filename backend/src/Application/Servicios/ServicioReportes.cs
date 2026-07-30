using HotelParadiseResort.Application.Abstracciones;
using HotelParadiseResort.Application.DTOs.Reportes;
using HotelParadiseResort.Application.Servicios.Interfaces;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.TemplateMethod;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Shared.Excepciones;
using HotelParadiseResort.Shared.Resultados;
using Microsoft.Extensions.Logging;

namespace HotelParadiseResort.Application.Servicios;

/// <summary>
/// COMPONENTE 6 — Reportes Administrativos (RF09).
///
/// Genera los informes de ocupación, ingresos y temporadas a partir de la información
/// histórica. Aplica el patrón Template Method: este servicio prepara los datos y
/// delega el esqueleto de generación en las subclases de <see cref="ReporteBase"/>.
/// </summary>
public sealed class ServicioReportes : IServicioReportes
{
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IProveedorFechaHora _reloj;
    private readonly ILogger<ServicioReportes> _registro;

    public ServicioReportes(
        IUnidadDeTrabajo unidadDeTrabajo,
        IProveedorFechaHora reloj,
        ILogger<ServicioReportes> registro)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
        _registro = registro;
    }

    /// <summary>
    /// Ocupación: cada estadía aporta sus noches como noche-habitación vendida.
    /// </summary>
    public async Task<Resultado<ReporteDto>> GenerarReporteOcupacionAsync(
        FiltroReporteDto filtro, CancellationToken cancelacion = default)
    {
        var validacion = ValidarFiltro(filtro);
        if (validacion.EsFallido)
        {
            return Resultado.Fallo<ReporteDto>(validacion.Error!, validacion.TipoError);
        }

        var estadias = await _unidadDeTrabajo.Estadias.ObtenerEnRangoAsync(
            filtro.FechaDesde, filtro.FechaHasta, filtro.TipoHabitacionId, cancelacion);

        var registros = estadias
            .Select(e => new RegistroReporte(
                Fecha: e.FechaCheckIn.Date,
                Valor: e.CalcularNoches(),
                ValorSecundario: e.CantidadHuespedes,
                TipoHabitacionId: e.Habitacion?.TipoHabitacionId,
                Etiqueta: $"EST-{e.Id}"))
            .ToList();

        var totalHabitaciones = await _unidadDeTrabajo.Habitaciones.ContarActivasAsync(cancelacion);

        return Ejecutar(new ReporteOcupacion(registros), filtro, totalHabitaciones);
    }

    /// <summary>
    /// Ingresos: cada factura aporta su hospedaje y sus consumos por separado, ya
    /// prorrateado el descuento entre ambos conceptos.
    /// </summary>
    public async Task<Resultado<ReporteDto>> GenerarReporteIngresosAsync(
        FiltroReporteDto filtro, CancellationToken cancelacion = default)
    {
        var validacion = ValidarFiltro(filtro);
        if (validacion.EsFallido)
        {
            return Resultado.Fallo<ReporteDto>(validacion.Error!, validacion.TipoError);
        }

        var facturas = await _unidadDeTrabajo.Facturas.ObtenerEnRangoAsync(
            filtro.FechaDesde, filtro.FechaHasta, cancelacion);

        var registros = facturas
            .Select(f =>
            {
                // El descuento se reparte proporcionalmente entre hospedaje y consumos
                // para que la suma de ambas series coincida con el total facturado.
                var bruto = f.SubtotalHospedaje + f.SubtotalConsumos;
                var factor = bruto > 0 ? (bruto - f.Descuento) / bruto : 1m;

                return new RegistroReporte(
                    Fecha: f.FechaEmision.Date,
                    Valor: decimal.Round(f.SubtotalHospedaje * factor, 2, MidpointRounding.AwayFromZero),
                    ValorSecundario: decimal.Round(f.SubtotalConsumos * factor, 2, MidpointRounding.AwayFromZero),
                    TipoHabitacionId: null,
                    Etiqueta: f.Numero);
            })
            .ToList();

        return Ejecutar(new ReporteIngresos(registros), filtro, 0);
    }

    /// <summary>
    /// Temporadas: cada reserva cuenta como una unidad de demanda en su mes de entrada.
    /// </summary>
    public async Task<Resultado<ReporteDto>> GenerarReporteTemporadaAsync(
        FiltroReporteDto filtro, CancellationToken cancelacion = default)
    {
        var validacion = ValidarFiltro(filtro);
        if (validacion.EsFallido)
        {
            return Resultado.Fallo<ReporteDto>(validacion.Error!, validacion.TipoError);
        }

        var reservas = await _unidadDeTrabajo.Reservas.ObtenerEnRangoAsync(
            filtro.FechaDesde, filtro.FechaHasta, filtro.TipoHabitacionId, cancelacion);

        var registros = reservas
            .Select(r => new RegistroReporte(
                Fecha: r.FechaEntrada.Date,
                Valor: 1,
                ValorSecundario: r.MontoEstimado,
                TipoHabitacionId: r.Habitacion?.TipoHabitacionId,
                Etiqueta: r.Codigo))
            .ToList();

        return Ejecutar(new ReporteTemporada(registros), filtro, 0);
    }

    /// <summary>
    /// Indicadores del panel principal, con los conteos que muestran las tarjetas KPI
    /// y el grid de estado de habitaciones.
    /// </summary>
    public async Task<Resultado<ResumenPanelDto>> ObtenerResumenPanelAsync(
        CancellationToken cancelacion = default)
    {
        var hoy = _reloj.FechaOperativa;

        var porEstado = await _unidadDeTrabajo.Habitaciones.ContarPorEstadoAsync(cancelacion);
        var totalHabitaciones = await _unidadDeTrabajo.Habitaciones.ContarActivasAsync(cancelacion);

        var ocupadas = porEstado.GetValueOrDefault(TipoEstadoHabitacion.Ocupada);
        var disponibles = porEstado.GetValueOrDefault(TipoEstadoHabitacion.Disponible);
        var reservadas = porEstado.GetValueOrDefault(TipoEstadoHabitacion.Reservada);
        var enLimpieza = porEstado.GetValueOrDefault(TipoEstadoHabitacion.EnLimpieza);
        var enMantenimiento = porEstado.GetValueOrDefault(TipoEstadoHabitacion.EnMantenimiento);

        var reservasActivas = await _unidadDeTrabajo.Reservas
            .ContarPorEstadoAsync(TipoEstadoReserva.Confirmada, cancelacion);

        var llegadasHoy = await _unidadDeTrabajo.Reservas.ContarReservasDelDiaAsync(hoy, cancelacion);
        var checkInsHoy = await _unidadDeTrabajo.Estadias.ContarCheckInsDelDiaAsync(hoy, cancelacion);
        var checkOutsHoy = await _unidadDeTrabajo.Estadias.ContarCheckOutsDelDiaAsync(hoy, cancelacion);
        var ingresosHoy = await _unidadDeTrabajo.Facturas.ObtenerIngresosDelDiaAsync(hoy, cancelacion);

        var porcentajeOcupacion = totalHabitaciones > 0
            ? decimal.Round((decimal)ocupadas / totalHabitaciones * 100, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var resumen = new ResumenPanelDto(
            totalHabitaciones,
            ocupadas,
            disponibles,
            reservadas,
            enLimpieza,
            enMantenimiento,
            reservasActivas,
            llegadasHoy,
            checkInsHoy,
            checkOutsHoy,
            ingresosHoy,
            porcentajeOcupacion);

        return Resultado.Exitoso(resumen);
    }

    /// <summary>
    /// Invoca el método plantilla del reporte y traduce su salida al DTO de la API.
    /// </summary>
    private Resultado<ReporteDto> Ejecutar(
        ReporteBase reporte, FiltroReporteDto filtro, int totalHabitaciones)
    {
        var parametros = new ParametrosReporte
        {
            FechaDesde = filtro.FechaDesde,
            FechaHasta = filtro.FechaHasta,
            TipoHabitacionId = filtro.TipoHabitacionId,
            TotalHabitaciones = totalHabitaciones
        };

        try
        {
            var resultado = reporte.Generar(parametros);

            _registro.LogInformation(
                "Se generó el reporte {Tipo} para el rango {Desde:d} – {Hasta:d}.",
                resultado.Tipo, filtro.FechaDesde, filtro.FechaHasta);

            var dto = new ReporteDto(
                resultado.Tipo.ToString(),
                resultado.Titulo,
                resultado.FechaDesde,
                resultado.FechaHasta,
                resultado.FechaGeneracion,
                resultado.Indicadores
                    .Select(i => new IndicadorDto(i.Nombre, i.Valor, i.Formato, i.Descripcion))
                    .ToList(),
                resultado.Series
                    .Select(s => new PuntoSerieDto(s.Etiqueta, s.Valor, s.ValorSecundario))
                    .ToList());

            return Resultado.Exitoso(dto);
        }
        catch (ExcepcionReglaNegocio excepcion)
        {
            return Resultado.Fallo<ReporteDto>(excepcion.Message, TipoError.Validacion);
        }
    }

    private static Resultado ValidarFiltro(FiltroReporteDto filtro)
    {
        if (filtro.FechaHasta.Date < filtro.FechaDesde.Date)
        {
            return Resultado.Fallo(
                "La fecha final no puede ser anterior a la fecha inicial.", TipoError.Validacion);
        }

        // Acota el volumen de datos para sostener el tiempo de respuesta de RNF02.
        if ((filtro.FechaHasta.Date - filtro.FechaDesde.Date).Days > 366 * 3)
        {
            return Resultado.Fallo(
                "El rango del reporte no puede superar los 3 años.", TipoError.Validacion);
        }

        return Resultado.Exitoso();
    }
}
