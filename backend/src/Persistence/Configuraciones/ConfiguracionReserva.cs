using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Persistence.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelParadiseResort.Persistence.Configuraciones;

public sealed class ConfiguracionReserva : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> constructor)
    {
        constructor.ToTable("Reserva");

        constructor.HasKey(r => r.Id);

        constructor.Property(r => r.Codigo)
            .IsRequired()
            .HasMaxLength(20)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(r => r.FechaEntrada)
            .IsRequired()
            .HasColumnType("date");

        constructor.Property(r => r.FechaSalida)
            .IsRequired()
            .HasColumnType("date");

        constructor.Property(r => r.CantidadHuespedes)
            .IsRequired();

        constructor.Property(r => r.Estado)
            .IsRequired()
            .HasConversion<int>();

        constructor.Property(r => r.CanalOrigen)
            .IsRequired()
            .HasConversion<int>();

        constructor.Property(r => r.MontoEstimado)
            .IsRequired()
            .HasPrecision(18, 2);

        constructor.Property(r => r.Observaciones)
            .HasMaxLength(500);

        constructor.Property(r => r.MotivoCancelacion)
            .HasMaxLength(300);

        constructor.Property(r => r.VersionFila)
            .IsRowVersion();

        constructor.HasOne(r => r.Cliente)
            .WithMany(c => c.Reservas)
            .HasForeignKey(r => r.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(r => r.Habitacion)
            .WithMany(h => h.Reservas)
            .HasForeignKey(r => r.HabitacionId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(r => r.UsuarioRegistro)
            .WithMany(u => u.ReservasGestionadas)
            .HasForeignKey(r => r.UsuarioRegistroId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasIndex(r => r.Codigo)
            .IsUnique()
            .HasDatabaseName("IX_Reserva_Codigo");

        // Índice principal de la detección de solapamientos: evita recorrer toda la
        // tabla al verificar disponibilidad antes de confirmar una reserva.
        constructor.HasIndex(r => new { r.HabitacionId, r.FechaEntrada, r.FechaSalida, r.Estado })
            .HasDatabaseName("IX_Reserva_Habitacion_Fechas_Estado");

        constructor.HasIndex(r => r.ClienteId)
            .HasDatabaseName("IX_Reserva_Cliente");

        constructor.HasIndex(r => new { r.Estado, r.FechaEntrada })
            .HasDatabaseName("IX_Reserva_Estado_FechaEntrada");

        // Invariante de negocio garantizada por el motor, no solo por la aplicación.
        constructor.ToTable(t => t.HasCheckConstraint(
            "CK_Reserva_Fechas", "[FechaSalida] >= [FechaEntrada]"));

        constructor.ToTable(t => t.HasCheckConstraint(
            "CK_Reserva_Huespedes", "[CantidadHuespedes] > 0"));

        constructor.ToTable(t => t.HasCheckConstraint(
            "CK_Reserva_Monto", "[MontoEstimado] >= 0"));
    }
}
