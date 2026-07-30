using Asp.Versioning;
using HotelParadiseResort.Application.DTOs.Reservas;
using HotelParadiseResort.Application.Fachada;
using HotelParadiseResort.Shared.Constantes;
using HotelParadiseResort.Shared.Paginacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelParadiseResort.API.Controllers;

/// <summary>COMPONENTE 3 — Gestión de Reservas (RF04).</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reservas")]
[Authorize(Roles = RolesSistema.Empleado)]
public sealed class ControladorReservas : ControladorBase
{
    private readonly IFachadaServiciosHotel _hotel;
    private readonly FachadaServiciosHotel _fachadaCompuesta;

    public ControladorReservas(IFachadaServiciosHotel hotel, FachadaServiciosHotel fachadaCompuesta)
    {
        _hotel = hotel;
        _fachadaCompuesta = fachadaCompuesta;
    }

    /// <summary>Listado paginado con filtros por estado y rango de fechas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<ReservaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] ParametrosPaginacion parametros,
        [FromQuery] string? estado,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        CancellationToken cancelacion) =>
        Responder(await _hotel.Reservas.ListarAsync(parametros, estado, desde, hasta, cancelacion));

    /// <summary>Obtiene una reserva por su identificador.</summary>
    [HttpGet("{id:int}", Name = nameof(ObtenerReserva))]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerReserva(int id, CancellationToken cancelacion) =>
        Responder(await _hotel.Reservas.ObtenerPorIdAsync(id, cancelacion));

    /// <summary>Reservas confirmadas con entrada prevista para la fecha indicada.</summary>
    [HttpGet("llegadas")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConsultarLlegadas(
        [FromQuery] DateTime? fecha, CancellationToken cancelacion) =>
        Responder(await _hotel.Reservas.ConsultarLlegadasDelDiaAsync(fecha, cancelacion));

    /// <summary>
    /// RF04 — Crea la reserva verificando la disponibilidad de forma transaccional.
    /// Con <paramref name="confirmar"/> en true se usa la operación compuesta de la
    /// fachada, que crea y confirma en un solo paso (botón "Confirmar reserva").
    /// Devuelve 409 si la habitación no está disponible en el rango.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearReservaDto solicitud,
        [FromQuery] bool confirmar,
        CancellationToken cancelacion)
    {
        var resultado = await _fachadaCompuesta.CrearReservaAsync(solicitud, confirmar, cancelacion);

        return ResponderCreado(resultado, nameof(ObtenerReserva), new { id = resultado.Valor?.Id });
    }

    /// <summary>Modifica una reserva que aún admite cambios.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarReservaDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Reservas.ActualizarAsync(id, solicitud, cancelacion));

    /// <summary>Confirma una reserva pendiente, habilitando su check-in.</summary>
    [HttpPost("{id:int}/confirmar")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Confirmar(int id, CancellationToken cancelacion) =>
        Responder(await _hotel.Reservas.ConfirmarAsync(id, cancelacion));

    /// <summary>Cancela la reserva y libera la habitación.</summary>
    [HttpPost("{id:int}/cancelar")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancelar(
        int id, [FromBody] CancelarReservaDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Reservas.CancelarAsync(id, solicitud, cancelacion));
}
