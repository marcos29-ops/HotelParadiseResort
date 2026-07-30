namespace HotelParadiseResort.Shared.Resultados;

/// <summary>
/// Clasifica el motivo de un fallo de negocio para que la capa de API pueda
/// traducirlo al código de estado HTTP correspondiente sin conocer el dominio.
/// </summary>
public enum TipoError
{
    Ninguno = 0,

    /// <summary>Datos de entrada inválidos. Se traduce a HTTP 400.</summary>
    Validacion = 1,

    /// <summary>El recurso solicitado no existe. Se traduce a HTTP 404.</summary>
    NoEncontrado = 2,

    /// <summary>Conflicto con el estado actual del recurso. Se traduce a HTTP 409.</summary>
    Conflicto = 3,

    /// <summary>La operación viola una regla de negocio. Se traduce a HTTP 422.</summary>
    ReglaNegocio = 4,

    /// <summary>Credenciales ausentes o inválidas. Se traduce a HTTP 401.</summary>
    NoAutenticado = 5,

    /// <summary>El usuario carece de permisos para la operación. Se traduce a HTTP 403.</summary>
    NoAutorizado = 6
}
