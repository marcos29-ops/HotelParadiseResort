using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Persistence.Contexto;
using HotelParadiseResort.Persistence.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotelParadiseResort.Persistence.Configuracion;

/// <summary>Registro del contexto de datos y de la unidad de trabajo.</summary>
public static class RegistroPersistencia
{
    public const string NombreCadenaConexion = "HotelDB";

    public static IServiceCollection AgregarPersistencia(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var cadenaConexion = configuracion.GetConnectionString(NombreCadenaConexion);

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                $"No se encontró la cadena de conexión '{NombreCadenaConexion}'. Configúrela " +
                "mediante variables de entorno o el gestor de secretos.");
        }

        servicios.AddDbContext<HotelDbContext>(opciones =>
            opciones.UseSqlServer(cadenaConexion, sql =>
            {
                sql.MigrationsAssembly(typeof(HotelDbContext).Assembly.FullName);

                // Reintenta ante fallos transitorios de red o del motor, requisito para
                // sostener la disponibilidad con 50 usuarios concurrentes (RNF03).
                sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);

                sql.CommandTimeout(30);
            }));

        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();

        return servicios;
    }
}
