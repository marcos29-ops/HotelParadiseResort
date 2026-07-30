using System.Security.Claims;
using HotelParadiseResort.Application.Abstracciones;
using Microsoft.AspNetCore.Http;

namespace HotelParadiseResort.Infrastructure.Servicios;

/// <summary>
/// Identidad del usuario que realiza la petición, leída de las declaraciones del token.
/// Aísla a la capa de aplicación de ASP.NET Core: los servicios registran la autoría de
/// cada operación sin conocer <c>HttpContext</c>.
/// </summary>
public sealed class UsuarioActual : IUsuarioActual
{
    private readonly IHttpContextAccessor _accesorContexto;

    public UsuarioActual(IHttpContextAccessor accesorContexto) => _accesorContexto = accesorContexto;

    private ClaimsPrincipal? Principal => _accesorContexto.HttpContext?.User;

    public int? Id =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? NombreUsuario => Principal?.FindFirstValue(ClaimTypes.Name);

    public string? Rol => Principal?.FindFirstValue(ClaimTypes.Role);

    public bool EstaAutenticado => Principal?.Identity?.IsAuthenticated ?? false;
}
