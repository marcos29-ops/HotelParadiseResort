using HotelParadiseResort.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelParadiseResort.Persistence.Configuraciones;

public sealed class ConfiguracionHistorialCliente : IEntityTypeConfiguration<HistorialCliente>
{
    public void Configure(EntityTypeBuilder<HistorialCliente> constructor)
    {
        constructor.ToTable("HistorialCliente");

        constructor.HasKey(h => h.Id);

        constructor.Property(h => h.Accion)
            .IsRequired()
            .HasMaxLength(50);

        constructor.Property(h => h.Detalle)
            .IsRequired()
            .HasMaxLength(1000);

        constructor.Property(h => h.FechaRegistro)
            .IsRequired();

        constructor.Property(h => h.VersionFila)
            .IsRowVersion();

        constructor.HasOne(h => h.Cliente)
            .WithMany()
            .HasForeignKey(h => h.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        constructor.HasOne(h => h.Usuario)
            .WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasIndex(h => new { h.ClienteId, h.FechaRegistro })
            .HasDatabaseName("IX_HistorialCliente_Cliente_Fecha");
    }
}
