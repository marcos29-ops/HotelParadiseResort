using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Domain.Patrones.TemplateMethod;

/// <summary>
/// PATRÓN TEMPLATE METHOD — Reporte de ingresos.
///
/// Separa lo facturado por hospedaje de lo facturado por servicios adicionales, tal
/// como lo presenta el wireframe: dos series comparadas por semana y tres KPI.
/// Cada registro es una factura emitida (Valor = hospedaje, Secundario = consumos).
/// </summary>
public sealed class ReporteIngresos : ReporteBase
{
    private readonly IReadOnlyList<RegistroReporte> _origen;

    public ReporteIngresos(IReadOnlyList<RegistroReporte> origen) => _origen = origen;

    public override TipoReporte Tipo => TipoReporte.Ingresos;

    public override string Titulo => "Reporte de ingresos";

    protected override IReadOnlyList<RegistroReporte> ObtenerDatos(ParametrosReporte parametros) => _origen;

    protected override IReadOnlyList<IndicadorReporte> Calcular(
        IReadOnlyList<RegistroReporte> datos, ParametrosReporte parametros)
    {
        var ingresosHospedaje = decimal.Round(datos.Sum(d => d.Valor), 2, MidpointRounding.AwayFromZero);
        var ingresosServicios = decimal.Round(datos.Sum(d => d.ValorSecundario), 2, MidpointRounding.AwayFromZero);
        var total = ingresosHospedaje + ingresosServicios;

        var promedioPorFactura = datos.Count > 0
            ? decimal.Round(total / datos.Count, 2, MidpointRounding.AwayFromZero)
            : 0m;

        return
        [
            new IndicadorReporte("Ingresos por hospedaje", ingresosHospedaje, "moneda",
                "Facturado por concepto de alojamiento"),
            new IndicadorReporte("Ingresos por servicios", ingresosServicios, "moneda",
                "Restaurante, lavandería, transporte y actividades"),
            new IndicadorReporte("Ingresos totales del período", total, "moneda",
                "Suma de hospedaje y servicios adicionales"),
            new IndicadorReporte("Promedio por factura", promedioPorFactura, "moneda",
                $"Sobre {datos.Count} factura(s) emitida(s)")
        ];
    }
}
