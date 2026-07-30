using FluentAssertions;
using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.State.Reservas;
using HotelParadiseResort.Shared.Excepciones;

namespace HotelParadiseResort.Tests.Dominio;

/// <summary>
/// PATRÓN STATE — Ciclo de vida de la reserva.
/// Regla de negocio verificada: el check-in solo procede sobre una reserva confirmada.
/// </summary>
public sealed class PruebasEstadoReserva
{
    [Fact]
    public void ReservaNueva_NaceEnEstadoPendiente()
    {
        var reserva = CrearReserva();

        reserva.Estado.Should().Be(TipoEstadoReserva.Pendiente);
    }

    [Fact]
    public void ReservaPendiente_NoAdmiteCheckIn()
    {
        var reserva = CrearReserva(TipoEstadoReserva.Pendiente);

        reserva.ObtenerEstado().PermiteCheckIn.Should().BeFalse();
    }

    [Fact]
    public void ReservaConfirmada_AdmiteCheckIn()
    {
        var reserva = CrearReserva(TipoEstadoReserva.Confirmada);

        reserva.ObtenerEstado().PermiteCheckIn.Should().BeTrue();
    }

    [Theory]
    [InlineData(TipoEstadoReserva.Pendiente, TipoEstadoReserva.Confirmada)]
    [InlineData(TipoEstadoReserva.Pendiente, TipoEstadoReserva.Cancelada)]
    [InlineData(TipoEstadoReserva.Confirmada, TipoEstadoReserva.Completada)]
    [InlineData(TipoEstadoReserva.Confirmada, TipoEstadoReserva.Cancelada)]
    public void TransicionesValidas_SeAplican(TipoEstadoReserva origen, TipoEstadoReserva destino)
    {
        var reserva = CrearReserva(origen);

        reserva.CambiarEstado(destino);

        reserva.Estado.Should().Be(destino);
    }

    [Theory]
    [InlineData(TipoEstadoReserva.Pendiente, TipoEstadoReserva.Completada)]
    [InlineData(TipoEstadoReserva.Cancelada, TipoEstadoReserva.Confirmada)]
    [InlineData(TipoEstadoReserva.Completada, TipoEstadoReserva.Cancelada)]
    [InlineData(TipoEstadoReserva.Completada, TipoEstadoReserva.Confirmada)]
    public void TransicionesInvalidas_SonRechazadas(
        TipoEstadoReserva origen, TipoEstadoReserva destino)
    {
        var reserva = CrearReserva(origen);

        var accion = () => reserva.CambiarEstado(destino);

        accion.Should().Throw<ExcepcionTransicionEstadoInvalida>();
    }

    [Theory]
    [InlineData(TipoEstadoReserva.Cancelada)]
    [InlineData(TipoEstadoReserva.Completada)]
    public void EstadosTerminales_NoLiberanHabitacionComprometida(TipoEstadoReserva estado)
    {
        var reserva = CrearReserva(estado);

        reserva.ObtenerEstado().ComprometeHabitacion.Should().BeFalse();
    }

    /// <summary>
    /// Detección de solapamientos: dos estadías que se tocan en un extremo no chocan,
    /// porque quien sale libera la habitación el mismo día en que entra el siguiente.
    /// </summary>
    [Theory]
    [InlineData("2026-08-21", "2026-08-22", true)]   // contenida
    [InlineData("2026-08-19", "2026-08-21", true)]   // solapa el inicio
    [InlineData("2026-08-22", "2026-08-25", true)]   // solapa el final
    [InlineData("2026-08-18", "2026-08-30", true)]   // envuelve por completo
    [InlineData("2026-08-23", "2026-08-26", false)]  // empieza el día de la salida
    [InlineData("2026-08-17", "2026-08-20", false)]  // termina el día de la entrada
    [InlineData("2026-09-01", "2026-09-05", false)]  // sin relación
    public void SeSolapaCon_DetectaConflictosDeFechas(string entrada, string salida, bool esperado)
    {
        var reserva = CrearReserva();
        reserva.FechaEntrada = new DateTime(2026, 8, 20);
        reserva.FechaSalida = new DateTime(2026, 8, 23);

        var resultado = reserva.SeSolapaCon(DateTime.Parse(entrada), DateTime.Parse(salida));

        resultado.Should().Be(esperado);
    }

    private static Reserva CrearReserva(TipoEstadoReserva estado = TipoEstadoReserva.Pendiente) => new()
    {
        Codigo = "RES-00001",
        ClienteId = 1,
        HabitacionId = 1,
        UsuarioRegistroId = 1,
        FechaEntrada = new DateTime(2026, 8, 20),
        FechaSalida = new DateTime(2026, 8, 23),
        CantidadHuespedes = 2,
        Estado = estado,
        CanalOrigen = CanalOrigenReserva.Telefono
    };
}
