namespace HotelParadiseResort.Infrastructure.Seguridad;

/// <summary>
/// Parámetros de emisión y validación de los tokens. La clave nunca se versiona:
/// proviene de variables de entorno o del gestor de secretos.
/// </summary>
public sealed class OpcionesJwt
{
    public const string SeccionConfiguracion = "Jwt";

    /// <summary>Longitud mínima de la clave de firma para HMAC-SHA256.</summary>
    public const int LongitudMinimaClave = 32;

    public string Clave { get; set; } = string.Empty;

    public string Emisor { get; set; } = string.Empty;

    public string Audiencia { get; set; } = string.Empty;

    public int MinutosExpiracion { get; set; } = 60;
}
