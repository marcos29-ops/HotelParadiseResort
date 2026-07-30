using HotelParadiseResort.Application.Abstracciones;
using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Persistence.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HotelParadiseResort.Persistence.Inicializacion;

/// <summary>
/// Aplica las migraciones pendientes y siembra los datos mínimos para que el sistema
/// sea operable en su primer arranque: la cuenta de administrador —sin la cual nadie
/// podría autenticarse— y los catálogos de tipos de habitación y servicios que la
/// documentación describe.
///
/// No se siembran clientes, reservas ni facturas: esos son datos reales de operación.
/// </summary>
public sealed class InicializadorBaseDatos
{
    private const string VariableContrasenaAdmin = "Seed:AdminPassword";

    private readonly HotelDbContext _contexto;
    private readonly IServicioContrasena _servicioContrasena;
    private readonly IConfiguration _configuracion;
    private readonly ILogger<InicializadorBaseDatos> _registro;

    public InicializadorBaseDatos(
        HotelDbContext contexto,
        IServicioContrasena servicioContrasena,
        IConfiguration configuracion,
        ILogger<InicializadorBaseDatos> registro)
    {
        _contexto = contexto;
        _servicioContrasena = servicioContrasena;
        _configuracion = configuracion;
        _registro = registro;
    }

    public async Task InicializarAsync(CancellationToken cancelacion = default)
    {
        await AplicarMigracionesAsync(cancelacion);
        await SembrarAdministradorAsync(cancelacion);
        await SembrarTiposHabitacionAsync(cancelacion);
        await SembrarServiciosAdicionalesAsync(cancelacion);

        await _contexto.SaveChangesAsync(cancelacion);
    }

    private async Task AplicarMigracionesAsync(CancellationToken cancelacion)
    {
        var pendientes = (await _contexto.Database.GetPendingMigrationsAsync(cancelacion)).ToList();

        if (pendientes.Count == 0)
        {
            _registro.LogInformation("La base de datos está actualizada.");
            return;
        }

        _registro.LogInformation(
            "Aplicando {Cantidad} migración(es) pendiente(s).", pendientes.Count);

        await _contexto.Database.MigrateAsync(cancelacion);
    }

    /// <summary>
    /// Crea la cuenta de administrador inicial. La contraseña proviene de la
    /// configuración —variable de entorno o gestor de secretos—; nunca está en el código.
    /// </summary>
    private async Task SembrarAdministradorAsync(CancellationToken cancelacion)
    {
        if (await _contexto.Usuarios.AnyAsync(u => u.Rol == RolUsuario.Administrador, cancelacion))
        {
            return;
        }

        var contrasena = _configuracion[VariableContrasenaAdmin];

        if (string.IsNullOrWhiteSpace(contrasena))
        {
            throw new InvalidOperationException(
                $"No existe ninguna cuenta de administrador y no se configuró '{VariableContrasenaAdmin}'. " +
                "Defina la variable de entorno 'Seed__AdminPassword' para el primer arranque.");
        }

        _contexto.Usuarios.Add(new Usuario
        {
            Nombre = "Administrador del sistema",
            NombreUsuario = "admin",
            Correo = "admin@paradiseresort.cr",
            ContrasenaHash = _servicioContrasena.Hashear(contrasena),
            Rol = RolUsuario.Administrador,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });

        _registro.LogInformation("Se creó la cuenta de administrador inicial.");
    }

    /// <summary>
    /// Tipos de habitación de referencia. La tarifa de la categoría "Doble – Vista al
    /// mar" es de $85.00 por noche, tal como aparece en los wireframes de la Etapa 2.
    /// </summary>
    private async Task SembrarTiposHabitacionAsync(CancellationToken cancelacion)
    {
        if (await _contexto.TiposHabitacion.AnyAsync(cancelacion))
        {
            return;
        }

        _contexto.TiposHabitacion.AddRange(
            new TipoHabitacion
            {
                Nombre = "Individual",
                Descripcion = "Habitación individual con cama sencilla.",
                TarifaBasePorNoche = 55.00m,
                CapacidadMaxima = 1,
                Activo = true
            },
            new TipoHabitacion
            {
                Nombre = "Doble – Vista al mar",
                Descripcion = "Habitación doble con balcón y vista al mar.",
                TarifaBasePorNoche = 85.00m,
                CapacidadMaxima = 2,
                Activo = true
            },
            new TipoHabitacion
            {
                Nombre = "Doble estándar",
                Descripcion = "Habitación doble con vista interna.",
                TarifaBasePorNoche = 70.00m,
                CapacidadMaxima = 2,
                Activo = true
            },
            new TipoHabitacion
            {
                Nombre = "Suite familiar",
                Descripcion = "Suite con sala independiente y dos habitaciones.",
                TarifaBasePorNoche = 145.00m,
                CapacidadMaxima = 5,
                Activo = true
            });

        _registro.LogInformation("Se sembró el catálogo de tipos de habitación.");
    }

    /// <summary>
    /// Servicios complementarios descritos en el enunciado: restaurante, lavandería,
    /// transporte y actividades recreativas.
    /// </summary>
    private async Task SembrarServiciosAdicionalesAsync(CancellationToken cancelacion)
    {
        if (await _contexto.ServiciosAdicionales.AnyAsync(cancelacion))
        {
            return;
        }

        _contexto.ServiciosAdicionales.AddRange(
            new ServicioAdicional
            {
                Nombre = "Restaurante",
                Tipo = TipoServicioAdicional.Restaurante,
                Descripcion = "Consumos del restaurante del hotel.",
                PrecioBase = 0m,
                Activo = true
            },
            new ServicioAdicional
            {
                Nombre = "Desayuno bufé",
                Tipo = TipoServicioAdicional.Restaurante,
                Descripcion = "Desayuno bufé por persona.",
                PrecioBase = 12.00m,
                Activo = true
            },
            new ServicioAdicional
            {
                Nombre = "Lavandería",
                Tipo = TipoServicioAdicional.Lavanderia,
                Descripcion = "Servicio de lavandería por prenda.",
                PrecioBase = 3.00m,
                Activo = true
            },
            new ServicioAdicional
            {
                Nombre = "Transporte aeropuerto",
                Tipo = TipoServicioAdicional.Transporte,
                Descripcion = "Traslado entre el aeropuerto y el hotel.",
                PrecioBase = 18.00m,
                Activo = true
            },
            new ServicioAdicional
            {
                Nombre = "Actividad recreativa",
                Tipo = TipoServicioAdicional.ActividadRecreativa,
                Descripcion = "Actividades y tours organizados por el hotel.",
                PrecioBase = 25.00m,
                Activo = true
            });

        _registro.LogInformation("Se sembró el catálogo de servicios adicionales.");
    }
}
