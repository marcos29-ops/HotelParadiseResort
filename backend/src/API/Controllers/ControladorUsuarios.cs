using Asp.Versioning;
using HotelParadiseResort.Application.DTOs.Usuarios;
using HotelParadiseResort.Application.Fachada;
using HotelParadiseResort.Shared.Constantes;
using HotelParadiseResort.Shared.Paginacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelParadiseResort.API.Controllers;

/// <summary>
/// Administración del personal del sistema. Exclusiva del rol Administrador, conforme
/// al diagrama de casos de uso.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/usuarios")]
[Authorize(Roles = RolesSistema.Administrador)]
public sealed class ControladorUsuarios : ControladorBase
{
    private readonly IFachadaServiciosHotel _hotel;

    public ControladorUsuarios(IFachadaServiciosHotel hotel) => _hotel = hotel;

    /// <summary>Listado paginado del personal.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<UsuarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] ParametrosPaginacion parametros, CancellationToken cancelacion) =>
        Responder(await _hotel.Usuarios.ListarAsync(parametros, cancelacion));

    /// <summary>Obtiene un usuario por su identificador.</summary>
    [HttpGet("{id:int}", Name = nameof(ObtenerUsuario))]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerUsuario(int id, CancellationToken cancelacion) =>
        Responder(await _hotel.Usuarios.ObtenerPorIdAsync(id, cancelacion));

    /// <summary>Crea una cuenta de personal.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearUsuarioDto solicitud, CancellationToken cancelacion)
    {
        var resultado = await _hotel.Usuarios.CrearAsync(solicitud, cancelacion);

        return ResponderCreado(resultado, nameof(ObtenerUsuario), new { id = resultado.Valor?.Id });
    }

    /// <summary>Actualiza los datos y el rol de una cuenta.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarUsuarioDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Usuarios.ActualizarAsync(id, solicitud, cancelacion));

    /// <summary>Restablece la contraseña de una cuenta.</summary>
    [HttpPost("{id:int}/restablecer-contrasena")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestablecerContrasena(
        int id, [FromBody] RestablecerContrasenaDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Usuarios.RestablecerContrasenaAsync(id, solicitud, cancelacion));

    /// <summary>Desactiva una cuenta sin eliminar su historial de operaciones.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken cancelacion) =>
        Responder(await _hotel.Usuarios.DesactivarAsync(id, cancelacion));
}
