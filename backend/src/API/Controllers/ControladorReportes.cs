using Asp.Versioning;
using HotelParadiseResort.Application.DTOs.Reportes;
using HotelParadiseResort.Application.Fachada;
using HotelParadiseResort.Shared.Constantes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelParadiseResort.API.Controllers;

/// <summary>
/// COMPONENTE 6 — Reportes Administrativos (RF09).
///
/// Reservado al rol Administrador: la Etapa 1 aplica el principio ISP separando la
/// reportería macro de las funciones de Recepción.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reportes")]
[Authorize(Roles = RolesSistema.Administrador)]
public sealed class ControladorReportes : ControladorBase
{
    private readonly IFachadaServiciosHotel _hotel;

    public ControladorReportes(IFachadaServiciosHotel hotel) => _hotel = hotel;

    /// <summary>Porcentaje de ocupación y noches vendidas del período.</summary>
    [HttpGet("ocupacion")]
    [ProducesResponseType(typeof(ReporteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ocupacion(
        [FromQuery] DateTime fechaDesde,
        [FromQuery] DateTime fechaHasta,
        [FromQuery] int? tipoHabitacionId,
        CancellationToken cancelacion) =>
        Responder(await _hotel.Reportes.GenerarReporteOcupacionAsync(
            new FiltroReporteDto(fechaDesde, fechaHasta, tipoHabitacionId), cancelacion));

    /// <summary>Ingresos separados por hospedaje y servicios adicionales.</summary>
    [HttpGet("ingresos")]
    [ProducesResponseType(typeof(ReporteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ingresos(
        [FromQuery] DateTime fechaDesde,
        [FromQuery] DateTime fechaHasta,
        [FromQuery] int? tipoHabitacionId,
        CancellationToken cancelacion) =>
        Responder(await _hotel.Reportes.GenerarReporteIngresosAsync(
            new FiltroReporteDto(fechaDesde, fechaHasta, tipoHabitacionId), cancelacion));

    /// <summary>Demanda agrupada por mes para identificar temporadas altas.</summary>
    [HttpGet("temporadas")]
    [ProducesResponseType(typeof(ReporteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Temporadas(
        [FromQuery] DateTime fechaDesde,
        [FromQuery] DateTime fechaHasta,
        [FromQuery] int? tipoHabitacionId,
        CancellationToken cancelacion) =>
        Responder(await _hotel.Reportes.GenerarReporteTemporadaAsync(
            new FiltroReporteDto(fechaDesde, fechaHasta, tipoHabitacionId), cancelacion));
}

/// <summary>Indicadores del panel principal. Accesible a todo el personal.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/panel")]
[Authorize(Roles = RolesSistema.Empleado)]
public sealed class ControladorPanel : ControladorBase
{
    private readonly IFachadaServiciosHotel _hotel;

    public ControladorPanel(IFachadaServiciosHotel hotel) => _hotel = hotel;

    /// <summary>Tarjetas KPI y conteos por estado de habitación.</summary>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenPanelDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerResumen(CancellationToken cancelacion) =>
        Responder(await _hotel.Reportes.ObtenerResumenPanelAsync(cancelacion));
}
