using Asp.Versioning;
using HotelParadiseResort.Application.DTOs.Facturacion;
using HotelParadiseResort.Application.Fachada;
using HotelParadiseResort.Shared.Constantes;
using HotelParadiseResort.Shared.Paginacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelParadiseResort.API.Controllers;

/// <summary>COMPONENTE 5 — Facturación (RF08).</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/facturas")]
[Authorize(Roles = RolesSistema.Empleado)]
public sealed class ControladorFacturacion : ControladorBase
{
    private readonly IFachadaServiciosHotel _hotel;

    public ControladorFacturacion(IFachadaServiciosHotel hotel) => _hotel = hotel;

    /// <summary>Listado paginado de comprobantes por rango de emisión.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<FacturaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] ParametrosPaginacion parametros,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        CancellationToken cancelacion) =>
        Responder(await _hotel.Facturacion.ListarAsync(parametros, desde, hasta, cancelacion));

    /// <summary>Obtiene una factura con su desglose completo (reimpresión).</summary>
    [HttpGet("{id:int}", Name = nameof(ObtenerFactura))]
    [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerFactura(int id, CancellationToken cancelacion) =>
        Responder(await _hotel.Facturacion.ObtenerPorIdAsync(id, cancelacion));

    /// <summary>Obtiene la factura emitida para una estadía.</summary>
    [HttpGet("por-estadia/{estadiaId:int}")]
    [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorEstadia(int estadiaId, CancellationToken cancelacion) =>
        Responder(await _hotel.Facturacion.ObtenerPorEstadiaAsync(estadiaId, cancelacion));

    /// <summary>
    /// RF08 — Emite la factura consolidada de una estadía cerrada, ensamblada con el
    /// patrón Builder. Devuelve 409 si la estadía ya fue facturada.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Generar(
        [FromBody] GenerarFacturaDto solicitud, CancellationToken cancelacion)
    {
        var resultado = await _hotel.Facturacion.GenerarAsync(solicitud, cancelacion);

        return ResponderCreado(resultado, nameof(ObtenerFactura), new { id = resultado.Valor?.Id });
    }

    /// <summary>Aplica un descuento justificado sobre una factura pendiente de pago.</summary>
    [HttpPatch("{id:int}/descuento")]
    [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AplicarDescuento(
        int id, [FromBody] AplicarDescuentoDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Facturacion.AplicarDescuentoAsync(id, solicitud, cancelacion));

    /// <summary>Registra el cobro de la factura.</summary>
    [HttpPost("{id:int}/pago")]
    [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarPago(
        int id, [FromBody] RegistrarPagoDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Facturacion.RegistrarPagoAsync(id, solicitud, cancelacion));
}
