using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Persistence.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelParadiseResort.Persistence.Configuraciones;

public sealed class ConfiguracionUsuario : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> constructor)
    {
        constructor.ToTable("Usuario");

        constructor.HasKey(u => u.Id);

        constructor.Property(u => u.Nombre)
            .IsRequired()
            .HasMaxLength(120)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(u => u.NombreUsuario)
            .IsRequired()
            .HasMaxLength(50)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(u => u.ContrasenaHash)
            .IsRequired()
            .HasMaxLength(500);

        constructor.Property(u => u.Correo)
            .IsRequired()
            .HasMaxLength(150)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(u => u.Rol)
            .IsRequired()
            .HasConversion<int>();

        constructor.Property(u => u.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        // Token de concurrencia optimista a nivel de tupla (RNF03).
        constructor.Property(u => u.VersionFila)
            .IsRowVersion();

        constructor.HasIndex(u => u.NombreUsuario)
            .IsUnique()
            .HasDatabaseName("IX_Usuario_NombreUsuario");

        constructor.HasIndex(u => u.Correo)
            .IsUnique()
            .HasDatabaseName("IX_Usuario_Correo");
    }
}
