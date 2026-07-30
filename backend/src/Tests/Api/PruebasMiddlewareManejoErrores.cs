using System.Net;
using System.Text.Json;
using FluentAssertions;
using HotelParadiseResort.API.Middleware;
using HotelParadiseResort.Shared.Excepciones;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HotelParadiseResort.Tests.Api;

/// <summary>
/// Manejador global de errores.
///
/// Comprueba lo que exige el proyecto: que ninguna excepción escape sin traducirse y
/// que el usuario nunca reciba una traza técnica, sino un mensaje comprensible.
/// </summary>
public sealed class PruebasMiddlewareManejoErrores
{
    [Fact]
    public async Task ExcepcionDeReglaDeNegocio_SeTraduceANoProcesable()
    {
        var (estado, problema) = await EjecutarAsync(
            new ExcepcionReglaNegocio("La estadía ya fue facturada."));

        estado.Should().Be(StatusCodes.Status422UnprocessableEntity);
        problema.GetProperty("detail").GetString().Should().Be("La estadía ya fue facturada.");
        problema.GetProperty("title").GetString().Should().Be("No se pudo completar la operación");
    }

    [Fact]
    public async Task TransicionDeEstadoInvalida_SeTraduceANoProcesable()
    {
        var (estado, problema) = await EjecutarAsync(
            new ExcepcionTransicionEstadoInvalida("habitación", "Ocupada", "Reservada"));

        estado.Should().Be(StatusCodes.Status422UnprocessableEntity);
        problema.GetProperty("title").GetString().Should().Contain("estado actual");
    }

    /// <summary>Colisión de concurrencia optimista (RNF03) → 409 con mensaje accionable.</summary>
    [Fact]
    public async Task ConflictoDeConcurrencia_SeTraduceAConflicto()
    {
        var (estado, problema) = await EjecutarAsync(new DbUpdateConcurrencyException());

        estado.Should().Be(StatusCodes.Status409Conflict);
        problema.GetProperty("detail").GetString().Should().Contain("Otro usuario modificó");
    }

    [Fact]
    public async Task ErrorDeEscrituraEnBaseDeDatos_SeTraduceAConflicto()
    {
        var (estado, _) = await EjecutarAsync(new DbUpdateException("violación de índice único"));

        estado.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task AccesoNoAutorizado_SeTraduceAProhibido()
    {
        var (estado, _) = await EjecutarAsync(new UnauthorizedAccessException());

        estado.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task SolicitudCancelada_SeTraduceAlCodigoDeClienteDesconectado()
    {
        var (estado, _) = await EjecutarAsync(new OperationCanceledException());

        estado.Should().Be(499);
    }

    /// <summary>
    /// Una excepción inesperada no debe filtrar detalles internos: en producción se
    /// devuelve un mensaje genérico y la traza queda solo en el log.
    /// </summary>
    [Fact]
    public async Task ExcepcionInesperadaEnProduccion_NoExponeLaTraza()
    {
        var (estado, problema) = await EjecutarAsync(
            new InvalidOperationException("cadena de conexión inválida: Password=secreto"),
            entorno: "Production");

        estado.Should().Be(StatusCodes.Status500InternalServerError);
        problema.GetProperty("detail").GetString().Should().Be(
            "Ocurrió un error inesperado. Si el problema persiste, contacte al administrador del sistema.");
        problema.TryGetProperty("excepcion", out _).Should().BeFalse();
        problema.ToString().Should().NotContain("secreto");
    }

    [Fact]
    public async Task ExcepcionInesperadaEnDesarrollo_IncluyeLaTrazaParaDepurar()
    {
        var (_, problema) = await EjecutarAsync(
            new InvalidOperationException("fallo interno"), entorno: "Development");

        problema.TryGetProperty("excepcion", out var traza).Should().BeTrue();
        traza.GetString().Should().Contain("InvalidOperationException");
    }

    [Fact]
    public async Task RespuestaDeError_IncluyeElIdentificadorDeTrazaYElTipoDeContenido()
    {
        var contexto = CrearContexto();

        await CrearMiddleware(_ => throw new ExcepcionReglaNegocio("error"))
            .InvokeAsync(contexto);

        contexto.Response.ContentType.Should().Be("application/problem+json");
        LeerProblema(contexto).GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SinExcepcion_LaPeticionContinuaSinAlterarse()
    {
        var contexto = CrearContexto();
        var continuo = false;

        await CrearMiddleware(_ =>
        {
            continuo = true;
            return Task.CompletedTask;
        }).InvokeAsync(contexto);

        continuo.Should().BeTrue();
        contexto.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    // --- Utilidades -----------------------------------------------------------

    private static async Task<(int Estado, JsonElement Problema)> EjecutarAsync(
        Exception excepcion, string entorno = "Development")
    {
        var contexto = CrearContexto();

        await CrearMiddleware(_ => throw excepcion, entorno).InvokeAsync(contexto);

        return (contexto.Response.StatusCode, LeerProblema(contexto));
    }

    private static MiddlewareManejoErrores CrearMiddleware(
        RequestDelegate siguiente, string entorno = "Development")
    {
        var ambiente = new Mock<IHostEnvironment>();
        ambiente.SetupGet(a => a.EnvironmentName).Returns(entorno);

        return new MiddlewareManejoErrores(
            siguiente, NullLogger<MiddlewareManejoErrores>.Instance, ambiente.Object);
    }

    private static DefaultHttpContext CrearContexto()
    {
        var contexto = new DefaultHttpContext { TraceIdentifier = "traza-de-prueba" };
        contexto.Request.Path = "/api/v1/reservas";
        contexto.Request.Method = HttpMethods.Post;
        contexto.Response.Body = new MemoryStream();
        return contexto;
    }

    private static JsonElement LeerProblema(HttpContext contexto)
    {
        contexto.Response.Body.Seek(0, SeekOrigin.Begin);
        var cuerpo = new StreamReader(contexto.Response.Body).ReadToEnd();

        return string.IsNullOrWhiteSpace(cuerpo)
            ? default
            : JsonDocument.Parse(cuerpo).RootElement;
    }
}
