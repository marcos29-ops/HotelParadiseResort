using HotelParadiseResort.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelParadiseResort.Persistence.Configuraciones;

public sealed class ConfiguracionEstadia : IEntityTypeConfiguration<Estadia>
{
    public void Configure(EntityTypeBuilder<Estadia> constructor)
    {
        constructor.ToTable("Estadia");

        constructor.HasKey(e => e.Id);

        constructor.Property(e => e.FechaCheckIn)
            .IsRequired();

        constructor.Property(e => e.CantidadHuespedes)
            .IsRequired();

        constructor.Property(e => e.Estado)
            .IsRequired()
            .HasConversion<int>();

        constructor.Property(e => e.Observaciones)
            .HasMaxLength(500);

        constructor.Property(e => e.VersionFila)
            .IsRowVersion();

        // Una reserva origina como máximo una estadía.
        constructor.HasOne(e => e.Reserva)
            .WithOne(r => r.Estadia)
            .HasForeignKey<Estadia>(e => e.ReservaId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(e => e.Habitacion)
            .WithMany(h => h.Estadias)
            .HasForeignKey(e => e.HabitacionId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(e => e.UsuarioCheckIn)
            .WithMany()
            .HasForeignKey(e => e.UsuarioCheckInId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(e => e.UsuarioCheckOut)
            .WithMany()
            .HasForeignKey(e => e.UsuarioCheckOutId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasIndex(e => e.ReservaId)
            .IsUnique()
            .HasDatabaseName("IX_Estadia_Reserva");

        // Localiza la estadía en curso de una habitación, operación del buscador de
        // check-in/check-out. Filtrado para indexar solo las estadías abiertas.
        constructor.HasIndex(e => new { e.HabitacionId, e.Estado })
            .HasDatabaseName("IX_Estadia_Habitacion_Estado");

        constructor.HasIndex(e => e.FechaCheckIn)
            .HasDatabaseName("IX_Estadia_FechaCheckIn");

        constructor.ToTable(t => t.HasCheckConstraint(
            "CK_Estadia_FechaCheckOut", "[FechaCheckOut] IS NULL OR [FechaCheckOut] >= [FechaCheckIn]"));

        constructor.ToTable(t => t.HasCheckConstraint(
            "CK_Estadia_Huespedes", "[CantidadHuespedes] > 0"));
    }
}
