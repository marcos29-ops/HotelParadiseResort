using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Persistence.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelParadiseResort.Persistence.Configuraciones;

public sealed class ConfiguracionServicioAdicional : IEntityTypeConfiguration<ServicioAdicional>
{
    public void Configure(EntityTypeBuilder<ServicioAdicional> constructor)
    {
        constructor.ToTable("ServicioAdicional");

        constructor.HasKey(s => s.Id);

        constructor.Property(s => s.Nombre)
            .IsRequired()
            .HasMaxLength(120)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(s => s.Tipo)
            .IsRequired()
            .HasConversion<int>();

        constructor.Property(s => s.Descripcion)
            .HasMaxLength(400)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(s => s.PrecioBase)
            .IsRequired()
            .HasPrecision(18, 2);

        constructor.Property(s => s.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        constructor.Property(s => s.VersionFila)
            .IsRowVersion();

        constructor.HasIndex(s => s.Nombre)
            .IsUnique()
            .HasDatabaseName("IX_ServicioAdicional_Nombre");

        constructor.HasIndex(s => s.Tipo)
            .HasDatabaseName("IX_ServicioAdicional_Tipo");

        constructor.ToTable(t => t.HasCheckConstraint(
            "CK_ServicioAdicional_Precio", "[PrecioBase] >= 0"));
    }
}

public sealed class ConfiguracionConsumo : IEntityTypeConfiguration<Consumo>
{
    public void Configure(EntityTypeBuilder<Consumo> constructor)
    {
        constructor.ToTable("Consumo");

        constructor.HasKey(c => c.Id);

        constructor.Property(c => c.Descripcion)
            .IsRequired()
            .HasMaxLength(300)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(c => c.Cantidad)
            .IsRequired();

        constructor.Property(c => c.PrecioUnitario)
            .IsRequired()
            .HasPrecision(18, 2);

        constructor.Property(c => c.FechaConsumo)
            .IsRequired();

        constructor.Property(c => c.VersionFila)
            .IsRowVersion();

        constructor.HasOne(c => c.Estadia)
            .WithMany(e => e.Consumos)
            .HasForeignKey(c => c.EstadiaId)
            .OnDelete(DeleteBehavior.Cascade);

        constructor.HasOne(c => c.ServicioAdicional)
            .WithMany(s => s.Consumos)
            .HasForeignKey(c => c.ServicioAdicionalId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(c => c.UsuarioRegistro)
            .WithMany()
            .HasForeignKey(c => c.UsuarioRegistroId)
            .OnDelete(DeleteBehavior.Restrict);

        // La consolidación de la cuenta recupera todos los consumos de una estadía.
        constructor.HasIndex(c => c.EstadiaId)
            .HasDatabaseName("IX_Consumo_Estadia");

        constructor.HasIndex(c => c.FechaConsumo)
            .HasDatabaseName("IX_Consumo_Fecha");

        constructor.ToTable(t => t.HasCheckConstraint(
            "CK_Consumo_Cantidad", "[Cantidad] > 0"));

        constructor.ToTable(t => t.HasCheckConstraint(
            "CK_Consumo_PrecioUnitario", "[PrecioUnitario] >= 0"));
    }
}
