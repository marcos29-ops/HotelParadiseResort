using HotelParadiseResort.Domain.Comun;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.Strategy;

namespace HotelParadiseResort.Domain.Entidades;

/// <summary>
/// Permanencia efectiva del huésped en el hotel: se abre con el check-in y se cierra
/// con el check-out. Los consumos se asocian a la estadía —no a la reserva—, que es
/// el vínculo que el enunciado señala como roto en la operación manual actual.
/// </summary>
public class Estadia : EntidadBase
{
    public int ReservaId { get; set; }

    public Reserva? Reserva { get; set; }

    public int HabitacionId { get; set; }

    public Habitacion? Habitacion { get; set; }

    public DateTime FechaCheckIn { get; set; }

    public DateTime? FechaCheckOut { get; set; }

    /// <summary>Usuario de recepción que registró el check-in.</summary>
    public int UsuarioCheckInId { get; set; }

    public Usuario? UsuarioCheckIn { get; set; }

    public int? UsuarioCheckOutId { get; set; }

    public Usuario? UsuarioCheckOut { get; set; }

    public int CantidadHuespedes { get; set; }

    public TipoEstadoEstadia Estado { get; set; } = TipoEstadoEstadia.EnCurso;

    public string? Observaciones { get; set; }

    public ICollection<Consumo> Consumos { get; set; } = new List<Consumo>();

    public Factura? Factura { get; set; }

    /// <summary>
    /// Noches facturables: las **contratadas en la reserva**, no las que resultan de
    /// los sellos de tiempo reales de entrada y salida.
    ///
    /// Es lo que el huésped acordó pagar y lo que muestra el comprobante ("20/07 al
    /// 23/07 — 3 noches"). Una salida anticipada no reduce el cargo de forma
    /// automática: eso exige modificar la reserva, decisión que corresponde a
    /// recepción y no a un recálculo silencioso.
    /// </summary>
    public int CalcularNoches()
    {
        if (Reserva is not null)
        {
            return CalculadoraNoches.Calcular(Reserva.FechaEntrada, Reserva.FechaSalida);
        }

        // Sin la reserva cargada, se recurre a las fechas propias de la estadía.
        return CalculadoraNoches.Calcular(FechaCheckIn, FechaCheckOut ?? FechaCheckIn);
    }

    public bool EstaAbierta() => Estado == TipoEstadoEstadia.EnCurso;

    public bool PuedeFacturarse() => Estado == TipoEstadoEstadia.Finalizada;
}
