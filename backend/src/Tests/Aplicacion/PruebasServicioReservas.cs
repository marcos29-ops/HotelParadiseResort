using FluentAssertions;
using HotelParadiseResort.Application.Abstracciones;
using HotelParadiseResort.Application.DTOs.Reservas;
using HotelParadiseResort.Application.Servicios;
using HotelParadiseResort.Application.Tarifas;
using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.Strategy;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Shared.Resultados;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HotelParadiseResort.Tests.Aplicacion;

/// <summary>
/// Regla de negocio central del sistema: no se confirma una reserva si la habitación
/// no está libre en el rango solicitado.
///
/// Las dependencias se sustituyen con Moq; no se emplea ninguna base de datos en
/// memoria, conforme a la restricción de motor del proyecto.
/// </summary>
public sealed class PruebasServicioReservas
{
    private static readonly DateTime Hoy = new(2026, 9, 20);

    private readonly Mock<IUnidadDeTrabajo> _unidadDeTrabajo = new();
    private readonly Mock<IRepositorioHabitacion> _habitaciones = new();
    private readonly Mock<IRepositorioReserva> _reservas = new();
    private readonly Mock<IRepositorioCliente> _clientes = new();
    private readonly Mock<IRepositorioEstadia> _estadias = new();

    public PruebasServicioReservas()
    {
        _unidadDeTrabajo.SetupGet(u => u.Habitaciones).Returns(_habitaciones.Object);
        _unidadDeTrabajo.SetupGet(u => u.Reservas).Returns(_reservas.Object);
        _unidadDeTrabajo.SetupGet(u => u.Clientes).Returns(_clientes.Object);
        _unidadDeTrabajo.SetupGet(u => u.Estadias).Returns(_estadias.Object);

        // La transacción se ejecuta directamente: lo que se verifica aquí es la regla
        // de negocio, no el comportamiento transaccional del motor.
        _unidadDeTrabajo
            .Setup(u => u.EjecutarEnTransaccionAsync(
                It.IsAny<Func<CancellationToken, Task<Resultado<int>>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<Resultado<int>>>, CancellationToken>(
                (operacion, ct) => operacion(ct));

        _reservas.Setup(r => r.GenerarCodigoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("RES-00001");
    }

    [Fact]
    public async Task NoConfirmaLaReserva_SiLaHabitacionNoEstaDisponible()
    {
        ConfigurarClienteExistente();
        ConfigurarHabitacion(capacidadMaxima: 2);
        _habitaciones
            .Setup(h => h.EstaDisponibleAsync(
                It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await CrearServicio().CrearAsync(SolicitudValida());

        resultado.EsFallido.Should().BeTrue();
        resultado.TipoError.Should().Be(TipoError.Conflicto);
        resultado.Error.Should().Contain("no está disponible");

        _reservas.Verify(r => r.AgregarAsync(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreaLaReservaEnEstadoPendiente_CuandoHayDisponibilidad()
    {
        ConfigurarClienteExistente();
        ConfigurarHabitacion(capacidadMaxima: 2);
        ConfigurarDisponibilidad(disponible: true);

        Reserva? capturada = null;
        _reservas
            .Setup(r => r.AgregarAsync(It.IsAny<Reserva>(), It.IsAny<CancellationToken>()))
            .Callback<Reserva, CancellationToken>((r, _) => capturada = r)
            .Returns(Task.CompletedTask);

        _reservas.Setup(r => r.ObtenerCompletaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => capturada);

        var resultado = await CrearServicio().CrearAsync(SolicitudValida());

        resultado.EsExitoso.Should().BeTrue();
        capturada.Should().NotBeNull();
        // Toda reserva nace pendiente y requiere confirmación explícita.
        capturada!.Estado.Should().Be(TipoEstadoReserva.Pendiente);
        capturada.MontoEstimado.Should().Be(255.00m);
        capturada.CanalOrigen.Should().Be(CanalOrigenReserva.Telefono);
    }

    [Fact]
    public async Task RechazaLaReserva_SiSuperaLaCapacidadDeLaHabitacion()
    {
        ConfigurarClienteExistente();
        ConfigurarHabitacion(capacidadMaxima: 2);
        ConfigurarDisponibilidad(disponible: true);

        var resultado = await CrearServicio().CrearAsync(SolicitudValida() with { CantidadHuespedes = 5 });

        resultado.EsFallido.Should().BeTrue();
        resultado.TipoError.Should().Be(TipoError.ReglaNegocio);
        resultado.Error.Should().Contain("máximo");
    }

    [Fact]
    public async Task RechazaLaReserva_ConFechaDeEntradaAnteriorAHoy()
    {
        var resultado = await CrearServicio().CrearAsync(
            SolicitudValida() with { FechaEntrada = Hoy.AddDays(-5), FechaSalida = Hoy.AddDays(-2) });

        resultado.EsFallido.Should().BeTrue();
        resultado.TipoError.Should().Be(TipoError.Validacion);
    }

    [Fact]
    public async Task RechazaLaReserva_ConSalidaAnteriorALaEntrada()
    {
        var resultado = await CrearServicio().CrearAsync(
            SolicitudValida() with { FechaEntrada = Hoy.AddDays(5), FechaSalida = Hoy.AddDays(2) });

        resultado.EsFallido.Should().BeTrue();
        resultado.TipoError.Should().Be(TipoError.Validacion);
    }

    [Fact]
    public async Task RechazaLaReserva_ConUnCanalDeOrigenDesconocido()
    {
        var resultado = await CrearServicio().CrearAsync(
            SolicitudValida() with { CanalOrigen = "Telepatía" });

        resultado.EsFallido.Should().BeTrue();
        resultado.TipoError.Should().Be(TipoError.Validacion);
    }

    // --- Utilidades del escenario ---------------------------------------------

    private ServicioReservas CrearServicio()
    {
        var reloj = new Mock<IProveedorFechaHora>();
        reloj.SetupGet(r => r.Ahora).Returns(Hoy);
        // La validación de fechas de negocio usa la fecha operativa del hotel, no la UTC.
        reloj.SetupGet(r => r.FechaOperativa).Returns(Hoy.Date);

        var usuarioActual = new Mock<IUsuarioActual>();
        usuarioActual.SetupGet(u => u.Id).Returns(1);

        var selector = new SelectorEstrategiaTarifa([new TarifaEstandar(), new TarifaTemporadaAlta()]);

        return new ServicioReservas(
            _unidadDeTrabajo.Object,
            selector,
            usuarioActual.Object,
            reloj.Object,
            NullLogger<ServicioReservas>.Instance);
    }

    private static CrearReservaDto SolicitudValida() => new(
        ClienteId: 1,
        ClienteNuevo: null,
        HabitacionId: 1,
        FechaEntrada: Hoy,
        FechaSalida: Hoy.AddDays(3),
        CantidadHuespedes: 2,
        CanalOrigen: "Telefono",
        Observaciones: null);

    private void ConfigurarClienteExistente() =>
        _clientes.Setup(c => c.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cliente
            {
                Id = 1,
                Identificacion = "1-0234-0567",
                Nombre = "Ana",
                Apellidos = "Pérez Ramírez"
            });

    private void ConfigurarHabitacion(int capacidadMaxima) =>
        _habitaciones.Setup(h => h.ObtenerConTipoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Habitacion
            {
                Id = 1,
                Numero = "204",
                Piso = 2,
                TipoHabitacionId = 1,
                Estado = TipoEstadoHabitacion.Disponible,
                TipoHabitacion = new TipoHabitacion
                {
                    Id = 1,
                    Nombre = "Doble – Vista al mar",
                    TarifaBasePorNoche = 85.00m,
                    CapacidadMaxima = capacidadMaxima
                }
            });

    private void ConfigurarDisponibilidad(bool disponible) =>
        _habitaciones.Setup(h => h.EstaDisponibleAsync(
                It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(disponible);
}
