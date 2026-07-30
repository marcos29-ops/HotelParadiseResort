using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelParadiseResort.Application.Abstracciones;
using HotelParadiseResort.Domain.Entidades;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelParadiseResort.Infrastructure.Seguridad;

/// <summary>
/// Emisión de tokens JWT firmados con HMAC-SHA256 (RNF01).
///
/// El token transporta el identificador, el nombre de usuario y el rol, que es lo que
/// la autorización por roles necesita. No incluye datos sensibles: su contenido es
/// legible por cualquiera que lo posea.
/// </summary>
public sealed class ServicioToken : IServicioToken
{
    private readonly OpcionesJwt _opciones;

    public ServicioToken(IOptions<OpcionesJwt> opciones)
    {
        _opciones = opciones.Value;

        if (string.IsNullOrWhiteSpace(_opciones.Clave) ||
            Encoding.UTF8.GetByteCount(_opciones.Clave) < OpcionesJwt.LongitudMinimaClave)
        {
            throw new InvalidOperationException(
                $"La clave de firma JWT debe definirse y tener al menos " +
                $"{OpcionesJwt.LongitudMinimaClave} bytes. Configure 'Jwt:Clave' mediante " +
                "variables de entorno o el gestor de secretos.");
        }
    }

    public (string Token, DateTime Expiracion) GenerarToken(Usuario usuario)
    {
        var expiracion = DateTime.UtcNow.AddMinutes(_opciones.MinutosExpiracion);

        var declaraciones = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, usuario.NombreUsuario),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.Role, usuario.Rol.ToString())
        };

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Clave));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Emisor,
            audience: _opciones.Audiencia,
            claims: declaraciones,
            notBefore: DateTime.UtcNow,
            expires: expiracion,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiracion);
    }
}
