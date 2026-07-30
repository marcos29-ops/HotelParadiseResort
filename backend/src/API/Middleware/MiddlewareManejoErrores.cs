using System.Text.Json;
using HotelParadiseResort.Shared.Excepciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelParadiseResort.API.Middleware;

/// <summary>
/// Manejador global de errores. Registra el detalle técnico en el log y devuelve al
/// cliente un <c>ProblemDetails</c> con un mensaje comprensible: la interfaz nunca
/// muestra trazas ni detalles internos al usuario.
/// </summary>
public sealed class MiddlewareManejoErrores
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _siguiente;
    private readonly ILogger<MiddlewareManejoErrores> _registro;
    private readonly IHostEnvironment _entorno;

    public MiddlewareManejoErrores(
        RequestDelegate siguiente,
        ILogger<MiddlewareManejoErrores> registro,
        IHostEnvironment entorno)
    {
        _siguiente = siguiente;
        _registro = registro;
        _entorno = entorno;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (Exception excepcion)
        {
            await ManejarAsync(contexto, excepcion);
        }
    }

    private async Task ManejarAsync(HttpContext contexto, Exception excepcion)
    {
        var (estado, titulo, detalle) = Clasificar(excepcion);

        if (estado >= StatusCodes.Status500InternalServerError)
        {
            _registro.LogError(
                excepcion,
                "Error no controlado al procesar {Metodo} {Ruta}.",
                contexto.Request.Method, contexto.Request.Path);
        }
        else
        {
            _registro.LogWarning(
                "Solicitud rechazada en {Metodo} {Ruta}: {Motivo}",
                contexto.Request.Method, contexto.Request.Path, detalle);
        }

        var problema = new ProblemDetails
        {
            Status = estado,
            Title = titulo,
            Detail = detalle,
            Instance = contexto.Request.Path
        };

        problema.Extensions["traceId"] = contexto.TraceIdentifier;

        // La traza solo se expone fuera de producción, para no filtrar información
        // interna del sistema.
        if (_entorno.IsDevelopment() && estado >= StatusCodes.Status500InternalServerError)
        {
            problema.Extensions["excepcion"] = excepcion.ToString();
        }

        contexto.Response.Clear();
        contexto.Response.StatusCode = estado;
        contexto.Response.ContentType = "application/problem+json";

        await contexto.Response.WriteAsync(JsonSerializer.Serialize(problema, OpcionesJson));
    }

    private static (int Estado, string Titulo, string Detalle) Clasificar(Exception excepcion) =>
        excepcion switch
        {
            ExcepcionTransicionEstadoInvalida transicion => (
                StatusCodes.Status422UnprocessableEntity,
                "Operación no permitida en el estado actual",
                transicion.Message),

            ExcepcionReglaNegocio regla => (
                StatusCodes.Status422UnprocessableEntity,
                "No se pudo completar la operación",
                regla.Message),

            // Otro usuario modificó el registro entre la lectura y la escritura (RNF03).
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Conflicto de concurrencia",
                "Otro usuario modificó esta información mientras usted trabajaba. " +
                "Actualice la pantalla y vuelva a intentarlo."),

            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "Conflicto con los datos existentes",
                "No fue posible guardar los cambios porque entran en conflicto con " +
                "información ya registrada."),

            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Acceso denegado",
                "No cuenta con permisos para realizar esta operación."),

            // 499 (Client Closed Request) no está en StatusCodes: es una extensión de Nginx.
            OperationCanceledException => (
                499,
                "Solicitud cancelada",
                "La solicitud fue cancelada antes de completarse."),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno del servidor",
                "Ocurrió un error inesperado. Si el problema persiste, contacte al " +
                "administrador del sistema.")
        };
}
