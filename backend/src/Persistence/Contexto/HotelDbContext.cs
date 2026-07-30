using System.Reflection;
using HotelParadiseResort.Domain.Comun;
using HotelParadiseResort.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace HotelParadiseResort.Persistence.Contexto;

/// <summary>
/// Contexto de Entity Framework Core del sistema. Las configuraciones de cada entidad
/// viven en clases <c>IEntityTypeConfiguration</c> separadas para no concentrar el
/// mapeo completo en esta clase.
/// </summary>
public class HotelDbContext : DbContext
{
    public HotelDbContext(DbContextOptions<HotelDbContext> opciones)
        : base(opciones)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<HistorialCliente> HistorialClientes => Set<HistorialCliente>();

    public DbSet<TipoHabitacion> TiposHabitacion => Set<TipoHabitacion>();

    public DbSet<Habitacion> Habitaciones => Set<Habitacion>();

    public DbSet<Reserva> Reservas => Set<Reserva>();

    public DbSet<Estadia> Estadias => Set<Estadia>();

    public DbSet<ServicioAdicional> ServiciosAdicionales => Set<ServicioAdicional>();

    public DbSet<Consumo> Consumos => Set<Consumo>();

    public DbSet<Factura> Facturas => Set<Factura>();

    public DbSet<DetalleFactura> DetallesFactura => Set<DetalleFactura>();

    /// <summary>
    /// Collation de los campos de texto sobre los que se busca: insensible a mayúsculas
    /// y **a acentos**. Sin ella, buscar "Perez" no encontraría a "Pérez" ni "munoz" a
    /// "Muñoz", algo inaceptable con nombres en español.
    ///
    /// Se aplica columna por columna —y no solo a nivel de base de datos— para que el
    /// comportamiento no dependa de la collation con la que se haya creado `HotelDB`
    /// en cada entorno.
    /// </summary>
    public const string CollationBusqueda = "Latin1_General_CI_AI";

    protected override void OnModelCreating(ModelBuilder constructor)
    {
        base.OnModelCreating(constructor);
        constructor.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Sella las marcas de auditoría en un único punto, de modo que ningún servicio
    /// deba recordar asignarlas.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AplicarMarcasAuditoria();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        AplicarMarcasAuditoria();
        return base.SaveChanges();
    }

    private void AplicarMarcasAuditoria()
    {
        var ahora = DateTime.UtcNow;

        foreach (var entrada in ChangeTracker.Entries<EntidadBase>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    if (entrada.Entity.FechaCreacion == default)
                    {
                        entrada.Entity.FechaCreacion = ahora;
                    }

                    break;

                case EntityState.Modified:
                    entrada.Entity.FechaModificacion = ahora;
                    entrada.Property(e => e.FechaCreacion).IsModified = false;
                    break;
            }
        }
    }
}
