using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Persistence.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelParadiseResort.Persistence.Configuraciones;

public sealed class ConfiguracionTipoHabitacion : IEntityTypeConfiguration<TipoHabitacion>
{
    public void Configure(EntityTypeBuilder<TipoHabitacion> constructor)
    {
        constructor.ToTable("TipoHabitacion");

        constructor.HasKey(t => t.Id);

        constructor.Property(t => t.Nombre)
            .IsRequired()
            .HasMaxLength(100)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(t => t.Descripcion)
            .HasMaxLength(400);

        constructor.Property(t => t.TarifaBasePorNoche)
            .IsRequired()
            .HasPrecision(18, 2);

        constructor.Property(t => t.CapacidadMaxima)
            .IsRequired();

        constructor.Property(t => t.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        constructor.Property(t => t.VersionFila)
            .IsRowVersion();

        constructor.HasIndex(t => t.Nombre)
            .IsUnique()
            .HasDatabaseName("IX_TipoHabitacion_Nombre");

        constructor.ToTable(t => t.HasCheckConstraint(
            "CK_TipoHabitacion_Tarifa", "[TarifaBasePorNoche] >= 0"));

        constructor.ToTable(t => t.HasCheckConstraint(
            "CK_TipoHabitacion_Capacidad", "[CapacidadMaxima] > 0"));
    }
}

public sealed class ConfiguracionHabitacion : IEntityTypeConfiguration<Habitacion>
{
    public void Configure(EntityTypeBuilder<Habitacion> constructor)
    {
        constructor.ToTable("Habitacion");

        constructor.HasKey(h => h.Id);

        constructor.Property(h => h.Numero)
            .IsRequired()
            .HasMaxLength(10)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(h => h.Piso)
            .IsRequired();

        constructor.Property(h => h.Estado)
            .IsRequired()
            .HasConversion<int>();

        constructor.Property(h => h.Observaciones)
            .HasMaxLength(500);

        constructor.Property(h => h.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        constructor.Property(h => h.VersionFila)
            .IsRowVersion();

        constructor.HasOne(h => h.TipoHabitacion)
            .WithMany(t => t.Habitaciones)
            .HasForeignKey(h => h.TipoHabitacionId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasIndex(h => h.Numero)
            .IsUnique()
            .HasDatabaseName("IX_Habitacion_Numero");

        // Índice de apoyo a la consulta de disponibilidad, la más frecuente del sistema (RNF02).
        constructor.HasIndex(h => new { h.Estado, h.TipoHabitacionId })
            .HasDatabaseName("IX_Habitacion_Estado_Tipo");
    }
}
