using HotelParadiseResort.Application.Abstracciones;
using HotelParadiseResort.Infrastructure.Seguridad;
using HotelParadiseResort.Infrastructure.Servicios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotelParadiseResort.Infrastructure.Configuracion;

/// <summary>
/// Registro de los servicios técnicos. Cada capa expone su propio método de extensión
/// para que <c>Program</c> no conozca las implementaciones concretas.
/// </summary>
public static class RegistroInfraestructura
{
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.Configure<OpcionesJwt>(configuracion.GetSection(OpcionesJwt.SeccionConfiguracion));

        servicios.AddHttpContextAccessor();

        servicios.AddSingleton<IServicioContrasena, ServicioContrasena>();
        servicios.AddSingleton<IProveedorFechaHora>(_ => new ProveedorFechaHora(configuracion));
        servicios.AddScoped<IServicioToken, ServicioToken>();
        servicios.AddScoped<IUsuarioActual, UsuarioActual>();

        return servicios;
    }
}
