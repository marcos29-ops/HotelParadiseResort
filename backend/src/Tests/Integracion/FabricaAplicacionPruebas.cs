using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelParadiseResort.Application.DTOs.Autenticacion;
using HotelParadiseResort.Persistence.Contexto;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotelParadiseResort.Tests.Integracion;

/// <summary>
/// Levanta la API completa contra una base de datos **real y dedicada a pruebas**
/// (`HotelDB_Pruebas`), separada de la de desarrollo.
///
/// No se emplea una base en memoria: el proyecto lo prohíbe expresamente y, además,
/// ocultaría precisamente lo que estas pruebas deben verificar —índices únicos,
/// restricciones CHECK, claves foráneas, concurrencia optimista y collation.
/// </summary>
public sealed class FabricaAplicacionPruebas : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ContrasenaAdmin = "Pruebas2026!";

    private const string NombreBaseDatos = "HotelDB_Pruebas";

    private readonly string _cadenaConexion;

    public FabricaAplicacionPruebas()
    {
        var servidor = Environment.GetEnvironmentVariable("PRUEBAS_SQL_SERVIDOR") ?? "localhost,1433";
        var usuario = Environment.GetEnvironmentVariable("PRUEBAS_SQL_USUARIO") ?? "sa";
        var contrasena = Environment.GetEnvironmentVariable("PRUEBAS_SQL_PASSWORD") ?? string.Empty;

        _cadenaConexion =
            $"Server={servidor};Database={NombreBaseDatos};User Id={usuario};" +
            $"Password={contrasena};TrustServerCertificate=True;";
    }

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseEnvironment(Environments.Development);

        constructor.UseSetting("ConnectionStrings:HotelDB", _cadenaConexion);
        constructor.UseSetting("Jwt:Clave", "clave-de-pruebas-de-integracion-con-longitud-suficiente-2026");
        constructor.UseSetting("Jwt:Emisor", "ParadiseResort.API");
        constructor.UseSetting("Jwt:Audiencia", "ParadiseResort.Client");
        constructor.UseSetting("Seed:AdminPassword", ContrasenaAdmin);
    }

    public async Task InitializeAsync()
    {
        // El primer acceso al cliente dispara el arranque: migraciones y seed.
        using var _ = CreateClient();
        await LimpiarDatosTransaccionalesAsync();
    }

    public new async Task DisposeAsync()
    {
        using (var ambito = Services.CreateScope())
        {
            var contexto = ambito.ServiceProvider.GetRequiredService<HotelDbContext>();
            await contexto.Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
    }

    /// <summary>
    /// Vacía las tablas transaccionales entre clases de prueba, conservando los
    /// catálogos y el usuario administrador que siembra el inicializador.
    /// </summary>
    public async Task LimpiarDatosTransaccionalesAsync()
    {
        using var ambito = Services.CreateScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<HotelDbContext>();

        // Sentencias literales, en el orden que respeta las claves foráneas. No se
        // construyen por interpolación para no introducir SQL dinámico ni siquiera
        // en el código de pruebas.
        string[] sentencias =
        [
            "DELETE FROM [DetalleFactura]",
            "DELETE FROM [Factura]",
            "DELETE FROM [Consumo]",
            "DELETE FROM [Estadia]",
            "DELETE FROM [Reserva]",
            "DELETE FROM [HistorialCliente]",
            "DELETE FROM [Cliente]",
            "DELETE FROM [Habitacion]"
        ];

        foreach (var sentencia in sentencias)
        {
            await contexto.Database.ExecuteSqlRawAsync(sentencia);
        }
    }

    /// <summary>Cliente HTTP autenticado con el rol indicado.</summary>
    public async Task<HttpClient> CrearClienteAutenticadoAsync(
        string nombreUsuario = "admin", string? contrasena = null)
    {
        var cliente = CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacion/iniciar-sesion",
            new SolicitudInicioSesionDto(nombreUsuario, contrasena ?? ContrasenaAdmin));

        respuesta.EnsureSuccessStatusCode();

        var sesion = await respuesta.Content.ReadFromJsonAsync<RespuestaInicioSesionDto>();

        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", sesion!.Token);

        return cliente;
    }

    /// <summary>Acceso directo al contexto para preparar o verificar el estado.</summary>
    public async Task<T> EjecutarEnContextoAsync<T>(Func<HotelDbContext, Task<T>> operacion)
    {
        using var ambito = Services.CreateScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<HotelDbContext>();
        return await operacion(contexto);
    }
}

/// <summary>
/// Colección que comparte una única instancia de la API entre las clases de prueba,
/// evitando levantar el host y migrar la base una vez por clase.
/// </summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionIntegracion : ICollectionFixture<FabricaAplicacionPruebas>
{
    public const string Nombre = "Integracion";
}
