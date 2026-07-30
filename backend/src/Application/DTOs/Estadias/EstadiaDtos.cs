namespace HotelParadiseResort.Application.DTOs.Estadias;

/// <summary>
/// Estadía con el resumen del huésped y su cuenta. Alimenta la cabecera fija de la
/// pantalla de check-in/check-out, que mantiene los datos visibles en todo momento.
/// </summary>
public sealed record EstadiaDto(
    int Id,
    int ReservaId,
    string ReservaCodigo,
    int ClienteId,
    string ClienteNombre,
    string ClienteIdentificacion,
    int HabitacionId,
    string HabitacionNumero,
    string TipoHabitacion,
    DateTime FechaCheckIn,
    DateTime? FechaCheckOut,
    DateTime FechaSalidaPrevista,
    int Noches,
    int CantidadHuespedes,
    string Estado,
    string? Observaciones,
    string RegistradaPor,
    decimal TotalConsumos,
    bool TieneFactura,
    IReadOnlyList<ConsumoDto> Consumos);

/// <summary>Datos del formulario de check-in.</summary>
public sealed record RegistrarCheckInDto(
    int ReservaId,
    DateTime? FechaCheckIn,
    int CantidadHuespedes,
    string? Observaciones);

/// <summary>Datos del formulario de check-out.</summary>
public sealed record RegistrarCheckOutDto(
    DateTime? FechaCheckOut,
    string? Observaciones);

/// <summary>Consumo imputado a una estadía.</summary>
public sealed record ConsumoDto(
    int Id,
    int EstadiaId,
    int ServicioAdicionalId,
    string Servicio,
    string TipoServicio,
    string Descripcion,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Monto,
    DateTime FechaConsumo,
    string RegistradoPor);

/// <summary>Registro de un cargo nuevo sobre la estadía.</summary>
public sealed record RegistrarConsumoDto(
    int ServicioAdicionalId,
    string Descripcion,
    int Cantidad,
    decimal? PrecioUnitario,
    DateTime? FechaConsumo);

/// <summary>
/// Cuenta consolidada de la estadía tal como la produce la cadena de decoradores.
/// Es la vista previa que el recepcionista ve antes de emitir la factura.
/// </summary>
public sealed record CuentaEstadiaDto(
    int EstadiaId,
    string Descripcion,
    int Noches,
    decimal TarifaPorNoche,
    decimal SubtotalHospedaje,
    decimal SubtotalConsumos,
    decimal Total,
    string EstrategiaTarifa,
    IReadOnlyList<LineaCuentaDto> Lineas);

public sealed record LineaCuentaDto(
    string Concepto,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal,
    bool EsHospedaje,
    DateTime? Fecha);

/// <summary>Servicio del catálogo que recepción puede cargar a una estadía.</summary>
public sealed record ServicioAdicionalDto(
    int Id,
    string Nombre,
    string Tipo,
    string? Descripcion,
    decimal PrecioBase,
    bool Activo);

public sealed record CrearServicioAdicionalDto(
    string Nombre,
    string Tipo,
    string? Descripcion,
    decimal PrecioBase);

public sealed record ActualizarServicioAdicionalDto(
    string Nombre,
    string Tipo,
    string? Descripcion,
    decimal PrecioBase,
    bool Activo);
