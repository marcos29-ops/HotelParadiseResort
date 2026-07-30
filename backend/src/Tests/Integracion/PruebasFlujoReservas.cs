using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelParadiseResort.Application.DTOs.Clientes;
using HotelParadiseResort.Application.DTOs.Habitaciones;
using HotelParadiseResort.Application.DTOs.Reservas;
using HotelParadiseResort.Shared.Paginacion;

namespace HotelParadiseResort.Tests.Integracion;

/// <summary>
/// Flujo de reservas de extremo a extremo contra la base de datos real (RF03, RF04).
/// Verifica el objetivo central del sistema: impedir la duplicidad de reservas.
/// </summary>
[Collection(ColeccionIntegracion.Nombre)]
public sealed class PruebasFlujoReservas : IAsyncLifetime
{
    private readonly FabricaAplicacionPruebas _fabrica;
    private HttpClient _cliente = null!;
    private int _habitacionId;

    public PruebasFlujoReservas(FabricaAplicacionPruebas fabrica) => _fabrica = fabrica;

    public async Task InitializeAsync()
    {
        await _fabrica.LimpiarDatosTransaccionalesAsync();
        _cliente = await _fabrica.CrearClienteAutenticadoAsync();
        _habitacionId = await CrearHabitacionAsync("301");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ConsultarDisponibilidad_DevuelveLaTarifaCalculada()
    {
        var (entrada, salida) = RangoFuturo(30, 3);

        var respuesta = await _cliente.GetAsync(
            $"/api/v1/habitaciones/disponibilidad?fechaEntrada={entrada:yyyy-MM-dd}&fechaSalida={salida:yyyy-MM-dd}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var disponibles = await respuesta.Content.ReadFromJsonAsync<List<HabitacionDisponibleDto>>();
        var habitacion = disponibles!.Single(h => h.Id == _habitacionId);

        habitacion.Noches.Should().Be(3);
        habitacion.MontoTotal.Should().Be(habitacion.TarifaPorNoche * 3);
        habitacion.EstrategiaTarifa.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ConsultarDisponibilidad_RechazaUnRangoInvertido()
    {
        var respuesta = await _cliente.GetAsync(
            "/api/v1/habitaciones/disponibilidad?fechaEntrada=2026-12-20&fechaSalida=2026-12-15");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CrearReserva_ConClienteNuevo_QuedaPendiente()
    {
        var (entrada, salida) = RangoFuturo(40, 3);

        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/reservas", new CrearReservaDto(
            null,
            new CrearClienteDto("5-1111-2222", "Laura", "Vargas Solís", null, "8877-1122", null, null),
            _habitacionId, entrada, salida, 2, "Telefono", null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        var reserva = await respuesta.Content.ReadFromJsonAsync<ReservaDto>();
        reserva!.Estado.Should().Be("Pendiente");
        reserva.Noches.Should().Be(3);
        reserva.PermiteCheckIn.Should().BeFalse();
        reserva.Codigo.Should().StartWith("RES-");
    }

    [Fact]
    public async Task CrearReserva_ConConfirmarActivo_QuedaConfirmada()
    {
        var (entrada, salida) = RangoFuturo(50, 2);

        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/reservas?confirmar=true", new CrearReservaDto(
            null,
            new CrearClienteDto("5-3333-4444", "Diego", "Mora Chaves", null, null, null, null),
            _habitacionId, entrada, salida, 1, "CorreoElectronico", null));

        var reserva = await respuesta.Content.ReadFromJsonAsync<ReservaDto>();

        reserva!.Estado.Should().Be("Confirmada");
        reserva.PermiteCheckIn.Should().BeTrue();
    }

    /// <summary>Regla de negocio central: no se admite una segunda reserva solapada.</summary>
    [Fact]
    public async Task CrearReserva_SobreFechasYaOcupadas_DevuelveConflicto()
    {
        var (entrada, salida) = RangoFuturo(60, 4);
        await CrearReservaAsync("6-1111-1111", entrada, salida, confirmar: true);

        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/reservas", new CrearReservaDto(
            null,
            new CrearClienteDto("6-2222-2222", "Sofía", "Blanco León", null, null, null, null),
            _habitacionId, entrada.AddDays(1), salida.AddDays(-1), 1, "RedesSociales", null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>Dos estadías que se tocan en un extremo no se solapan.</summary>
    [Fact]
    public async Task CrearReserva_QueEmpiezaElDiaDeLaSalidaAnterior_EsAceptada()
    {
        var (entrada, salida) = RangoFuturo(80, 3);
        await CrearReservaAsync("7-1111-1111", entrada, salida, confirmar: true);

        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/reservas", new CrearReservaDto(
            null,
            new CrearClienteDto("7-2222-2222", "Marta", "Gómez Ruiz", null, null, null, null),
            _habitacionId, salida, salida.AddDays(2), 1, "Presencial", null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CrearReserva_ConFechaPasada_EsRechazada()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/reservas", new CrearReservaDto(
            null,
            new CrearClienteDto("8-1111-1111", "Pedro", "Rojas Vega", null, null, null, null),
            _habitacionId, DateTime.UtcNow.Date.AddDays(-5), DateTime.UtcNow.Date.AddDays(-2),
            1, "Telefono", null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CrearReserva_QueSuperaLaCapacidad_EsRechazada()
    {
        var (entrada, salida) = RangoFuturo(100, 2);

        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/reservas", new CrearReservaDto(
            null,
            new CrearClienteDto("9-1111-1111", "Ana", "Lobo Díaz", null, null, null, null),
            _habitacionId, entrada, salida, 15, "Telefono", null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ConfirmarYCancelar_RecorrenElCicloDeEstados()
    {
        var (entrada, salida) = RangoFuturo(120, 2);
        var reserva = await CrearReservaAsync("9-2222-2222", entrada, salida, confirmar: false);

        var confirmada = await _cliente.PostAsync($"/api/v1/reservas/{reserva.Id}/confirmar", null);
        confirmada.StatusCode.Should().Be(HttpStatusCode.OK);
        (await confirmada.Content.ReadFromJsonAsync<ReservaDto>())!.Estado.Should().Be("Confirmada");

        var cancelada = await _cliente.PostAsJsonAsync(
            $"/api/v1/reservas/{reserva.Id}/cancelar", new CancelarReservaDto("El huésped canceló su viaje"));

        cancelada.StatusCode.Should().Be(HttpStatusCode.OK);
        (await cancelada.Content.ReadFromJsonAsync<ReservaDto>())!.Estado.Should().Be("Cancelada");
    }

    [Fact]
    public async Task ConfirmarDosVeces_LaSegundaEsRechazada()
    {
        var (entrada, salida) = RangoFuturo(140, 2);
        var reserva = await CrearReservaAsync("9-3333-3333", entrada, salida, confirmar: true);

        var respuesta = await _cliente.PostAsync($"/api/v1/reservas/{reserva.Id}/confirmar", null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CancelarUnaReserva_LiberaLaHabitacionParaEseRango()
    {
        var (entrada, salida) = RangoFuturo(160, 3);
        var reserva = await CrearReservaAsync("9-4444-4444", entrada, salida, confirmar: true);

        await _cliente.PostAsJsonAsync(
            $"/api/v1/reservas/{reserva.Id}/cancelar", new CancelarReservaDto("Prueba de liberación"));

        var disponibles = await _cliente.GetFromJsonAsync<List<HabitacionDisponibleDto>>(
            $"/api/v1/habitaciones/disponibilidad?fechaEntrada={entrada:yyyy-MM-dd}&fechaSalida={salida:yyyy-MM-dd}");

        disponibles.Should().Contain(h => h.Id == _habitacionId);
    }

    [Fact]
    public async Task ModificarReserva_ActualizaFechasYRecalculaElMonto()
    {
        var (entrada, salida) = RangoFuturo(180, 2);
        var reserva = await CrearReservaAsync("9-5555-5555", entrada, salida, confirmar: false);

        var respuesta = await _cliente.PutAsJsonAsync(
            $"/api/v1/reservas/{reserva.Id}",
            new ActualizarReservaDto(_habitacionId, entrada, entrada.AddDays(5), 2, "Presencial", "Ampliación"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var actualizada = await respuesta.Content.ReadFromJsonAsync<ReservaDto>();
        actualizada!.Noches.Should().Be(5);
        actualizada.MontoEstimado.Should().BeGreaterThan(reserva.MontoEstimado);
    }

    [Fact]
    public async Task ObtenerReservaInexistente_DevuelveNoEncontrado()
    {
        var respuesta = await _cliente.GetAsync("/api/v1/reservas/999999");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListarReservas_DevuelvePaginacionYFiltroPorEstado()
    {
        var (entrada, salida) = RangoFuturo(200, 2);
        await CrearReservaAsync("9-6666-6666", entrada, salida, confirmar: true);

        var pagina = await _cliente.GetFromJsonAsync<ResultadoPaginado<ReservaDto>>(
            "/api/v1/reservas?pagina=1&tamanoPagina=5&estado=Confirmada");

        pagina!.Elementos.Should().OnlyContain(r => r.Estado == "Confirmada");
        pagina.TamanoPagina.Should().Be(5);
        pagina.Pagina.Should().Be(1);
    }

    [Fact]
    public async Task ListarReservas_ConEstadoInvalido_DevuelveSolicitudIncorrecta()
    {
        var respuesta = await _cliente.GetAsync("/api/v1/reservas?estado=Inventado");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- Utilidades -----------------------------------------------------------

    private static (DateTime Entrada, DateTime Salida) RangoFuturo(int diasDesdeHoy, int noches)
    {
        var entrada = DateTime.UtcNow.Date.AddDays(diasDesdeHoy);
        return (entrada, entrada.AddDays(noches));
    }

    private async Task<int> CrearHabitacionAsync(string numero)
    {
        var tipos = await _cliente.GetFromJsonAsync<List<TipoHabitacionDto>>("/api/v1/tipos-habitacion");
        var tipoDoble = tipos!.First(t => t.CapacidadMaxima >= 2);

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/habitaciones", new CrearHabitacionDto(numero, 3, tipoDoble.Id, null));

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<HabitacionDto>())!.Id;
    }

    private async Task<ReservaDto> CrearReservaAsync(
        string identificacion, DateTime entrada, DateTime salida, bool confirmar)
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/reservas?confirmar={confirmar.ToString().ToLowerInvariant()}",
            new CrearReservaDto(
                null,
                new CrearClienteDto(identificacion, "Huésped", "De Prueba", null, null, null, null),
                _habitacionId, entrada, salida, 1, "Telefono", null));

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<ReservaDto>())!;
    }
}
