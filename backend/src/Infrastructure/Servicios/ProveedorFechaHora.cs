using HotelParadiseResort.Application.Abstracciones;
using Microsoft.Extensions.Configuration;

namespace HotelParadiseResort.Infrastructure.Servicios;

/// <summary>
/// Fuente de tiempo del sistema.
///
/// Las marcas de tiempo se guardan en UTC para que la operación sea consistente con
/// independencia de dónde esté desplegado el servidor. En cambio, las reglas que
/// razonan sobre días del calendario usan <see cref="FechaOperativa"/>, expresada en
/// la zona horaria del hotel.
/// </summary>
public sealed class ProveedorFechaHora : IProveedorFechaHora
{
    /// <summary>Zona por defecto: la del hotel, en Costa Rica.</summary>
    private const string ZonaPorDefecto = "America/Costa_Rica";

    private readonly TimeZoneInfo _zonaHotel;

    public ProveedorFechaHora(IConfiguration configuracion)
    {
        var identificador = configuracion["Hotel:ZonaHoraria"] ?? ZonaPorDefecto;

        // Si el sistema operativo no reconoce el identificador, se recurre a la zona
        // local del servidor en lugar de impedir el arranque de la aplicación.
        _zonaHotel = TimeZoneInfo.FindSystemTimeZoneById(identificador) is { } zona
            ? zona
            : TimeZoneInfo.Local;
    }

    public DateTime Ahora => DateTime.UtcNow;

    public DateTime HoyUtc => DateTime.UtcNow.Date;

    public DateTime FechaOperativa =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zonaHotel).Date;
}
