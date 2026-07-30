namespace HotelParadiseResort.Domain.Enumeraciones;

/// <summary>
/// Canal por el que ingresó la solicitud de reserva. El enunciado identifica la
/// dispersión multi-canal como una de las causas de los errores actuales, por lo que
/// el sistema registra el origen de cada reserva.
/// </summary>
public enum CanalOrigenReserva
{
    Telefono = 1,
    CorreoElectronico = 2,
    RedesSociales = 3,
    Presencial = 4,

    /// <summary>Reservada para la integración futura con plataformas externas (OTA).</summary>
    PlataformaExterna = 5
}
