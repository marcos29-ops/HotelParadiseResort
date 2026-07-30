using HotelParadiseResort.Application.DTOs.Clientes;
using HotelParadiseResort.Application.DTOs.Estadias;
using HotelParadiseResort.Application.DTOs.Facturacion;
using HotelParadiseResort.Application.DTOs.Habitaciones;
using HotelParadiseResort.Application.DTOs.Reservas;
using HotelParadiseResort.Domain.Entidades;

namespace HotelParadiseResort.Application.Mapeo;

/// <summary>
/// Conversión de entidades a DTOs en un único lugar (DRY). Se resuelve con métodos
/// estáticos en lugar de reflexión porque los DTO son <c>record</c> con constructor
/// posicional y varios campos son calculados o provienen de relaciones cargadas.
/// </summary>
public static class MapeadorClientes
{
    public static ClienteDto AClienteDto(Cliente cliente, int cantidadReservas) => new(
        cliente.Id,
        cliente.Identificacion,
        cliente.Nombre,
        cliente.Apellidos,
        cliente.NombreCompleto,
        cliente.Correo,
        cliente.Telefono,
        cliente.Nacionalidad,
        cliente.FechaNacimiento,
        cliente.Activo,
        cantidadReservas);
}

public static class MapeadorHabitaciones
{
    public static HabitacionDto AHabitacionDto(Habitacion habitacion)
    {
        var estado = habitacion.ObtenerEstado();

        return new HabitacionDto(
            habitacion.Id,
            habitacion.Numero,
            habitacion.Piso,
            habitacion.TipoHabitacionId,
            habitacion.TipoHabitacion?.Nombre ?? string.Empty,
            habitacion.TipoHabitacion?.TarifaBasePorNoche ?? 0m,
            habitacion.TipoHabitacion?.CapacidadMaxima ?? 0,
            habitacion.Estado.ToString(),
            estado.Nombre,
            habitacion.Observaciones,
            habitacion.Activo);
    }

    public static TipoHabitacionDto ATipoHabitacionDto(TipoHabitacion tipo, int cantidadHabitaciones) => new(
        tipo.Id,
        tipo.Nombre,
        tipo.Descripcion,
        tipo.TarifaBasePorNoche,
        tipo.CapacidadMaxima,
        tipo.Activo,
        cantidadHabitaciones);
}

public static class MapeadorReservas
{
    public static ReservaDto AReservaDto(Reserva reserva)
    {
        var estado = reserva.ObtenerEstado();

        return new ReservaDto(
            reserva.Id,
            reserva.Codigo,
            reserva.ClienteId,
            reserva.Cliente?.NombreCompleto ?? string.Empty,
            reserva.Cliente?.Identificacion ?? string.Empty,
            reserva.HabitacionId,
            reserva.Habitacion?.Numero ?? string.Empty,
            reserva.Habitacion?.TipoHabitacion?.Nombre ?? string.Empty,
            reserva.FechaEntrada,
            reserva.FechaSalida,
            reserva.CalcularNoches(),
            reserva.CantidadHuespedes,
            reserva.Estado.ToString(),
            reserva.CanalOrigen.ToString(),
            reserva.MontoEstimado,
            reserva.Observaciones,
            reserva.UsuarioRegistro?.Nombre ?? string.Empty,
            reserva.FechaCreacion,
            reserva.Estadia is not null,
            estado.PermiteModificacion,
            estado.PermiteCheckIn);
    }
}

public static class MapeadorEstadias
{
    public static EstadiaDto AEstadiaDto(Estadia estadia)
    {
        var consumos = estadia.Consumos.Select(AConsumoDto).OrderBy(c => c.FechaConsumo).ToList();

        return new EstadiaDto(
            estadia.Id,
            estadia.ReservaId,
            estadia.Reserva?.Codigo ?? string.Empty,
            estadia.Reserva?.ClienteId ?? 0,
            estadia.Reserva?.Cliente?.NombreCompleto ?? string.Empty,
            estadia.Reserva?.Cliente?.Identificacion ?? string.Empty,
            estadia.HabitacionId,
            estadia.Habitacion?.Numero ?? string.Empty,
            estadia.Habitacion?.TipoHabitacion?.Nombre ?? string.Empty,
            estadia.FechaCheckIn,
            estadia.FechaCheckOut,
            estadia.Reserva?.FechaSalida ?? estadia.FechaCheckIn,
            estadia.CalcularNoches(),
            estadia.CantidadHuespedes,
            estadia.Estado.ToString(),
            estadia.Observaciones,
            estadia.UsuarioCheckIn?.Nombre ?? string.Empty,
            decimal.Round(consumos.Sum(c => c.Monto), 2, MidpointRounding.AwayFromZero),
            estadia.Factura is not null,
            consumos);
    }

    public static ConsumoDto AConsumoDto(Consumo consumo) => new(
        consumo.Id,
        consumo.EstadiaId,
        consumo.ServicioAdicionalId,
        consumo.ServicioAdicional?.Nombre ?? string.Empty,
        consumo.ServicioAdicional?.Tipo.ToString() ?? string.Empty,
        consumo.Descripcion,
        consumo.Cantidad,
        consumo.PrecioUnitario,
        consumo.CalcularMonto(),
        consumo.FechaConsumo,
        consumo.UsuarioRegistro?.Nombre ?? string.Empty);

    public static ServicioAdicionalDto AServicioDto(ServicioAdicional servicio) => new(
        servicio.Id,
        servicio.Nombre,
        servicio.Tipo.ToString(),
        servicio.Descripcion,
        servicio.PrecioBase,
        servicio.Activo);
}

public static class MapeadorFacturas
{
    public static FacturaDto AFacturaDto(Factura factura)
    {
        var reserva = factura.Estadia?.Reserva;

        return new FacturaDto(
            factura.Id,
            factura.Numero,
            factura.EstadiaId,
            factura.ClienteId,
            factura.Cliente?.NombreCompleto ?? string.Empty,
            factura.Cliente?.Identificacion ?? string.Empty,
            factura.Estadia?.Habitacion?.Numero ?? string.Empty,
            factura.Estadia?.Habitacion?.TipoHabitacion?.Nombre ?? string.Empty,
            reserva?.Codigo ?? string.Empty,
            reserva?.FechaEntrada ?? factura.FechaEmision,
            reserva?.FechaSalida ?? factura.FechaEmision,
            factura.FechaEmision,
            factura.Noches,
            factura.TarifaPorNoche,
            factura.EstrategiaTarifa,
            factura.SubtotalHospedaje,
            factura.SubtotalConsumos,
            factura.Descuento,
            factura.JustificacionDescuento,
            factura.Total,
            factura.MetodoPago?.ToString(),
            factura.EstadoPago.ToString(),
            factura.FechaPago,
            factura.UsuarioEmision?.Nombre ?? string.Empty,
            factura.Detalles
                .OrderByDescending(d => d.EsHospedaje)
                .ThenBy(d => d.FechaConsumo)
                .Select(d => new DetalleFacturaDto(
                    d.Id, d.Concepto, d.Cantidad, d.PrecioUnitario, d.Subtotal, d.EsHospedaje, d.FechaConsumo))
                .ToList());
    }
}
