using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Persistence.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelParadiseResort.Persistence.Configuraciones;

public sealed class ConfiguracionCliente : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> constructor)
    {
        constructor.ToTable("Cliente");

        constructor.HasKey(c => c.Id);

        constructor.Property(c => c.Identificacion)
            .IsRequired()
            .HasMaxLength(30)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(80)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(c => c.Apellidos)
            .IsRequired()
            .HasMaxLength(120)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(c => c.Correo)
            .HasMaxLength(150)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(c => c.Telefono)
            .HasMaxLength(30)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(c => c.Nacionalidad)
            .HasMaxLength(80);

        constructor.Property(c => c.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        constructor.Property(c => c.VersionFila)
            .IsRowVersion();

        constructor.HasIndex(c => c.Identificacion)
            .IsUnique()
            .HasDatabaseName("IX_Cliente_Identificacion");

        // Acelera el buscador por nombre de la pantalla de clientes.
        constructor.HasIndex(c => new { c.Apellidos, c.Nombre })
            .HasDatabaseName("IX_Cliente_Apellidos_Nombre");

        constructor.Ignore(c => c.NombreCompleto);
    }
}
