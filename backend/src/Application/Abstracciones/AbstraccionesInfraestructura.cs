using HotelParadiseResort.Domain.Entidades;

namespace HotelParadiseResort.Application.Abstracciones;

/// <summary>
/// Emisión de tokens de acceso. La capa de aplicación no conoce JWT ni su
/// configuración: solo pide un token para un usuario ya autenticado.
/// </summary>
public interface IServicioToken
{
    (string Token, DateTime Expiracion) GenerarToken(Usuario usuario);
}

/// <summary>
/// Resguardo de contraseñas. Las credenciales nunca se almacenan en claro.
/// </summary>
public interface IServicioContrasena
{
    string Hashear(string contrasena);

    bool Verificar(string contrasena, string hashAlmacenado);
}

/// <summary>
/// Identidad del usuario que realiza la petición en curso. Permite registrar la
/// autoría de cada operación sin acoplar los servicios a ASP.NET Core.
/// </summary>
public interface IUsuarioActual
{
    int? Id { get; }

    string? NombreUsuario { get; }

    string? Rol { get; }

    bool EstaAutenticado { get; }
}

/// <summary>
/// Fuente de fecha y hora. Inyectarla mantiene las reglas dependientes del tiempo
/// verificables en las pruebas.
/// </summary>
public interface IProveedorFechaHora
{
    /// <summary>Instante actual en UTC. Es lo que se persiste en las marcas de tiempo.</summary>
    DateTime Ahora { get; }

    DateTime HoyUtc { get; }

    /// <summary>
    /// Fecha del calendario en la zona horaria del hotel.
    ///
    /// Las reglas de negocio que comparan días —«la entrada no puede ser anterior a
    /// hoy», «llegadas del día»— deben usar esta fecha y no la de UTC: en Costa Rica
    /// (UTC−6) a partir de las 18:00 locales el día UTC ya avanzó, y el recepcionista
    /// no podría registrar una reserva para el mismo día en que está trabajando.
    /// </summary>
    DateTime FechaOperativa { get; }
}
