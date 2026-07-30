namespace HotelParadiseResort.Application.DTOs.Usuarios;

/// <summary>Usuario del sistema. Nunca expone la contraseña ni su hash.</summary>
public sealed record UsuarioDto(
    int Id,
    string Nombre,
    string NombreUsuario,
    string Correo,
    string Rol,
    bool Activo,
    DateTime? UltimoAcceso,
    DateTime FechaCreacion);

public sealed record CrearUsuarioDto(
    string Nombre,
    string NombreUsuario,
    string Correo,
    string Contrasena,
    string Rol);

public sealed record ActualizarUsuarioDto(
    string Nombre,
    string Correo,
    string Rol,
    bool Activo);

/// <summary>Restablecimiento de contraseña realizado por un administrador.</summary>
public sealed record RestablecerContrasenaDto(string ContrasenaNueva);
