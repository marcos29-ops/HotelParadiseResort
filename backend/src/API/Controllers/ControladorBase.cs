using HotelParadiseResort.Shared.Resultados;
using Microsoft.AspNetCore.Mvc;

namespace HotelParadiseResort.API.Controllers;

/// <summary>
/// Base de los controladores. Traduce el <see cref="Resultado"/> de la capa de
/// aplicación al código de estado HTTP correspondiente, de modo que los controladores
/// no contengan lógica de decisión ni conozcan el dominio.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ControladorBase : ControllerBase
{
    /// <summary>Devuelve 200 con el valor, o el error traducido.</summary>
    protected IActionResult Responder<T>(Resultado<T> resultado) =>
        resultado.EsExitoso ? Ok(resultado.Valor) : TraducirError(resultado);

    /// <summary>Devuelve 204 si la operación no produce contenido, o el error traducido.</summary>
    protected IActionResult Responder(Resultado resultado) =>
        resultado.EsExitoso ? NoContent() : TraducirError(resultado);

    /// <summary>Devuelve 201 con la cabecera Location del recurso creado.</summary>
    protected IActionResult ResponderCreado<T>(Resultado<T> resultado, string nombreRuta, object valoresRuta) =>
        resultado.EsExitoso
            ? CreatedAtRoute(nombreRuta, valoresRuta, resultado.Valor)
            : TraducirError(resultado);

    private IActionResult TraducirError(Resultado resultado)
    {
        var (estado, titulo) = resultado.TipoError switch
        {
            TipoError.Validacion => (StatusCodes.Status400BadRequest, "Datos inválidos"),
            TipoError.NoAutenticado => (StatusCodes.Status401Unauthorized, "No autenticado"),
            TipoError.NoAutorizado => (StatusCodes.Status403Forbidden, "Acceso denegado"),
            TipoError.NoEncontrado => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            TipoError.Conflicto => (StatusCodes.Status409Conflict, "Conflicto"),
            TipoError.ReglaNegocio => (StatusCodes.Status422UnprocessableEntity, "Operación no permitida"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
        };

        return Problem(
            detail: resultado.Error,
            statusCode: estado,
            title: titulo,
            instance: HttpContext.Request.Path);
    }
}
