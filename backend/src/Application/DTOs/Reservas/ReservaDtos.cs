namespace HotelParadiseResort.Application.DTOs.Reservas;

/// <summary>Reserva con los datos que el listado y el detalle necesitan mostrar.</summary>
public sealed record ReservaDto(
    int Id,
    string Codigo,
    int ClienteId,
    string ClienteNombre,
    string ClienteIdentificacion,
    int HabitacionId,
    string HabitacionNumero,
    string TipoHabitacion,
    DateTime FechaEntrada,
    DateTime FechaSalida,
    int Noches,
    int CantidadHuespedes,
    string Estado,
    string CanalOrigen,
    decimal MontoEstimado,
    string? Observaciones,
    string RegistradaPor,
    DateTime FechaCreacion,
    bool TieneEstadia,
    bool PermiteModificacion,
    bool PermiteCheckIn);

/// <summary>
/// Datos de la pantalla "Nueva reserva". El cliente puede indicarse por su
/// identificador o registrarse en el mismo paso mediante <see cref="ClienteNuevo"/>.
/// </summary>
public sealed record CrearReservaDto(
    int? ClienteId,
    Clientes.CrearClienteDto? ClienteNuevo,
    int HabitacionId,
    DateTime FechaEntrada,
    DateTime FechaSalida,
    int CantidadHuespedes,
    string CanalOrigen,
    string? Observaciones);

/// <summary>Modificación de una reserva que todavía admite cambios.</summary>
public sealed record ActualizarReservaDto(
    int HabitacionId,
    DateTime FechaEntrada,
    DateTime FechaSalida,
    int CantidadHuespedes,
    string CanalOrigen,
    string? Observaciones);

/// <summary>Cancelación con su motivo, requerido para la auditoría.</summary>
public sealed record CancelarReservaDto(string Motivo);
