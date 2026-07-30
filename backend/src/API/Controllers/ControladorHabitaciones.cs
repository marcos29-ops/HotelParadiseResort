using Asp.Versioning;
using HotelParadiseResort.Application.DTOs.Habitaciones;
using HotelParadiseResort.Application.Fachada;
using HotelParadiseResort.Shared.Constantes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelParadiseResort.API.Controllers;

/// <summary>COMPONENTE 2 — Gestión de Habitaciones y Disponibilidad (RF02, RF03).</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/habitaciones")]
[Authorize(Roles = RolesSistema.Empleado)]
public sealed class ControladorHabitaciones : ControladorBase
{
    private readonly IFachadaServiciosHotel _hotel;

    public ControladorHabitaciones(IFachadaServiciosHotel hotel) => _hotel = hotel;

    /// <summary>Inventario completo con su estado actual (grid del panel principal).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HabitacionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancelacion) =>
        Responder(await _hotel.Habitaciones.ListarAsync(cancelacion));

    /// <summary>Obtiene una habitación por su identificador.</summary>
    [HttpGet("{id:int}", Name = nameof(ObtenerHabitacion))]
    [ProducesResponseType(typeof(HabitacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerHabitacion(int id, CancellationToken cancelacion) =>
        Responder(await _hotel.Habitaciones.ObtenerPorIdAsync(id, cancelacion));

    /// <summary>RF03 — Habitaciones libres en el rango, con la tarifa ya calculada.</summary>
    [HttpGet("disponibilidad")]
    [ProducesResponseType(typeof(IReadOnlyList<HabitacionDisponibleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConsultarDisponibilidad(
        [FromQuery] DateTime fechaEntrada,
        [FromQuery] DateTime fechaSalida,
        [FromQuery] int? tipoHabitacionId,
        [FromQuery] int? cantidadHuespedes,
        CancellationToken cancelacion) =>
        Responder(await _hotel.Habitaciones.ConsultarDisponibilidadAsync(
            new ConsultaDisponibilidadDto(fechaEntrada, fechaSalida, tipoHabitacionId, cantidadHuespedes),
            cancelacion));

    /// <summary>Registra una habitación nueva en el inventario.</summary>
    [HttpPost]
    [Authorize(Roles = RolesSistema.Administrador)]
    [ProducesResponseType(typeof(HabitacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearHabitacionDto solicitud, CancellationToken cancelacion)
    {
        var resultado = await _hotel.Habitaciones.CrearAsync(solicitud, cancelacion);

        return ResponderCreado(resultado, nameof(ObtenerHabitacion), new { id = resultado.Valor?.Id });
    }

    /// <summary>Actualiza los datos de una habitación.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Administrador)]
    [ProducesResponseType(typeof(HabitacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarHabitacionDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Habitaciones.ActualizarAsync(id, solicitud, cancelacion));

    /// <summary>
    /// Cambia el estado de la habitación. La transición la valida el patrón State; una
    /// transición no permitida devuelve 422.
    /// </summary>
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(HabitacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CambiarEstado(
        int id, [FromBody] CambiarEstadoHabitacionDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Habitaciones.CambiarEstadoAsync(id, solicitud, cancelacion));
}

/// <summary>Catálogo de tipos de habitación y sus tarifas base.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tipos-habitacion")]
[Authorize(Roles = RolesSistema.Empleado)]
public sealed class ControladorTiposHabitacion : ControladorBase
{
    private readonly IFachadaServiciosHotel _hotel;

    public ControladorTiposHabitacion(IFachadaServiciosHotel hotel) => _hotel = hotel;

    /// <summary>Lista los tipos de habitación disponibles.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TipoHabitacionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancelacion) =>
        Responder(await _hotel.Habitaciones.ListarTiposAsync(cancelacion));

    /// <summary>Crea un tipo de habitación.</summary>
    [HttpPost]
    [Authorize(Roles = RolesSistema.Administrador)]
    [ProducesResponseType(typeof(TipoHabitacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearTipoHabitacionDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Habitaciones.CrearTipoAsync(solicitud, cancelacion));

    /// <summary>Actualiza un tipo de habitación.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Administrador)]
    [ProducesResponseType(typeof(TipoHabitacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarTipoHabitacionDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Habitaciones.ActualizarTipoAsync(id, solicitud, cancelacion));
}
