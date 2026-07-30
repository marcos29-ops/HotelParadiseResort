using FluentAssertions;
using HotelParadiseResort.Infrastructure.Seguridad;

namespace HotelParadiseResort.Tests.Seguridad;

/// <summary>
/// Resguardo de credenciales (RNF01). Verifica que las contraseñas nunca se almacenen
/// en claro y que el hash resista los errores de entrada más comunes.
/// </summary>
public sealed class PruebasServicioContrasena
{
    private readonly ServicioContrasena _servicio = new();

    [Fact]
    public void Hash_NoContieneLaContrasenaEnClaro()
    {
        const string contrasena = "Paradise2026!";

        var hash = _servicio.Hashear(contrasena);

        hash.Should().NotContain(contrasena);
    }

    [Fact]
    public void Verificar_AceptaLaContrasenaCorrecta()
    {
        var hash = _servicio.Hashear("Paradise2026!");

        _servicio.Verificar("Paradise2026!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verificar_RechazaUnaContrasenaIncorrecta()
    {
        var hash = _servicio.Hashear("Paradise2026!");

        _servicio.Verificar("Paradise2027!", hash).Should().BeFalse();
    }

    [Fact]
    public void Verificar_DistingueMayusculasDeMinusculas()
    {
        var hash = _servicio.Hashear("Paradise2026!");

        _servicio.Verificar("paradise2026!", hash).Should().BeFalse();
    }

    [Fact]
    public void MismaContrasena_ProduceHashesDistintosPorLaSalAleatoria()
    {
        var primero = _servicio.Hashear("Paradise2026!");
        var segundo = _servicio.Hashear("Paradise2026!");

        primero.Should().NotBe(segundo);
        _servicio.Verificar("Paradise2026!", primero).Should().BeTrue();
        _servicio.Verificar("Paradise2026!", segundo).Should().BeTrue();
    }

    [Fact]
    public void Hash_RegistraLasIteracionesParaPermitirElevarlasEnElFuturo()
    {
        var hash = _servicio.Hashear("Paradise2026!");

        var partes = hash.Split('.');
        partes.Should().HaveCount(3);
        int.Parse(partes[0]).Should().BeGreaterThanOrEqualTo(210_000);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("formato-invalido")]
    [InlineData("no.es.base64")]
    public void Verificar_RechazaHashesMalFormados(string hashAlmacenado)
    {
        _servicio.Verificar("Paradise2026!", hashAlmacenado).Should().BeFalse();
    }

    [Fact]
    public void Hashear_RechazaUnaContrasenaVacia()
    {
        var accion = () => _servicio.Hashear("   ");

        accion.Should().Throw<ArgumentException>();
    }
}
