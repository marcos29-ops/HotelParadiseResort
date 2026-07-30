using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Persistence.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelParadiseResort.Persistence.Configuraciones;

public sealed class ConfiguracionFactura : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> constructor)
    {
        constructor.ToTable("Factura");

        constructor.HasKey(f => f.Id);

        constructor.Property(f => f.Numero)
            .IsRequired()
            .HasMaxLength(20)
            .UseCollation(HotelDbContext.CollationBusqueda);

        constructor.Property(f => f.FechaEmision)
            .IsRequired();

        constructor.Property(f => f.Noches)
            .IsRequired();

        constructor.Property(f => f.TarifaPorNoche)
            .IsRequired()
            .HasPrecision(18, 2);

        constructor.Property(f => f.EstrategiaTarifa)
            .IsRequired()
            .HasMaxLength(40);

        constructor.Property(f => f.SubtotalHospedaje)
            .IsRequired()
            .HasPrecision(18, 2);

        constructor.Property(f => f.SubtotalConsumos)
            .IsRequired()
            .HasPrecision(18, 2);

        constructor.Property(f => f.Descuento)
            .IsRequired()
            .HasPrecision(18, 2);

        constructor.Property(f => f.JustificacionDescuento)
            .HasMaxLength(300);

        constructor.Property(f => f.Total)
            .IsRequired()
            .HasPrecision(18, 2);

        constructor.Property(f => f.MetodoPago)
            .HasConversion<int>();

        constructor.Property(f => f.EstadoPago)
            .IsRequired()
            .HasConversion<int>();

        constructor.Property(f => f.VersionFila)
            .IsRowVersion();

        // Una estadía se factura una sola vez.
        constructor.HasOne(f => f.Estadia)
            .WithOne(e => e.Factura)
            .HasForeignKey<Factura>(f => f.EstadiaId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(f => f.Cliente)
            .WithMany()
            .HasForeignKey(f => f.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(f => f.UsuarioEmision)
            .WithMany()
            .HasForeignKey(f => f.UsuarioEmisionId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasIndex(f => f.Numero)
            .IsUnique()
            .HasDatabaseName("IX_Factura_Numero");

        constructor.HasIndex(f => f.EstadiaId)
            .IsUnique()
            .HasDatabaseName("IX_Factura_Estadia");

        // Soporta el reporte de ingresos por rango de fechas.
        constructor.HasIndex(f => new { f.FechaEmision, f.EstadoPago })
            .HasDatabaseName("IX_Factura_FechaEmision_EstadoPago");

        constructor.ToTable(t => t.HasCheckConstraint(
            "CK_Factura_Montos",
            "[SubtotalHospedaje] >= 0 AND [SubtotalConsumos] >= 0 AND [Descuento] >= 0 AND [Total] >= 0"));

        constructor.ToTable(t => t.HasCheckConstraint(
            "CK_Factura_Descuento",
            "[Descuento] <= [SubtotalHospedaje] + [SubtotalConsumos]"));
    }
}

public sealed class ConfiguracionDetalleFactura : IEntityTypeConfiguration<DetalleFactura>
{
    public void Configure(EntityTypeBuilder<DetalleFactura> constructor)
    {
        constructor.ToTable("DetalleFactura");

        constructor.HasKey(d => d.Id);

        constructor.Property(d => d.Concepto)
            .IsRequired()
            .HasMaxLength(300);

        constructor.Property(d => d.Cantidad)
            .IsRequired();

        constructor.Property(d => d.PrecioUnitario)
            .IsRequired()
            .HasPrecision(18, 2);

        constructor.Property(d => d.Subtotal)
            .IsRequired()
            .HasPrecision(18, 2);

        constructor.Property(d => d.EsHospedaje)
            .IsRequired();

        constructor.Property(d => d.VersionFila)
            .IsRowVersion();

        constructor.HasOne(d => d.Factura)
            .WithMany(f => f.Detalles)
            .HasForeignKey(d => d.FacturaId)
            .OnDelete(DeleteBehavior.Cascade);

        constructor.HasIndex(d => d.FacturaId)
            .HasDatabaseName("IX_DetalleFactura_Factura");
    }
}
