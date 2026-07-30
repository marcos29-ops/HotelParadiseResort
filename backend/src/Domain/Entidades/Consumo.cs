using HotelParadiseResort.Domain.Comun;

namespace HotelParadiseResort.Domain.Entidades;

/// <summary>
/// Cargo concreto imputado a una estadía. Es el registro que evita la fuga de capital
/// por consumos no cobrados descrita en los objetivos de la Etapa 1.
/// </summary>
public class Consumo : EntidadBase
{
    public int EstadiaId { get; set; }

    public Estadia? Estadia { get; set; }

    public int ServicioAdicionalId { get; set; }

    public ServicioAdicional? ServicioAdicional { get; set; }

    public required string Descripcion { get; set; }

    public int Cantidad { get; set; } = 1;

    /// <summary>Precio unitario vigente al momento del consumo.</summary>
    public decimal PrecioUnitario { get; set; }

    /// <summary>Fecha y hora en que se prestó el servicio.</summary>
    public DateTime FechaConsumo { get; set; }

    /// <summary>Usuario que registró el cargo.</summary>
    public int UsuarioRegistroId { get; set; }

    public Usuario? UsuarioRegistro { get; set; }

    /// <summary>Importe del cargo: cantidad × precio unitario.</summary>
    public decimal CalcularMonto() =>
        decimal.Round(PrecioUnitario * Cantidad, 2, MidpointRounding.AwayFromZero);
}
