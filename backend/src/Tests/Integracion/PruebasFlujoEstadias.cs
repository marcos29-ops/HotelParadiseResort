using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelParadiseResort.Application.DTOs.Clientes;
using HotelParadiseResort.Application.DTOs.Estadias;
using HotelParadiseResort.Application.DTOs.Facturacion;
using HotelParadiseResort.Application.DTOs.Habitaciones;
using HotelParadiseResort.Application.DTOs.Reservas;
using HotelParadiseResort.Shared.Paginacion;

namespace HotelParadiseResort.Tests.Integracion;

/// <summary>
/// Check-in, consumos, check-out y facturación de extremo a extremo
/// (RF05, RF06, RF07, RF08). Cubre los patrones Decorator y Builder en ejecución real.
/// </summary>
[Collection(ColeccionIntegracion.Nombre)]
public sealed class PruebasFlujoEstadias : IAsyncLifetime
{
    private readonly FabricaAplicacionPruebas _fabrica;
    private HttpClient _cliente = null!;
    private int _habitacionId;
    private List<ServicioAdicionalDto> _servicios = [];

    public PruebasFlujoEstadias(FabricaAplicacionPruebas fabrica) => _fabrica = fabrica;

    public async Task InitializeAsync()
    {
        await _fabrica.LimpiarDatosTransaccionalesAsync();
        _cliente = await _fabrica.CrearClienteAutenticadoAsync();

        var tipos = await _cliente.GetFromJsonAsync<List<TipoHabitacionDto>>("/api/v1/tipos-habitacion");
        var tipo = tipos!.First(t => t.CapacidadMaxima >= 2);

        var creada = await _cliente.PostAsJsonAsync(
            "/api/v1/habitaciones", new CrearHabitacionDto("401", 4, tipo.Id, null));

        _habitacionId = (await creada.Content.ReadFromJsonAsync<HabitacionDto>())!.Id;

        _servicios = (await _cliente.GetFromJsonAsync<List<ServicioAdicionalDto>>(
            "/api/v1/servicios-adicionales"))!;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CheckIn_SobreReservaConfirmada_DejaLaHabitacionOcupada()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0001", diasDesdeHoy: 0);

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/estadias/check-in", new RegistrarCheckInDto(reserva.Id, null, 2, "Llegada puntual"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        var estadia = await respuesta.Content.ReadFromJsonAsync<EstadiaDto>();
        estadia!.Estado.Should().Be("EnCurso");

        var habitacion = await _cliente.GetFromJsonAsync<HabitacionDto>($"/api/v1/habitaciones/{_habitacionId}");
        habitacion!.Estado.Should().Be("Ocupada");
    }

    [Fact]
    public async Task CheckIn_SobreReservaPendiente_EsRechazado()
    {
        var reserva = await CrearReservaAsync("A-1000-0002", diasDesdeHoy: 0, confirmar: false);

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/estadias/check-in", new RegistrarCheckInDto(reserva.Id, null, 1, null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CheckIn_AntesDeLaFechaDeEntrada_EsRechazado()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0003", diasDesdeHoy: 60);

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/estadias/check-in", new RegistrarCheckInDto(reserva.Id, null, 1, null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CheckIn_DuplicadoSobreLaMismaReserva_DevuelveConflicto()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0004", diasDesdeHoy: 0);
        await RegistrarCheckInAsync(reserva.Id);

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/estadias/check-in", new RegistrarCheckInDto(reserva.Id, null, 1, null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// PATRÓN DECORATOR — La cuenta acumula el hospedaje más cada consumo registrado.
    /// </summary>
    [Fact]
    public async Task Cuenta_AcumulaHospedajeYConsumos()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0005", diasDesdeHoy: 0, noches: 3);
        var estadia = await RegistrarCheckInAsync(reserva.Id);

        await RegistrarConsumoAsync(estadia.Id, "Restaurante", "Cena - mesa 4", 1, 28.50m);
        await RegistrarConsumoAsync(estadia.Id, "Lavanderia", "2 prendas", 2, 3.00m);
        await RegistrarConsumoAsync(estadia.Id, "Transporte", "Aeropuerto - hotel", 1, 18.00m);

        var cuenta = await _cliente.GetFromJsonAsync<CuentaEstadiaDto>(
            $"/api/v1/estadias/{estadia.Id}/cuenta");

        cuenta!.Noches.Should().Be(3);
        cuenta.SubtotalConsumos.Should().Be(52.50m);
        cuenta.SubtotalHospedaje.Should().Be(cuenta.TarifaPorNoche * 3);
        cuenta.Total.Should().Be(cuenta.SubtotalHospedaje + cuenta.SubtotalConsumos);
        cuenta.Lineas.Should().HaveCount(4);
    }

    [Fact]
    public async Task Consumo_SeEliminaMientrasLaEstadiaSigueAbierta()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0006", diasDesdeHoy: 0);
        var estadia = await RegistrarCheckInAsync(reserva.Id);
        var consumo = await RegistrarConsumoAsync(estadia.Id, "Restaurante", "Cargo erróneo", 1, 15.00m);

        var respuesta = await _cliente.DeleteAsync($"/api/v1/estadias/{estadia.Id}/consumos/{consumo.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var cuenta = await _cliente.GetFromJsonAsync<CuentaEstadiaDto>($"/api/v1/estadias/{estadia.Id}/cuenta");
        cuenta!.SubtotalConsumos.Should().Be(0m);
    }

    [Fact]
    public async Task Consumo_SobreEstadiaCerrada_EsRechazado()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0007", diasDesdeHoy: 0);
        var estadia = await RegistrarCheckInAsync(reserva.Id);
        await _cliente.PostAsJsonAsync(
            $"/api/v1/estadias/{estadia.Id}/check-out", new RegistrarCheckOutDto(null, null));

        var respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/estadias/{estadia.Id}/consumos",
            new RegistrarConsumoDto(_servicios[0].Id, "Tardío", 1, 10m, null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CheckOut_CierraLaEstadiaYDejaLaHabitacionEnLimpieza()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0008", diasDesdeHoy: 0);
        var estadia = await RegistrarCheckInAsync(reserva.Id);

        var respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/estadias/{estadia.Id}/check-out", new RegistrarCheckOutDto(null, "Sin novedades"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await respuesta.Content.ReadFromJsonAsync<EstadiaDto>())!.Estado.Should().Be("Finalizada");

        var habitacion = await _cliente.GetFromJsonAsync<HabitacionDto>($"/api/v1/habitaciones/{_habitacionId}");
        habitacion!.Estado.Should().Be("EnLimpieza");

        var reservaFinal = await _cliente.GetFromJsonAsync<ReservaDto>($"/api/v1/reservas/{reserva.Id}");
        reservaFinal!.Estado.Should().Be("Completada");
    }

    [Fact]
    public async Task CheckOut_Duplicado_DevuelveConflicto()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0009", diasDesdeHoy: 0);
        var estadia = await RegistrarCheckInAsync(reserva.Id);
        await _cliente.PostAsJsonAsync(
            $"/api/v1/estadias/{estadia.Id}/check-out", new RegistrarCheckOutDto(null, null));

        var respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/estadias/{estadia.Id}/check-out", new RegistrarCheckOutDto(null, null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// PATRÓN BUILDER — La factura consolida hospedaje, consumos y descuento, y su
    /// total coincide con el de la vista previa de la cuenta.
    /// </summary>
    [Fact]
    public async Task Facturacion_ConsolidaHospedajeConsumosYDescuento()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0010", diasDesdeHoy: 0, noches: 3);
        var estadia = await RegistrarCheckInAsync(reserva.Id);

        await RegistrarConsumoAsync(estadia.Id, "Restaurante", "Cena - mesa 4", 1, 28.50m);
        await RegistrarConsumoAsync(estadia.Id, "Lavanderia", "2 prendas", 2, 3.00m);
        await RegistrarConsumoAsync(estadia.Id, "Transporte", "Aeropuerto - hotel", 1, 18.00m);

        var cuenta = await _cliente.GetFromJsonAsync<CuentaEstadiaDto>($"/api/v1/estadias/{estadia.Id}/cuenta");

        var respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/estadias/{estadia.Id}/check-out-y-facturar",
            new
            {
                checkOut = new { observaciones = "Salida sin novedades" },
                factura = new
                {
                    estadiaId = estadia.Id,
                    descuento = 10.00m,
                    justificacionDescuento = "Cortesía por demora en el check-in",
                    metodoPago = "TarjetaCredito",
                    registrarPagoInmediato = true
                }
            });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var factura = await respuesta.Content.ReadFromJsonAsync<FacturaDto>();
        factura!.Numero.Should().StartWith("FAC-");
        factura.SubtotalConsumos.Should().Be(52.50m);
        factura.SubtotalHospedaje.Should().Be(cuenta!.SubtotalHospedaje);
        factura.Descuento.Should().Be(10.00m);
        factura.Total.Should().Be(factura.SubtotalHospedaje + factura.SubtotalConsumos - 10.00m);
        factura.EstadoPago.Should().Be("Pagado");
        factura.Detalles.Should().HaveCount(4);
    }

    [Fact]
    public async Task Facturacion_ConDescuentoSinJustificacion_EsRechazada()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0011", diasDesdeHoy: 0);
        var estadia = await RegistrarCheckInAsync(reserva.Id);
        await _cliente.PostAsJsonAsync(
            $"/api/v1/estadias/{estadia.Id}/check-out", new RegistrarCheckOutDto(null, null));

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/facturas", new GenerarFacturaDto(estadia.Id, 10.00m, null, null, false));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Facturacion_SinCheckOutPrevio_EsRechazada()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0012", diasDesdeHoy: 0);
        var estadia = await RegistrarCheckInAsync(reserva.Id);

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/facturas", new GenerarFacturaDto(estadia.Id, null, null, null, false));

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Facturacion_DuplicadaSobreLaMismaEstadia_DevuelveConflicto()
    {
        var estadia = await FacturarEstadiaAsync("A-1000-0013");

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/facturas", new GenerarFacturaDto(estadia, null, null, null, false));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AplicarDescuento_SobreFacturaPagada_EsRechazado()
    {
        var estadiaId = await FacturarEstadiaAsync("A-1000-0014", pagar: true);
        var factura = await _cliente.GetFromJsonAsync<FacturaDto>($"/api/v1/facturas/por-estadia/{estadiaId}");

        var respuesta = await _cliente.PatchAsJsonAsync(
            $"/api/v1/facturas/{factura!.Id}/descuento", new AplicarDescuentoDto(5m, "Tardío"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AplicarDescuento_SobreFacturaPendiente_RecalculaElTotal()
    {
        var estadiaId = await FacturarEstadiaAsync("A-1000-0015", pagar: false);
        var factura = await _cliente.GetFromJsonAsync<FacturaDto>($"/api/v1/facturas/por-estadia/{estadiaId}");

        var respuesta = await _cliente.PatchAsJsonAsync(
            $"/api/v1/facturas/{factura!.Id}/descuento",
            new AplicarDescuentoDto(15m, "Ajuste comercial autorizado"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var actualizada = await respuesta.Content.ReadFromJsonAsync<FacturaDto>();
        actualizada!.Descuento.Should().Be(15m);
        actualizada.Total.Should().Be(
            actualizada.SubtotalHospedaje + actualizada.SubtotalConsumos - 15m);
    }

    [Fact]
    public async Task RegistrarPago_MarcaLaFacturaComoPagada()
    {
        var estadiaId = await FacturarEstadiaAsync("A-1000-0016", pagar: false);
        var factura = await _cliente.GetFromJsonAsync<FacturaDto>($"/api/v1/facturas/por-estadia/{estadiaId}");

        var respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/facturas/{factura!.Id}/pago", new RegistrarPagoDto("Efectivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagada = await respuesta.Content.ReadFromJsonAsync<FacturaDto>();
        pagada!.EstadoPago.Should().Be("Pagado");
        pagada.MetodoPago.Should().Be("Efectivo");
    }

    [Fact]
    public async Task RegistrarPago_DosVeces_DevuelveConflicto()
    {
        var estadiaId = await FacturarEstadiaAsync("A-1000-0017", pagar: true);
        var factura = await _cliente.GetFromJsonAsync<FacturaDto>($"/api/v1/facturas/por-estadia/{estadiaId}");

        var respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/facturas/{factura!.Id}/pago", new RegistrarPagoDto("Efectivo"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task BuscarEstadiaPorHabitacion_LocalizaLaEstadiaEnCurso()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0018", diasDesdeHoy: 0);
        await RegistrarCheckInAsync(reserva.Id);

        var estadia = await _cliente.GetFromJsonAsync<EstadiaDto>("/api/v1/estadias/por-habitacion/401");

        estadia!.HabitacionNumero.Should().Be("401");
        estadia.Estado.Should().Be("EnCurso");
    }

    [Fact]
    public async Task BuscarEstadiaPorHabitacionInexistente_DevuelveNoEncontrado()
    {
        var respuesta = await _cliente.GetAsync("/api/v1/estadias/por-habitacion/9999");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListarFacturas_DevuelvePaginacion()
    {
        await FacturarEstadiaAsync("A-1000-0019", pagar: true);

        var pagina = await _cliente.GetFromJsonAsync<ResultadoPaginado<FacturaDto>>(
            "/api/v1/facturas?pagina=1&tamanoPagina=10");

        pagina!.TotalRegistros.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ListarEstadias_FiltraPorEstado()
    {
        var reserva = await CrearReservaConfirmadaAsync("A-1000-0020", diasDesdeHoy: 0);
        await RegistrarCheckInAsync(reserva.Id);

        var pagina = await _cliente.GetFromJsonAsync<ResultadoPaginado<EstadiaDto>>(
            "/api/v1/estadias?estado=EnCurso");

        pagina!.Elementos.Should().OnlyContain(e => e.Estado == "EnCurso");
    }

    // --- Utilidades -----------------------------------------------------------

    private async Task<ReservaDto> CrearReservaAsync(
        string identificacion, int diasDesdeHoy, bool confirmar, int noches = 2)
    {
        var entrada = DateTime.UtcNow.Date.AddDays(diasDesdeHoy);

        var respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/reservas?confirmar={confirmar.ToString().ToLowerInvariant()}",
            new CrearReservaDto(
                null,
                new CrearClienteDto(identificacion, "Huésped", "De Prueba", null, null, null, null),
                _habitacionId, entrada, entrada.AddDays(noches), 2, "Telefono", null));

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<ReservaDto>())!;
    }

    private Task<ReservaDto> CrearReservaConfirmadaAsync(
        string identificacion, int diasDesdeHoy, int noches = 2) =>
        CrearReservaAsync(identificacion, diasDesdeHoy, confirmar: true, noches);

    private async Task<EstadiaDto> RegistrarCheckInAsync(int reservaId)
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/estadias/check-in", new RegistrarCheckInDto(reservaId, null, 2, null));

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<EstadiaDto>())!;
    }

    private async Task<ConsumoDto> RegistrarConsumoAsync(
        int estadiaId, string tipoServicio, string descripcion, int cantidad, decimal precio)
    {
        var servicio = _servicios.First(s => s.Tipo == tipoServicio);

        var respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/estadias/{estadiaId}/consumos",
            new RegistrarConsumoDto(servicio.Id, descripcion, cantidad, precio, null));

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<ConsumoDto>())!;
    }

    /// <summary>Recorre el ciclo completo hasta dejar la estadía facturada.</summary>
    private async Task<int> FacturarEstadiaAsync(string identificacion, bool pagar = true)
    {
        var reserva = await CrearReservaConfirmadaAsync(identificacion, diasDesdeHoy: 0);
        var estadia = await RegistrarCheckInAsync(reserva.Id);

        await RegistrarConsumoAsync(estadia.Id, "Restaurante", "Consumo de prueba", 1, 20.00m);
        await _cliente.PostAsJsonAsync(
            $"/api/v1/estadias/{estadia.Id}/check-out", new RegistrarCheckOutDto(null, null));

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/facturas",
            new GenerarFacturaDto(estadia.Id, null, null, pagar ? "Efectivo" : null, pagar));

        respuesta.EnsureSuccessStatusCode();

        return estadia.Id;
    }
}
