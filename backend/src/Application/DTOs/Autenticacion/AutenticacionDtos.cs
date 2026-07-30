namespace HotelParadiseResort.Application.DTOs.Autenticacion;

/// <summary>Credenciales enviadas desde la pantalla de inicio de sesión.</summary>
public sealed record SolicitudInicioSesionDto(string NombreUsuario, string Contrasena);

/// <summary>Token emitido y datos del usuario que la interfaz necesita tras autenticarse.</summary>
public sealed record RespuestaInicioSesionDto(
    string Token,
    DateTime Expiracion,
    UsuarioAutenticadoDto Usuario);

/// <summary>Identidad del usuario en sesión. No expone la contraseña ni su hash.</summary>
public sealed record UsuarioAutenticadoDto(
    int Id,
    string Nombre,
    string NombreUsuario,
    string Correo,
    string Rol);

/// <summary>Cambio de contraseña del propio usuario en sesión.</summary>
public sealed record CambioContrasenaDto(string ContrasenaActual, string ContrasenaNueva);
