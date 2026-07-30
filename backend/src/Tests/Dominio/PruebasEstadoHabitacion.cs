using FluentAssertions;
using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.State.Habitaciones;
using HotelParadiseResort.Shared.Excepciones;

namespace HotelParadiseResort.Tests.Dominio;

/// <summary>
/// PATRÓN STATE — Transiciones de estado de la habitación.
/// Verifica que cada estado admita únicamente las transiciones documentadas.
/// </summary>
public sealed class PruebasEstadoHabitacion
{
    [Theory]
    [InlineData(TipoEstadoHabitacion.Disponible, TipoEstadoHabitacion.Reservada)]
    [InlineData(TipoEstadoHabitacion.Disponible, TipoEstadoHabitacion.Ocupada)]
    [InlineData(TipoEstadoHabitacion.Disponible, TipoEstadoHabitacion.EnMantenimiento)]
    [InlineData(TipoEstadoHabitacion.Reservada, TipoEstadoHabitacion.Ocupada)]
    [InlineData(TipoEstadoHabitacion.Reservada, TipoEstadoHabitacion.Disponible)]
    [InlineData(TipoEstadoHabitacion.Ocupada, TipoEstadoHabitacion.EnLimpieza)]
    [InlineData(TipoEstadoHabitacion.EnLimpieza, TipoEstadoHabitacion.Disponible)]
    [InlineData(TipoEstadoHabitacion.EnMantenimiento, TipoEstadoHabitacion.Disponible)]
    public void TransicionesValidas_SeAplican(TipoEstadoHabitacion origen, TipoEstadoHabitacion destino)
    {
        var habitacion = CrearHabitacion(origen);

        habitacion.CambiarEstado(destino);

        habitacion.Estado.Should().Be(destino);
    }

    [Theory]
    [InlineData(TipoEstadoHabitacion.Ocupada, TipoEstadoHabitacion.Reservada)]
    [InlineData(TipoEstadoHabitacion.Ocupada, TipoEstadoHabitacion.Disponible)]
    [InlineData(TipoEstadoHabitacion.EnLimpieza, TipoEstadoHabitacion.Ocupada)]
    [InlineData(TipoEstadoHabitacion.Disponible, TipoEstadoHabitacion.Disponible)]
    [InlineData(TipoEstadoHabitacion.EnMantenimiento, TipoEstadoHabitacion.Ocupada)]
    public void TransicionesInvalidas_SonRechazadas(
        TipoEstadoHabitacion origen, TipoEstadoHabitacion destino)
    {
        var habitacion = CrearHabitacion(origen);

        var accion = () => habitacion.CambiarEstado(destino);

        accion.Should().Throw<ExcepcionTransicionEstadoInvalida>();
    }

    [Fact]
    public void HabitacionOcupada_NoAdmiteReservaNiCheckIn()
    {
        var estado = FabricaEstadoHabitacion.Crear(TipoEstadoHabitacion.Ocupada);

        estado.PermiteReservar.Should().BeFalse();
        estado.PermiteCheckIn.Should().BeFalse();
    }

    [Fact]
    public void HabitacionDisponible_AdmiteReservaYCheckIn()
    {
        var estado = FabricaEstadoHabitacion.Crear(TipoEstadoHabitacion.Disponible);

        estado.PermiteReservar.Should().BeTrue();
        estado.PermiteCheckIn.Should().BeTrue();
    }

    [Fact]
    public void HabitacionReservada_AdmiteCheckInPeroNoNuevaReserva()
    {
        var estado = FabricaEstadoHabitacion.Crear(TipoEstadoHabitacion.Reservada);

        estado.PermiteReservar.Should().BeFalse();
        estado.PermiteCheckIn.Should().BeTrue();
    }

    [Fact]
    public void HabitacionInactiva_NoEstaDisponibleAunqueSuEstadoLoPermita()
    {
        var habitacion = CrearHabitacion(TipoEstadoHabitacion.Disponible);
        habitacion.Activo = false;

        habitacion.EstaDisponibleParaReservar().Should().BeFalse();
    }

    private static Habitacion CrearHabitacion(TipoEstadoHabitacion estado) => new()
    {
        Numero = "204",
        Piso = 2,
        TipoHabitacionId = 1,
        Estado = estado
    };
}
