using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelParadiseResort.Application.DTOs.Clientes;
using HotelParadiseResort.Application.DTOs.Estadias;
using HotelParadiseResort.Application.DTOs.Habitaciones;
using HotelParadiseResort.Shared.Paginacion;

namespace HotelParadiseResort.Tests.Integracion;

/// <summary>
/// Gestión de clientes y de los catálogos de habitaciones y servicios
/// (RF01, RF02). Incluye la verificación de la búsqueda insensible a acentos.
/// </summary>
[Collection(ColeccionIntegracion.Nombre)]
public sealed class PruebasClientesYCatalogos : IAsyncLifetime
{
    private readonly FabricaAplicacionPruebas _fabrica;
    private HttpClient _cliente = null!;

    public PruebasClientesYCatalogos(FabricaAplicacionPruebas fabrica) => _fabrica = fabrica;

    public async Task InitializeAsync()
    {
        await _fabrica.LimpiarDatosTransaccionalesAsync();
        _cliente = await _fabrica.CrearClienteAutenticadoAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RegistrarCliente_DevuelveElRecursoCreado()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/clientes",
            new CrearClienteDto("C-100-001", "Ana", "Pérez Ramírez",
                "ana.perez@correo.cr", "8888-2211", "Costa Rica", new DateTime(1990, 5, 12)));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        respuesta.Headers.Location.Should().NotBeNull();

        var creado = await respuesta.Content.ReadFromJsonAsync<ClienteDto>();
        creado!.NombreCompleto.Should().Be("Ana Pérez Ramírez");
        creado.CantidadReservas.Should().Be(0);
        creado.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task RegistrarCliente_ConIdentificacionRepetida_DevuelveConflicto()
    {
        await CrearClienteAsync("C-100-002", "Luis", "Mora");

        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/clientes",
            new CrearClienteDto("C-100-002", "Otro", "Distinto", null, null, null, null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("")]
    [InlineData("correo-sin-arroba")]
    public async Task RegistrarCliente_ConDatosInvalidos_DevuelveSolicitudIncorrecta(string correo)
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/clientes",
            new CrearClienteDto("", "", "", correo, null, null, null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// La collation acento-insensible permite localizar a "Pérez" escribiendo "Perez",
    /// escenario cotidiano en la recepción de un hotel con huéspedes hispanohablantes.
    /// </summary>
    [Theory]
    [InlineData("Perez")]
    [InlineData("Pérez")]
    [InlineData("PEREZ")]
    [InlineData("perez")]
    [InlineData("ramirez")]
    public async Task BuscarCliente_EsInsensibleAMayusculasYAcentos(string termino)
    {
        await CrearClienteAsync("C-100-003", "Ana", "Pérez Ramírez");

        var pagina = await _cliente.GetFromJsonAsync<ResultadoPaginado<ClienteDto>>(
            $"/api/v1/clientes?busqueda={Uri.EscapeDataString(termino)}");

        pagina!.Elementos.Should().ContainSingle()
            .Which.NombreCompleto.Should().Be("Ana Pérez Ramírez");
    }

    [Fact]
    public async Task BuscarPorIdentificacion_DevuelveElCliente()
    {
        await CrearClienteAsync("C-100-004", "Marta", "Solís");

        var cliente = await _cliente.GetFromJsonAsync<ClienteDto>(
            "/api/v1/clientes/por-identificacion/C-100-004");

        cliente!.Identificacion.Should().Be("C-100-004");
    }

    [Fact]
    public async Task BuscarPorIdentificacionInexistente_DevuelveNoEncontrado()
    {
        var respuesta = await _cliente.GetAsync("/api/v1/clientes/por-identificacion/NO-EXISTE");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActualizarCliente_RegistraElCambioEnElHistorial()
    {
        var creado = await CrearClienteAsync("C-100-005", "Jorge", "Núñez");

        var respuesta = await _cliente.PutAsJsonAsync($"/api/v1/clientes/{creado.Id}",
            new ActualizarClienteDto("Jorge Andrés", "Núñez Castro",
                "jorge@correo.cr", "7000-1234", "Costa Rica", null, true));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await respuesta.Content.ReadFromJsonAsync<ClienteDto>())!.Nombre.Should().Be("Jorge Andrés");

        var historial = await _cliente.GetFromJsonAsync<List<HistorialClienteDto>>(
            $"/api/v1/clientes/{creado.Id}/historial");

        historial.Should().HaveCountGreaterThanOrEqualTo(2);
        historial.Should().Contain(h => h.Accion == "Registro");
        historial.Should().Contain(h => h.Accion == "Actualización");
    }

    [Fact]
    public async Task ActualizarClienteInexistente_DevuelveNoEncontrado()
    {
        var respuesta = await _cliente.PutAsJsonAsync("/api/v1/clientes/999999",
            new ActualizarClienteDto("X", "Y", null, null, null, null, true));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConsultarReservasDeUnClienteSinReservas_DevuelveListaVacia()
    {
        var creado = await CrearClienteAsync("C-100-006", "Sin", "Reservas");

        var reservas = await _cliente.GetFromJsonAsync<List<object>>(
            $"/api/v1/clientes/{creado.Id}/reservas");

        reservas.Should().BeEmpty();
    }

    [Fact]
    public async Task ListarClientes_RespetaElTamanoDePagina()
    {
        for (var i = 0; i < 5; i++)
        {
            await CrearClienteAsync($"C-200-00{i}", $"Cliente{i}", "Paginado");
        }

        var pagina = await _cliente.GetFromJsonAsync<ResultadoPaginado<ClienteDto>>(
            "/api/v1/clientes?pagina=1&tamanoPagina=3");

        pagina!.Elementos.Should().HaveCount(3);
        pagina.TotalRegistros.Should().BeGreaterThanOrEqualTo(5);
        pagina.TienePaginaSiguiente.Should().BeTrue();
        pagina.TienePaginaAnterior.Should().BeFalse();
    }

    // --- Catálogo de habitaciones ----------------------------------------------

    [Fact]
    public async Task CrearHabitacion_NaceDisponible()
    {
        var tipo = await ObtenerTipoAsync();

        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/habitaciones",
            new CrearHabitacionDto("501", 5, tipo.Id, "Con vista al jardín"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        var habitacion = await respuesta.Content.ReadFromJsonAsync<HabitacionDto>();
        habitacion!.Estado.Should().Be("Disponible");
        habitacion.TipoHabitacion.Should().Be(tipo.Nombre);
    }

    [Fact]
    public async Task CrearHabitacion_ConNumeroRepetido_DevuelveConflicto()
    {
        var tipo = await ObtenerTipoAsync();
        await _cliente.PostAsJsonAsync("/api/v1/habitaciones",
            new CrearHabitacionDto("502", 5, tipo.Id, null));

        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/habitaciones",
            new CrearHabitacionDto("502", 5, tipo.Id, null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CrearHabitacion_ConTipoInexistente_EsRechazada()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/habitaciones",
            new CrearHabitacionDto("503", 5, 999999, null));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CambiarEstado_AplicaLaTransicionValida()
    {
        var habitacion = await CrearHabitacionAsync("504");

        var respuesta = await _cliente.PatchAsJsonAsync(
            $"/api/v1/habitaciones/{habitacion.Id}/estado",
            new CambiarEstadoHabitacionDto("EnMantenimiento"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await respuesta.Content.ReadFromJsonAsync<HabitacionDto>())!.Estado.Should().Be("EnMantenimiento");
    }

    [Fact]
    public async Task CambiarEstado_ConTransicionInvalida_DevuelveNoProcesable()
    {
        var habitacion = await CrearHabitacionAsync("505");
        await _cliente.PatchAsJsonAsync($"/api/v1/habitaciones/{habitacion.Id}/estado",
            new CambiarEstadoHabitacionDto("Ocupada"));

        // Ocupada solo admite pasar a limpieza o mantenimiento.
        var respuesta = await _cliente.PatchAsJsonAsync(
            $"/api/v1/habitaciones/{habitacion.Id}/estado",
            new CambiarEstadoHabitacionDto("Reservada"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CambiarEstado_ConValorDesconocido_DevuelveSolicitudIncorrecta()
    {
        var habitacion = await CrearHabitacionAsync("506");

        var respuesta = await _cliente.PatchAsJsonAsync(
            $"/api/v1/habitaciones/{habitacion.Id}/estado",
            new CambiarEstadoHabitacionDto("Inventado"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HabitacionEnMantenimiento_QuedaFueraDeLaDisponibilidad()
    {
        var habitacion = await CrearHabitacionAsync("507");
        await _cliente.PatchAsJsonAsync($"/api/v1/habitaciones/{habitacion.Id}/estado",
            new CambiarEstadoHabitacionDto("EnMantenimiento"));

        var entrada = DateTime.UtcNow.Date.AddDays(200);
        var disponibles = await _cliente.GetFromJsonAsync<List<HabitacionDisponibleDto>>(
            $"/api/v1/habitaciones/disponibilidad?fechaEntrada={entrada:yyyy-MM-dd}&fechaSalida={entrada.AddDays(2):yyyy-MM-dd}");

        disponibles.Should().NotContain(h => h.Id == habitacion.Id);
    }

    [Fact]
    public async Task ActualizarHabitacion_ModificaSusDatos()
    {
        var habitacion = await CrearHabitacionAsync("508");
        var tipo = await ObtenerTipoAsync();

        var respuesta = await _cliente.PutAsJsonAsync($"/api/v1/habitaciones/{habitacion.Id}",
            new ActualizarHabitacionDto("508-A", 6, tipo.Id, "Renovada", true));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await respuesta.Content.ReadFromJsonAsync<HabitacionDto>())!.Numero.Should().Be("508-A");
    }

    [Fact]
    public async Task ObtenerHabitacionInexistente_DevuelveNoEncontrada()
    {
        var respuesta = await _cliente.GetAsync("/api/v1/habitaciones/999999");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Tipos de habitación y servicios ---------------------------------------

    [Fact]
    public async Task CrearTipoHabitacion_QuedaEnElCatalogo()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/tipos-habitacion",
            new CrearTipoHabitacionDto("Suite presidencial", "La mejor del hotel", 350.00m, 6));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var tipo = await respuesta.Content.ReadFromJsonAsync<TipoHabitacionDto>();
        tipo!.TarifaBasePorNoche.Should().Be(350.00m);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(-50, 2)]
    [InlineData(100, 0)]
    [InlineData(100, 50)]
    public async Task CrearTipoHabitacion_ConValoresInvalidos_EsRechazado(decimal tarifa, int capacidad)
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/tipos-habitacion",
            new CrearTipoHabitacionDto($"Tipo {tarifa}-{capacidad}", null, tarifa, capacidad));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CrearServicioAdicional_QuedaEnElCatalogo()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/servicios-adicionales",
            new CrearServicioAdicionalDto("Spa", "ActividadRecreativa", "Masajes y sauna", 45.00m));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var servicio = await respuesta.Content.ReadFromJsonAsync<ServicioAdicionalDto>();
        servicio!.Tipo.Should().Be("ActividadRecreativa");
    }

    [Fact]
    public async Task CrearServicioAdicional_ConTipoInvalido_EsRechazado()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/servicios-adicionales",
            new CrearServicioAdicionalDto("Servicio raro", "NoExiste", null, 10m));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListarServicios_DevuelveLosSembrados()
    {
        var servicios = await _cliente.GetFromJsonAsync<List<ServicioAdicionalDto>>(
            "/api/v1/servicios-adicionales");

        servicios.Should().NotBeEmpty();
        servicios.Should().Contain(s => s.Tipo == "Restaurante");
        servicios.Should().Contain(s => s.Tipo == "Lavanderia");
        servicios.Should().Contain(s => s.Tipo == "Transporte");
    }

    // --- Utilidades -----------------------------------------------------------

    private async Task<ClienteDto> CrearClienteAsync(string identificacion, string nombre, string apellidos)
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/clientes",
            new CrearClienteDto(identificacion, nombre, apellidos, null, null, null, null));

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<ClienteDto>())!;
    }

    private async Task<TipoHabitacionDto> ObtenerTipoAsync()
    {
        var tipos = await _cliente.GetFromJsonAsync<List<TipoHabitacionDto>>("/api/v1/tipos-habitacion");
        return tipos!.First();
    }

    private async Task<HabitacionDto> CrearHabitacionAsync(string numero)
    {
        var tipo = await ObtenerTipoAsync();

        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/habitaciones",
            new CrearHabitacionDto(numero, 5, tipo.Id, null));

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<HabitacionDto>())!;
    }
}
