namespace HotelParadiseResort.API.Configuracion;

/// <summary>
/// Política CORS para el cliente SPA. Los orígenes permitidos se declaran en la
/// configuración: nunca se abre la API a cualquier origen.
/// </summary>
public static class RegistroCors
{
    public const string PoliticaFrontend = "PoliticaFrontend";

    public static IServiceCollection AgregarCors(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var origenesPermitidos = configuracion
            .GetSection("Cors:OrigenesPermitidos")
            .Get<string[]>() ?? [];

        servicios.AddCors(opciones =>
            opciones.AddPolicy(PoliticaFrontend, politica =>
            {
                if (origenesPermitidos.Length == 0)
                {
                    return;
                }

                politica
                    .WithOrigins(origenesPermitidos)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }));

        return servicios;
    }
}
