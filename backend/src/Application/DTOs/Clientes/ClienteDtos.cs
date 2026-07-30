namespace HotelParadiseResort.Application.DTOs.Clientes;

/// <summary>Cliente tal como lo muestra el listado y el detalle.</summary>
public sealed record ClienteDto(
    int Id,
    string Identificacion,
    string Nombre,
    string Apellidos,
    string NombreCompleto,
    string? Correo,
    string? Telefono,
    string? Nacionalidad,
    DateTime? FechaNacimiento,
    bool Activo,
    int CantidadReservas);

/// <summary>Datos para registrar un huésped nuevo.</summary>
public sealed record CrearClienteDto(
    string Identificacion,
    string Nombre,
    string Apellidos,
    string? Correo,
    string? Telefono,
    string? Nacionalidad,
    DateTime? FechaNacimiento);

/// <summary>Datos modificables de un huésped ya registrado.</summary>
public sealed record ActualizarClienteDto(
    string Nombre,
    string Apellidos,
    string? Correo,
    string? Telefono,
    string? Nacionalidad,
    DateTime? FechaNacimiento,
    bool Activo);

/// <summary>Entrada de la bitácora de cambios de un cliente.</summary>
public sealed record HistorialClienteDto(
    int Id,
    string Accion,
    string Detalle,
    string Usuario,
    DateTime FechaRegistro);
