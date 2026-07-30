using HotelParadiseResort.Application.DTOs.Facturacion;
using HotelParadiseResort.Application.DTOs.Reservas;
using HotelParadiseResort.Application.Servicios.Interfaces;
using HotelParadiseResort.Shared.Resultados;

namespace HotelParadiseResort.Application.Fachada;

/// <summary>
/// PATRÓN FACADE — Implementación de la fachada de servicios del hotel.
///
/// Expone los seis componentes de negocio tras una superficie única y, además, ofrece
/// las operaciones compuestas que la Etapa 2 menciona explícitamente
/// (<c>crearReserva()</c> y <c>generarFacturaCheckOut()</c>), donde la fachada
/// coordina varios componentes en una sola llamada del cliente.
/// </summary>
public sealed class FachadaServiciosHotel : IFachadaServiciosHotel
{
    private readonly IServicioEstadias _estadias;
    private readonly IServicioFacturacion _facturacion;
    private readonly IServicioReservas _reservas;

    public FachadaServiciosHotel(
        IServicioAutenticacion autenticacion,
        IServicioUsuarios usuarios,
        IServicioClientes clientes,
        IServicioHabitaciones habitaciones,
        IServicioReservas reservas,
        IServicioEstadias estadias,
        IServicioFacturacion facturacion,
        IServicioReportes reportes)
    {
        Autenticacion = autenticacion;
        Usuarios = usuarios;
        Clientes = clientes;
        Habitaciones = habitaciones;
        Reportes = reportes;

        _reservas = reservas;
        _estadias = estadias;
        _facturacion = facturacion;
    }

    public IServicioAutenticacion Autenticacion { get; }

    public IServicioUsuarios Usuarios { get; }

    public IServicioClientes Clientes { get; }

    public IServicioHabitaciones Habitaciones { get; }

    public IServicioReservas Reservas => _reservas;

    public IServicioEstadias Estadias => _estadias;

    public IServicioFacturacion Facturacion => _facturacion;

    public IServicioReportes Reportes { get; }

    /// <summary>
    /// Operación compuesta <c>crearReserva()</c> de la Etapa 2: crea la reserva y, si se
    /// solicita, la deja confirmada en un solo paso. Es lo que hace el botón "Confirmar
    /// reserva" del wireframe, sin obligar al cliente a encadenar dos llamadas.
    /// </summary>
    public async Task<Resultado<ReservaDto>> CrearReservaAsync(
        CrearReservaDto solicitud, bool confirmar, CancellationToken cancelacion = default)
    {
        var creada = await _reservas.CrearAsync(solicitud, cancelacion);

        if (creada.EsFallido || !confirmar)
        {
            return creada;
        }

        return await _reservas.ConfirmarAsync(creada.Valor!.Id, cancelacion);
    }

    /// <summary>
    /// Operación compuesta <c>generarFacturaCheckOut()</c> de la Etapa 2: cierra la
    /// estadía y emite el comprobante consolidado. Reproduce el flujo del diagrama de
    /// secuencia de check-out, donde una sola acción del recepcionista desencadena el
    /// cierre y la facturación.
    /// </summary>
    public async Task<Resultado<FacturaDto>> GenerarFacturaCheckOutAsync(
        int estadiaId,
        DTOs.Estadias.RegistrarCheckOutDto checkOut,
        GenerarFacturaDto factura,
        CancellationToken cancelacion = default)
    {
        var cierre = await _estadias.RegistrarCheckOutAsync(estadiaId, checkOut, cancelacion);

        if (cierre.EsFallido)
        {
            return Resultado.Fallo<FacturaDto>(cierre.Error!, cierre.TipoError);
        }

        return await _facturacion.GenerarAsync(factura with { EstadiaId = estadiaId }, cancelacion);
    }
}
