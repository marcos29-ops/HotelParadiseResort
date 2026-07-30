using System.Text;
using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using HotelParadiseResort.API.Configuracion;
using HotelParadiseResort.API.Middleware;
using HotelParadiseResort.Application.Configuracion;
using HotelParadiseResort.Infrastructure.Configuracion;
using HotelParadiseResort.Infrastructure.Seguridad;
using HotelParadiseResort.Persistence.Configuracion;
using HotelParadiseResort.Persistence.Inicializacion;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var constructor = WebApplication.CreateBuilder(args);

// Logging estructurado. La configuración vive en appsettings para poder ajustar el
// nivel por entorno sin recompilar.
constructor.Host.UseSerilog((contexto, configuracion) =>
    configuracion.ReadFrom.Configuration(contexto.Configuration));

// --- Capas de la solución -------------------------------------------------------
constructor.Services.AgregarPersistencia(constructor.Configuration);
constructor.Services.AgregarInfraestructura(constructor.Configuration);
constructor.Services.AgregarAplicacion();
constructor.Services.AddScoped<InicializadorBaseDatos>();

// --- Autenticación y autorización (RNF01) ---------------------------------------
var opcionesJwt = constructor.Configuration
    .GetSection(OpcionesJwt.SeccionConfiguracion)
    .Get<OpcionesJwt>() ?? new OpcionesJwt();

if (string.IsNullOrWhiteSpace(opcionesJwt.Clave) ||
    Encoding.UTF8.GetByteCount(opcionesJwt.Clave) < OpcionesJwt.LongitudMinimaClave)
{
    throw new InvalidOperationException(
        $"La clave de firma JWT debe definirse y tener al menos {OpcionesJwt.LongitudMinimaClave} " +
        "bytes. Configure 'Jwt:Clave' mediante la variable de entorno 'Jwt__Clave' o " +
        "el gestor de secretos. Nunca la incluya en archivos versionados.");
}

constructor.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.RequireHttpsMetadata = !constructor.Environment.IsDevelopment();
        opciones.SaveToken = true;

        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = opcionesJwt.Emisor,
            ValidAudience = opcionesJwt.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcionesJwt.Clave)),
            // Sin margen de tolerancia: el token expira cuando dice que expira.
            ClockSkew = TimeSpan.Zero
        };
    });

constructor.Services.AddAuthorization();

// --- MVC, validación y versionado ------------------------------------------------
constructor.Services
    .AddControllers()
    .AddJsonOptions(opciones =>
    {
        opciones.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
        opciones.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

constructor.Services.AddFluentValidationAutoValidation();
constructor.Services.AddValidatorsFromAssemblyContaining<RegistroAplicacionAncla>();

constructor.Services
    .AddApiVersioning(opciones =>
    {
        opciones.DefaultApiVersion = new ApiVersion(1, 0);
        opciones.AssumeDefaultVersionWhenUnspecified = true;
        opciones.ReportApiVersions = true;
        opciones.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(opciones =>
    {
        opciones.GroupNameFormat = "'v'VVV";
        opciones.SubstituteApiVersionInUrl = true;
    });

constructor.Services.AgregarSwagger();
constructor.Services.AgregarCors(constructor.Configuration);

var aplicacion = constructor.Build();

// --- Canalización de peticiones ---------------------------------------------------
// El manejo de errores va primero para capturar cualquier fallo posterior.
aplicacion.UseMiddleware<MiddlewareManejoErrores>();

aplicacion.UseSerilogRequestLogging();

if (aplicacion.Environment.IsDevelopment())
{
    aplicacion.UseSwagger();
    aplicacion.UseSwaggerUI(opciones =>
    {
        opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Paradise Resort API v1");
        opciones.DocumentTitle = "Paradise Resort — API";
        opciones.RoutePrefix = "swagger";
    });
}
else
{
    aplicacion.UseHsts();
    aplicacion.UseHttpsRedirection();
}

aplicacion.UseCors(RegistroCors.PoliticaFrontend);

aplicacion.UseAuthentication();
aplicacion.UseAuthorization();

aplicacion.MapControllers();

// Comprobación de estado para el pipeline de despliegue y el ambiente de Staging.
aplicacion.MapGet("/salud", () => Results.Ok(new
{
    estado = "activo",
    servicio = "Paradise Resort API",
    version = "1.0",
    fecha = DateTime.UtcNow
})).AllowAnonymous().ExcludeFromDescription();

// --- Migraciones y datos iniciales ------------------------------------------------
await using (var ambito = aplicacion.Services.CreateAsyncScope())
{
    var inicializador = ambito.ServiceProvider.GetRequiredService<InicializadorBaseDatos>();
    await inicializador.InicializarAsync();
}

await aplicacion.RunAsync();

/// <summary>Punto de entrada expuesto para las pruebas de integración.</summary>
public partial class Program;
