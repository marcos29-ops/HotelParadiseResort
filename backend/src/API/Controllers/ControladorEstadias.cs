using Asp.Versioning;
using HotelParadiseResort.Application.DTOs.Estadias;
using HotelParadiseResort.Application.DTOs.Facturacion;
using HotelParadiseResort.Application.Fachada;
using HotelParadiseResort.Shared.Constantes;
using HotelParadiseResort.Shared.Paginacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelParadiseResort.API.Controllers;

/// <summary>COMPONENTE 4 — Gestión de Estadías y Consumos (RF05, RF06, RF07).</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/estadias")]
[Authorize(Roles = RolesSistema.Empleado)]
public sealed class ControladorEstadias : ControladorBase
{
    private readonly IFachadaServiciosHotel _hotel;
    private readonly FachadaServiciosHotel _fachadaCompuesta;

    public ControladorEstadias(IFachadaServiciosHotel hotel, FachadaServiciosHotel fachadaCompuesta)
    {
        _hotel = hotel;
        _fachadaCompuesta = fachadaCompuesta;
    }

    /// <summary>Listado paginado de estadías con filtro por estado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<EstadiaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] ParametrosPaginacion parametros,
        [FromQuery] string? estado,
        CancellationToken cancelacion) =>
        Responder(await _hotel.Estadias.ListarAsync(parametros, estado, cancelacion));

    /// <summary>Obtiene una estadía con sus consumos.</summary>
    [HttpGet("{id:int}", Name = nameof(ObtenerEstadia))]
    [ProducesResponseType(typeof(EstadiaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerEstadia(int id, CancellationToken cancelacion) =>
        Responder(await _hotel.Estadias.ObtenerPorIdAsync(id, cancelacion));

    /// <summary>Localiza la estadía en curso de una habitación (buscador de check-in/out).</summary>
    [HttpGet("por-habitacion/{numeroHabitacion}")]
    [ProducesResponseType(typeof(EstadiaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BuscarPorHabitacion(
        string numeroHabitacion, CancellationToken cancelacion) =>
        Responder(await _hotel.Estadias.BuscarActivaPorHabitacionAsync(numeroHabitacion, cancelacion));

    /// <summary>RF05 — Registra el check-in; la habitación pasa a "Ocupada".</summary>
    [HttpPost("check-in")]
    [ProducesResponseType(typeof(EstadiaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegistrarCheckIn(
        [FromBody] RegistrarCheckInDto solicitud, CancellationToken cancelacion)
    {
        var resultado = await _hotel.Estadias.RegistrarCheckInAsync(solicitud, cancelacion);

        return ResponderCreado(resultado, nameof(ObtenerEstadia), new { id = resultado.Valor?.Id });
    }

    /// <summary>RF06 — Cierra la estadía y la deja lista para facturación.</summary>
    [HttpPost("{id:int}/check-out")]
    [ProducesResponseType(typeof(EstadiaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarCheckOut(
        int id, [FromBody] RegistrarCheckOutDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Estadias.RegistrarCheckOutAsync(id, solicitud, cancelacion));

    /// <summary>
    /// Operación compuesta de la fachada: cierra la estadía y emite el comprobante en
    /// una sola llamada, reproduciendo el flujo del diagrama de secuencia de check-out.
    /// </summary>
    [HttpPost("{id:int}/check-out-y-facturar")]
    [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CerrarYFacturar(
        int id,
        [FromBody] SolicitudCheckOutFacturaDto solicitud,
        CancellationToken cancelacion) =>
        Responder(await _fachadaCompuesta.GenerarFacturaCheckOutAsync(
            id, solicitud.CheckOut, solicitud.Factura, cancelacion));

    /// <summary>Cuenta consolidada de la estadía (hospedaje + consumos).</summary>
    [HttpGet("{id:int}/cuenta")]
    [ProducesResponseType(typeof(CuentaEstadiaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarCuenta(int id, CancellationToken cancelacion) =>
        Responder(await _hotel.Estadias.ConsultarCuentaAsync(id, cancelacion));

    /// <summary>RF07 — Registra un consumo sobre la estadía.</summary>
    [HttpPost("{id:int}/consumos")]
    [ProducesResponseType(typeof(ConsumoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegistrarConsumo(
        int id, [FromBody] RegistrarConsumoDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Estadias.RegistrarConsumoAsync(id, solicitud, cancelacion));

    /// <summary>Elimina un consumo de una estadía todavía abierta.</summary>
    [HttpDelete("{id:int}/consumos/{consumoId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EliminarConsumo(
        int id, int consumoId, CancellationToken cancelacion) =>
        Responder(await _hotel.Estadias.EliminarConsumoAsync(id, consumoId, cancelacion));
}

/// <summary>Carga útil de la operación compuesta de check-out con facturación.</summary>
public sealed record SolicitudCheckOutFacturaDto(
    RegistrarCheckOutDto CheckOut,
    GenerarFacturaDto Factura);

/// <summary>Catálogo de servicios adicionales que recepción puede cargar a una estadía.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/servicios-adicionales")]
[Authorize(Roles = RolesSistema.Empleado)]
public sealed class ControladorServiciosAdicionales : ControladorBase
{
    private readonly IFachadaServiciosHotel _hotel;

    public ControladorServiciosAdicionales(IFachadaServiciosHotel hotel) => _hotel = hotel;

    /// <summary>Lista los servicios del catálogo.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServicioAdicionalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancelacion) =>
        Responder(await _hotel.Estadias.ListarServiciosAsync(cancelacion));

    /// <summary>Da de alta un servicio nuevo.</summary>
    [HttpPost]
    [Authorize(Roles = RolesSistema.Administrador)]
    [ProducesResponseType(typeof(ServicioAdicionalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearServicioAdicionalDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Estadias.CrearServicioAsync(solicitud, cancelacion));

    /// <summary>Actualiza un servicio del catálogo.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesSistema.Administrador)]
    [ProducesResponseType(typeof(ServicioAdicionalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarServicioAdicionalDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Estadias.ActualizarServicioAsync(id, solicitud, cancelacion));
}
