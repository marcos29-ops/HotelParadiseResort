using Microsoft.OpenApi.Models;

namespace HotelParadiseResort.API.Configuracion;

/// <summary>
/// Documentación interactiva de la API. Incorpora el esquema de seguridad para que
/// Swagger UI permita probar los endpoints protegidos con un token real.
/// </summary>
public static class RegistroSwagger
{
    public static IServiceCollection AgregarSwagger(this IServiceCollection servicios)
    {
        servicios.AddEndpointsApiExplorer();

        servicios.AddSwaggerGen(opciones =>
        {
            opciones.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Paradise Resort — Sistema de Gestión Hotelera",
                Description =
                    "API REST del sistema de gestión hotelera Paradise Resort. Expone los seis " +
                    "componentes de negocio a través de la fachada de servicios: clientes, " +
                    "habitaciones y disponibilidad, reservas, estadías y consumos, facturación " +
                    "y reportes administrativos.",
                Contact = new OpenApiContact
                {
                    Name = "Universidad Latina de Costa Rica — Análisis y Diseño de Sistemas II"
                }
            });

            var esquemaSeguridad = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "Autentíquese en /api/v1/autenticacion/iniciar-sesion y pegue aquí el token " +
                    "recibido. No anteponga la palabra 'Bearer'.",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            opciones.AddSecurityDefinition("Bearer", esquemaSeguridad);

            opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [esquemaSeguridad] = Array.Empty<string>()
            });

            // Incorpora los comentarios XML de los controladores a la documentación.
            var archivoXml = $"{typeof(RegistroSwagger).Assembly.GetName().Name}.xml";
            var rutaXml = Path.Combine(AppContext.BaseDirectory, archivoXml);

            if (File.Exists(rutaXml))
            {
                opciones.IncludeXmlComments(rutaXml);
            }
        });

        return servicios;
    }
}
