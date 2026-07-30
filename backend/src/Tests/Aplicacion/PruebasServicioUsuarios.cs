using FluentAssertions;
using HotelParadiseResort.Application.Abstracciones;
using HotelParadiseResort.Application.DTOs.Usuarios;
using HotelParadiseResort.Application.Servicios;
using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Shared.Resultados;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HotelParadiseResort.Tests.Aplicacion;

/// <summary>
/// Reglas de administración de usuarios verificadas de forma aislada.
///
/// La salvaguarda del "último administrador" depende del censo completo de usuarios,
/// por lo que se comprueba aquí con dobles de prueba y no contra la base compartida:
/// así el resultado no depende del orden de ejecución.
/// </summary>
public sealed class PruebasServicioUsuarios
{
    private readonly Mock<IUnidadDeTrabajo> _unidadDeTrabajo = new();
    private readonly Mock<IRepositorioUsuario> _usuarios = new();
    private readonly Mock<IServicioContrasena> _contrasenas = new();
    private readonly Mock<IUsuarioActual> _usuarioActual = new();

    public PruebasServicioUsuarios()
    {
        _unidadDeTrabajo.SetupGet(u => u.Usuarios).Returns(_usuarios.Object);
        _unidadDeTrabajo.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _contrasenas.Setup(c => c.Hashear(It.IsAny<string>())).Returns("hash-simulado");
        _usuarioActual.SetupGet(u => u.Id).Returns(99);
    }

    [Fact]
    public async Task DesactivarAlUltimoAdministradorActivo_EsRechazado()
    {
        var administrador = CrearUsuario(1, RolUsuario.Administrador, activo: true);
        ConfigurarCenso(administrador, CrearUsuario(2, RolUsuario.Recepcionista, activo: true));

        var resultado = await CrearServicio().DesactivarAsync(1);

        resultado.EsFallido.Should().BeTrue();
        resultado.TipoError.Should().Be(TipoError.ReglaNegocio);
        resultado.Error.Should().Contain("al menos un administrador");
        administrador.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task DesactivarUnAdministrador_ProcedeSiQuedaOtroActivo()
    {
        var administrador = CrearUsuario(1, RolUsuario.Administrador, activo: true);
        ConfigurarCenso(administrador, CrearUsuario(2, RolUsuario.Administrador, activo: true));

        var resultado = await CrearServicio().DesactivarAsync(1);

        resultado.EsExitoso.Should().BeTrue();
        administrador.Activo.Should().BeFalse();
    }

    [Fact]
    public async Task DesactivarLaPropiaCuentaEnSesion_EsRechazado()
    {
        var propio = CrearUsuario(99, RolUsuario.Administrador, activo: true);
        ConfigurarCenso(propio, CrearUsuario(2, RolUsuario.Administrador, activo: true));

        var resultado = await CrearServicio().DesactivarAsync(99);

        resultado.EsFallido.Should().BeTrue();
        resultado.Error.Should().Contain("propia cuenta");
    }

    [Fact]
    public async Task DegradarAlUltimoAdministrador_EsRechazado()
    {
        var administrador = CrearUsuario(1, RolUsuario.Administrador, activo: true);
        ConfigurarCenso(administrador);

        var resultado = await CrearServicio().ActualizarAsync(1,
            new ActualizarUsuarioDto("Nombre", "correo@paradiseresort.cr", "Recepcionista", true));

        resultado.EsFallido.Should().BeTrue();
        resultado.TipoError.Should().Be(TipoError.ReglaNegocio);
    }

    [Fact]
    public async Task ActualizarUnAdministrador_ProcedeSiConservaSuRol()
    {
        var administrador = CrearUsuario(1, RolUsuario.Administrador, activo: true);
        ConfigurarCenso(administrador);

        var resultado = await CrearServicio().ActualizarAsync(1,
            new ActualizarUsuarioDto("Nombre Nuevo", "nuevo@paradiseresort.cr", "Administrador", true));

        resultado.EsExitoso.Should().BeTrue();
        administrador.Nombre.Should().Be("Nombre Nuevo");
    }

    [Fact]
    public async Task ActualizarUsuarioInexistente_DevuelveNoEncontrado()
    {
        _usuarios.Setup(u => u.ObtenerPorIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var resultado = await CrearServicio().ActualizarAsync(50,
            new ActualizarUsuarioDto("X", "x@paradiseresort.cr", "Recepcionista", true));

        resultado.TipoError.Should().Be(TipoError.NoEncontrado);
    }

    [Fact]
    public async Task CrearUsuario_ConRolDesconocido_EsRechazado()
    {
        var resultado = await CrearServicio().CrearAsync(
            new CrearUsuarioDto("Nombre", "usuario", "u@paradiseresort.cr", "Clave2026Segura", "Gerente"));

        resultado.EsFallido.Should().BeTrue();
        resultado.TipoError.Should().Be(TipoError.Validacion);
    }

    [Fact]
    public async Task CrearUsuario_ConNombreYaRegistrado_DevuelveConflicto()
    {
        _usuarios.Setup(u => u.ExisteNombreUsuarioAsync(
                "repetido", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resultado = await CrearServicio().CrearAsync(
            new CrearUsuarioDto("Nombre", "repetido", "u@paradiseresort.cr", "Clave2026Segura", "Recepcionista"));

        resultado.TipoError.Should().Be(TipoError.Conflicto);
    }

    [Fact]
    public async Task CrearUsuario_AlmacenaLaContrasenaHasheada()
    {
        Usuario? capturado = null;
        _usuarios.Setup(u => u.ExisteNombreUsuarioAsync(
                It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _usuarios.Setup(u => u.AgregarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .Callback<Usuario, CancellationToken>((u, _) => capturado = u)
            .Returns(Task.CompletedTask);

        var resultado = await CrearServicio().CrearAsync(
            new CrearUsuarioDto("Nombre", "nuevo", "n@paradiseresort.cr", "Clave2026Segura", "Recepcionista"));

        resultado.EsExitoso.Should().BeTrue();
        capturado!.ContrasenaHash.Should().Be("hash-simulado");
        capturado.ContrasenaHash.Should().NotContain("Clave2026Segura");
    }

    [Fact]
    public async Task RestablecerContrasenaDeUsuarioInexistente_DevuelveNoEncontrado()
    {
        _usuarios.Setup(u => u.ObtenerPorIdAsync(70, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var resultado = await CrearServicio().RestablecerContrasenaAsync(
            70, new RestablecerContrasenaDto("NuevaClave2026"));

        resultado.TipoError.Should().Be(TipoError.NoEncontrado);
    }

    // --- Utilidades -----------------------------------------------------------

    private ServicioUsuarios CrearServicio() => new(
        _unidadDeTrabajo.Object,
        _contrasenas.Object,
        _usuarioActual.Object,
        NullLogger<ServicioUsuarios>.Instance);

    private static Usuario CrearUsuario(int id, RolUsuario rol, bool activo) => new()
    {
        Id = id,
        Nombre = $"Usuario {id}",
        NombreUsuario = $"usuario{id}",
        Correo = $"usuario{id}@paradiseresort.cr",
        ContrasenaHash = "hash",
        Rol = rol,
        Activo = activo
    };

    private void ConfigurarCenso(params Usuario[] usuarios)
    {
        _usuarios.Setup(u => u.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarios);

        foreach (var usuario in usuarios)
        {
            var capturado = usuario;
            _usuarios.Setup(u => u.ObtenerPorIdAsync(capturado.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(capturado);
        }
    }
}
