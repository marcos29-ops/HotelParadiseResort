namespace HotelParadiseResort.Application.DTOs.Habitaciones;

/// <summary>Habitación con su tipo y estado, para el panel y los listados.</summary>
public sealed record HabitacionDto(
    int Id,
    string Numero,
    int Piso,
    int TipoHabitacionId,
    string TipoHabitacion,
    decimal TarifaBasePorNoche,
    int CapacidadMaxima,
    string Estado,
    string EstadoDescripcion,
    string? Observaciones,
    bool Activo);

public sealed record CrearHabitacionDto(
    string Numero,
    int Piso,
    int TipoHabitacionId,
    string? Observaciones);

public sealed record ActualizarHabitacionDto(
    string Numero,
    int Piso,
    int TipoHabitacionId,
    string? Observaciones,
    bool Activo);

/// <summary>Cambio de estado solicitado desde el panel de habitaciones.</summary>
public sealed record CambiarEstadoHabitacionDto(string NuevoEstado);

/// <summary>Tipo de habitación del catálogo.</summary>
public sealed record TipoHabitacionDto(
    int Id,
    string Nombre,
    string? Descripcion,
    decimal TarifaBasePorNoche,
    int CapacidadMaxima,
    bool Activo,
    int CantidadHabitaciones);

public sealed record CrearTipoHabitacionDto(
    string Nombre,
    string? Descripcion,
    decimal TarifaBasePorNoche,
    int CapacidadMaxima);

public sealed record ActualizarTipoHabitacionDto(
    string Nombre,
    string? Descripcion,
    decimal TarifaBasePorNoche,
    int CapacidadMaxima,
    bool Activo);

/// <summary>Filtros de la consulta de disponibilidad (RF03).</summary>
public sealed record ConsultaDisponibilidadDto(
    DateTime FechaEntrada,
    DateTime FechaSalida,
    int? TipoHabitacionId,
    int? CantidadHuespedes);

/// <summary>
/// Habitación disponible junto a la tarifa ya calculada por la estrategia vigente,
/// de modo que la pantalla de reserva muestre el precio sin una llamada adicional.
/// </summary>
public sealed record HabitacionDisponibleDto(
    int Id,
    string Numero,
    int Piso,
    string TipoHabitacion,
    int CapacidadMaxima,
    int Noches,
    decimal TarifaPorNoche,
    decimal MontoTotal,
    string EstrategiaTarifa,
    string DescripcionTarifa);
