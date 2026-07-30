using Asp.Versioning;
using HotelParadiseResort.Application.DTOs.Clientes;
using HotelParadiseResort.Application.DTOs.Reservas;
using HotelParadiseResort.Application.Fachada;
using HotelParadiseResort.Shared.Constantes;
using HotelParadiseResort.Shared.Paginacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelParadiseResort.API.Controllers;

/// <summary>COMPONENTE 1 — Gestión de Clientes (RF01).</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/clientes")]
[Authorize(Roles = RolesSistema.Empleado)]
public sealed class ControladorClientes : ControladorBase
{
    private readonly IFachadaServiciosHotel _hotel;

    public ControladorClientes(IFachadaServiciosHotel hotel) => _hotel = hotel;

    /// <summary>Listado paginado con búsqueda y ordenamiento.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<ClienteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] ParametrosPaginacion parametros, CancellationToken cancelacion) =>
        Responder(await _hotel.Clientes.ListarAsync(parametros, cancelacion));

    /// <summary>Obtiene un cliente por su identificador.</summary>
    [HttpGet("{id:int}", Name = nameof(ObtenerCliente))]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerCliente(int id, CancellationToken cancelacion) =>
        Responder(await _hotel.Clientes.ObtenerPorIdAsync(id, cancelacion));

    /// <summary>Busca un huésped por su documento de identidad.</summary>
    [HttpGet("por-identificacion/{identificacion}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BuscarPorIdentificacion(
        string identificacion, CancellationToken cancelacion) =>
        Responder(await _hotel.Clientes.BuscarPorIdentificacionAsync(identificacion, cancelacion));

    /// <summary>Registra un huésped nuevo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(
        [FromBody] CrearClienteDto solicitud, CancellationToken cancelacion)
    {
        var resultado = await _hotel.Clientes.RegistrarAsync(solicitud, cancelacion);

        return ResponderCreado(resultado, nameof(ObtenerCliente), new { id = resultado.Valor?.Id });
    }

    /// <summary>Actualiza los datos de un huésped.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarClienteDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Clientes.ActualizarAsync(id, solicitud, cancelacion));

    /// <summary>Bitácora de cambios del cliente.</summary>
    [HttpGet("{id:int}/historial")]
    [ProducesResponseType(typeof(IReadOnlyList<HistorialClienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarHistorial(int id, CancellationToken cancelacion) =>
        Responder(await _hotel.Clientes.ConsultarHistorialAsync(id, cancelacion));

    /// <summary>Reservas asociadas al cliente.</summary>
    [HttpGet("{id:int}/reservas")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarReservas(int id, CancellationToken cancelacion) =>
        Responder(await _hotel.Clientes.ConsultarReservasAsync(id, cancelacion));
}
