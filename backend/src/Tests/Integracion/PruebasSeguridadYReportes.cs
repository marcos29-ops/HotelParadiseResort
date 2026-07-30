using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HotelParadiseResort.Application.DTOs.Autenticacion;
using HotelParadiseResort.Application.DTOs.Reportes;
using HotelParadiseResort.Application.DTOs.Usuarios;
using HotelParadiseResort.Shared.Paginacion;

namespace HotelParadiseResort.Tests.Integracion;

/// <summary>
/// Autenticación, autorización por rol y reportes administrativos (RNF01, RF09).
///
/// Verifica el principio ISP documentado en la Etapa 1: la interfaz de Recepción no
/// alcanza la reportería macro ni la gestión de usuarios.
/// </summary>
[Collection(ColeccionIntegracion.Nombre)]
public sealed class PruebasSeguridadYReportes : IAsyncLifetime
{
    private const string UsuarioRecepcion = "recepcion.pruebas";
    private const string ContrasenaRecepcion = "Recepcion2026";

    private readonly FabricaAplicacionPruebas _fabrica;
    private HttpClient _administrador = null!;

    public PruebasSeguridadYReportes(FabricaAplicacionPruebas fabrica) => _fabrica = fabrica;

    public async Task InitializeAsync()
    {
        await _fabrica.LimpiarDatosTransaccionalesAsync();
        _administrador = await _fabrica.CrearClienteAutenticadoAsync();

        var existentes = await _administrador.GetFromJsonAsync<ResultadoPaginado<UsuarioDto>>(
            $"/api/v1/usuarios?busqueda={UsuarioRecepcion}");

        if (existentes!.TotalRegistros == 0)
        {
            await _administrador.PostAsJsonAsync("/api/v1/usuarios", new CrearUsuarioDto(
                "María Recepción", UsuarioRecepcion,
                "recepcion.pruebas@paradiseresort.cr", ContrasenaRecepcion, "Recepcionista"));
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // --- Autenticación ----------------------------------------------------------

    [Fact]
    public async Task IniciarSesion_ConCredencialesValidas_DevuelveToken()
    {
        using var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/v1/autenticacion/iniciar-sesion",
            new SolicitudInicioSesionDto("admin", FabricaAplicacionPruebas.ContrasenaAdmin));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var sesion = await respuesta.Content.ReadFromJsonAsync<RespuestaInicioSesionDto>();
        sesion!.Token.Should().NotBeNullOrWhiteSpace();
        sesion.Usuario.Rol.Should().Be("Administrador");
        sesion.Expiracion.Should().BeAfter(DateTime.UtcNow);
    }

    [Theory]
    [InlineData("admin", "contrasena-incorrecta")]
    [InlineData("usuario-inexistente", "Cualquiera2026!")]
    public async Task IniciarSesion_ConCredencialesInvalidas_DevuelveNoAutenticado(
        string usuario, string contrasena)
    {
        using var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/v1/autenticacion/iniciar-sesion",
            new SolicitudInicioSesionDto(usuario, contrasena));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IniciarSesion_SinDatos_DevuelveSolicitudIncorrecta()
    {
        using var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/v1/autenticacion/iniciar-sesion",
            new SolicitudInicioSesionDto("", ""));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ObtenerPerfil_DevuelveLaIdentidadDelUsuarioEnSesion()
    {
        var perfil = await _administrador.GetFromJsonAsync<UsuarioAutenticadoDto>(
            "/api/v1/autenticacion/perfil");

        perfil!.NombreUsuario.Should().Be("admin");
        perfil.Rol.Should().Be("Administrador");
    }

    [Theory]
    [InlineData("/api/v1/clientes")]
    [InlineData("/api/v1/habitaciones")]
    [InlineData("/api/v1/reservas")]
    [InlineData("/api/v1/panel/resumen")]
    [InlineData("/api/v1/autenticacion/perfil")]
    public async Task EndpointsProtegidos_SinToken_DevuelvenNoAutenticado(string ruta)
    {
        using var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.GetAsync(ruta);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EndpointProtegido_ConTokenInvalido_DevuelveNoAutenticado()
    {
        using var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "token.falso.inventado");

        var respuesta = await cliente.GetAsync("/api/v1/clientes");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PuntoDeSalud_EsAccesibleSinAutenticacion()
    {
        using var cliente = _fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Autorización por rol (ISP) ---------------------------------------------

    [Theory]
    [InlineData("/api/v1/reportes/ocupacion?fechaDesde=2026-01-01&fechaHasta=2026-12-31")]
    [InlineData("/api/v1/reportes/ingresos?fechaDesde=2026-01-01&fechaHasta=2026-12-31")]
    [InlineData("/api/v1/reportes/temporadas?fechaDesde=2026-01-01&fechaHasta=2026-12-31")]
    [InlineData("/api/v1/usuarios")]
    public async Task Recepcionista_NoAccedeAReportesNiAUsuarios(string ruta)
    {
        var recepcion = await _fabrica.CrearClienteAutenticadoAsync(UsuarioRecepcion, ContrasenaRecepcion);

        var respuesta = await recepcion.GetAsync(ruta);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/v1/clientes")]
    [InlineData("/api/v1/habitaciones")]
    [InlineData("/api/v1/reservas")]
    [InlineData("/api/v1/panel/resumen")]
    public async Task Recepcionista_AccedeASusFuncionesOperativas(string ruta)
    {
        var recepcion = await _fabrica.CrearClienteAutenticadoAsync(UsuarioRecepcion, ContrasenaRecepcion);

        var respuesta = await recepcion.GetAsync(ruta);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Gestión de usuarios -----------------------------------------------------

    [Fact]
    public async Task CrearUsuario_ConNombreRepetido_DevuelveConflicto()
    {
        var respuesta = await _administrador.PostAsJsonAsync("/api/v1/usuarios", new CrearUsuarioDto(
            "Duplicado", UsuarioRecepcion, "otro@paradiseresort.cr", "Otra2026Clave", "Recepcionista"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("corta1A")]           // menos de 8 caracteres
    [InlineData("sinmayusculas26")]   // sin mayúscula
    [InlineData("SINMINUSCULAS26")]   // sin minúscula
    [InlineData("SinNumerosAqui")]    // sin dígito
    public async Task CrearUsuario_ConContrasenaDebil_EsRechazado(string contrasena)
    {
        var respuesta = await _administrador.PostAsJsonAsync("/api/v1/usuarios", new CrearUsuarioDto(
            "Prueba Política", $"usuario.{Guid.NewGuid():N}"[..20],
            $"{Guid.NewGuid():N}@paradiseresort.cr", contrasena, "Recepcionista"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CrearUsuario_ConRolInvalido_EsRechazado()
    {
        var respuesta = await _administrador.PostAsJsonAsync("/api/v1/usuarios", new CrearUsuarioDto(
            "Rol Inventado", $"rol.{Guid.NewGuid():N}"[..15],
            $"{Guid.NewGuid():N}@paradiseresort.cr", "Valida2026Clave", "Gerente"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Salvaguarda operativa: nadie puede desactivar su propia cuenta en sesión.
    ///
    /// La regla complementaria —conservar siempre un administrador activo— se verifica
    /// de forma aislada en <see cref="Aplicacion.PruebasServicioUsuarios"/>, porque
    /// depende del censo global de usuarios y aquí resultaría sensible al orden de
    /// ejecución de las demás pruebas.
    /// </summary>
    [Fact]
    public async Task DesactivarLaPropiaCuentaEnSesion_EsRechazado()
    {
        var perfil = await _administrador.GetFromJsonAsync<UsuarioAutenticadoDto>(
            "/api/v1/autenticacion/perfil");

        var respuesta = await _administrador.DeleteAsync($"/api/v1/usuarios/{perfil!.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CambiarContrasena_ConLaActualIncorrecta_EsRechazado()
    {
        var respuesta = await _administrador.PostAsJsonAsync("/api/v1/autenticacion/cambiar-contrasena",
            new CambioContrasenaDto("no-es-la-actual", "NuevaClave2026"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Opera sobre una cuenta creada para esta prueba, no sobre el recepcionista
    /// compartido: así no altera credenciales de las que dependen otras pruebas.
    /// </summary>
    [Fact]
    public async Task RestablecerContrasena_PermiteIniciarSesionConLaNueva()
    {
        const string contrasenaInicial = "Inicial2026Clave";
        const string contrasenaNueva = "Restablecida2026";

        var usuario = await CrearUsuarioDesechableAsync(contrasenaInicial);

        var respuesta = await _administrador.PostAsJsonAsync(
            $"/api/v1/usuarios/{usuario.Id}/restablecer-contrasena",
            new RestablecerContrasenaDto(contrasenaNueva));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var cliente = _fabrica.CreateClient();

        var conNueva = await cliente.PostAsJsonAsync("/api/v1/autenticacion/iniciar-sesion",
            new SolicitudInicioSesionDto(usuario.NombreUsuario, contrasenaNueva));
        conNueva.StatusCode.Should().Be(HttpStatusCode.OK);

        var conAnterior = await cliente.PostAsJsonAsync("/api/v1/autenticacion/iniciar-sesion",
            new SolicitudInicioSesionDto(usuario.NombreUsuario, contrasenaInicial));
        conAnterior.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActualizarUsuario_CambiaSusDatosYRol()
    {
        var usuario = await CrearUsuarioDesechableAsync("Actualizar2026Clave");

        var respuesta = await _administrador.PutAsJsonAsync($"/api/v1/usuarios/{usuario.Id}",
            new ActualizarUsuarioDto("Nombre Actualizado", usuario.Correo, "Administrador", true));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var actualizado = await respuesta.Content.ReadFromJsonAsync<UsuarioDto>();
        actualizado!.Nombre.Should().Be("Nombre Actualizado");
        actualizado.Rol.Should().Be("Administrador");
    }

    [Fact]
    public async Task ActualizarUsuarioInexistente_DevuelveNoEncontrado()
    {
        var respuesta = await _administrador.PutAsJsonAsync("/api/v1/usuarios/999999",
            new ActualizarUsuarioDto("X", "x@paradiseresort.cr", "Recepcionista", true));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DesactivarUsuario_LeImpideIniciarSesion()
    {
        const string contrasena = "Desactivar2026Clave";
        var usuario = await CrearUsuarioDesechableAsync(contrasena);

        var baja = await _administrador.DeleteAsync($"/api/v1/usuarios/{usuario.Id}");
        baja.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var cliente = _fabrica.CreateClient();
        var sesion = await cliente.PostAsJsonAsync("/api/v1/autenticacion/iniciar-sesion",
            new SolicitudInicioSesionDto(usuario.NombreUsuario, contrasena));

        sesion.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ObtenerUsuarioInexistente_DevuelveNoEncontrado()
    {
        var respuesta = await _administrador.GetAsync("/api/v1/usuarios/999999");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Crea una cuenta de recepción con nombre único, exclusiva de una prueba.</summary>
    private async Task<UsuarioDto> CrearUsuarioDesechableAsync(string contrasena)
    {
        var sufijo = Guid.NewGuid().ToString("N")[..8];

        var respuesta = await _administrador.PostAsJsonAsync("/api/v1/usuarios", new CrearUsuarioDto(
            $"Usuario {sufijo}", $"tmp.{sufijo}",
            $"tmp.{sufijo}@paradiseresort.cr", contrasena, "Recepcionista"));

        respuesta.EnsureSuccessStatusCode();

        return (await respuesta.Content.ReadFromJsonAsync<UsuarioDto>())!;
    }

    // --- Reportes y panel --------------------------------------------------------

    [Theory]
    [InlineData("ocupacion")]
    [InlineData("ingresos")]
    [InlineData("temporadas")]
    public async Task Reportes_DevuelvenIndicadoresYSeries(string reporte)
    {
        var resultado = await _administrador.GetFromJsonAsync<ReporteDto>(
            $"/api/v1/reportes/{reporte}?fechaDesde=2026-01-01&fechaHasta=2026-12-31");

        resultado!.Titulo.Should().NotBeNullOrWhiteSpace();
        resultado.Indicadores.Should().NotBeEmpty();
        resultado.Indicadores.Should().OnlyContain(i => !string.IsNullOrWhiteSpace(i.Formato));
    }

    [Theory]
    [InlineData("ocupacion")]
    [InlineData("ingresos")]
    [InlineData("temporadas")]
    public async Task Reportes_ConRangoInvertido_DevuelvenSolicitudIncorrecta(string reporte)
    {
        var respuesta = await _administrador.GetAsync(
            $"/api/v1/reportes/{reporte}?fechaDesde=2026-12-31&fechaHasta=2026-01-01");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reporte_ConRangoExcesivo_EsRechazado()
    {
        var respuesta = await _administrador.GetAsync(
            "/api/v1/reportes/ocupacion?fechaDesde=2000-01-01&fechaHasta=2026-12-31");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PanelResumen_DevuelveLosIndicadoresDelWireframe()
    {
        var resumen = await _administrador.GetFromJsonAsync<ResumenPanelDto>("/api/v1/panel/resumen");

        resumen!.TotalHabitaciones.Should().BeGreaterThanOrEqualTo(0);
        resumen.PorcentajeOcupacion.Should().BeInRange(0, 100);
        resumen.HabitacionesOcupadas.Should().BeLessThanOrEqualTo(resumen.TotalHabitaciones);
    }
}
