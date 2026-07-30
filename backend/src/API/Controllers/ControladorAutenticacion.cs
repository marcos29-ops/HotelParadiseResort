using System.Security.Claims;
using Asp.Versioning;
using HotelParadiseResort.Application.DTOs.Autenticacion;
using HotelParadiseResort.Application.Fachada;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelParadiseResort.API.Controllers;

/// <summary>Inicio de sesión y gestión de la propia credencial (RNF01).</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/autenticacion")]
public sealed class ControladorAutenticacion : ControladorBase
{
    private readonly IFachadaServiciosHotel _hotel;

    public ControladorAutenticacion(IFachadaServiciosHotel hotel) => _hotel = hotel;

    /// <summary>Autentica al usuario y emite el token de acceso.</summary>
    [HttpPost("iniciar-sesion")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespuestaInicioSesionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IniciarSesion(
        [FromBody] SolicitudInicioSesionDto solicitud, CancellationToken cancelacion) =>
        Responder(await _hotel.Autenticacion.IniciarSesionAsync(solicitud, cancelacion));

    /// <summary>Devuelve la identidad del usuario en sesión.</summary>
    [HttpGet("perfil")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioAutenticadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerPerfil(CancellationToken cancelacion)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var resultado = await _hotel.Usuarios.ObtenerPorIdAsync(id, cancelacion);

        if (resultado.EsFallido)
        {
            return Responder(resultado);
        }

        var usuario = resultado.Valor!;

        return Ok(new UsuarioAutenticadoDto(
            usuario.Id, usuario.Nombre, usuario.NombreUsuario, usuario.Correo, usuario.Rol));
    }

    /// <summary>Cambia la contraseña del usuario en sesión.</summary>
    [HttpPost("cambiar-contrasena")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CambiarContrasena(
        [FromBody] CambioContrasenaDto solicitud, CancellationToken cancelacion)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        return Responder(await _hotel.Autenticacion.CambiarContrasenaAsync(id, solicitud, cancelacion));
    }
}
