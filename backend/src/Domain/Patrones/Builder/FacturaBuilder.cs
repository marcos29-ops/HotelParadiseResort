using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.Decorator;
using HotelParadiseResort.Domain.Patrones.Strategy;
using HotelParadiseResort.Shared.Excepciones;

namespace HotelParadiseResort.Domain.Patrones.Builder;

/// <summary>
/// PATRÓN BUILDER — Ensamblaje paso a paso de la factura consolidada.
///
/// La factura reúne hospedaje, consumos, descuento y método de pago. Sin este builder
/// ese armado quedaría repartido entre el servicio de facturación y el de estadías.
/// El orden de las llamadas es indiferente; <see cref="Construir"/> valida que estén
/// los datos imprescindibles antes de materializar el comprobante.
/// </summary>
public sealed class FacturaBuilder
{
    private readonly List<DetalleFactura> _detalles = [];

    private string? _numero;
    private int _estadiaId;
    private int _clienteId;
    private int _usuarioEmisionId;
    private DateTime _fechaEmision = DateTime.UtcNow;

    private ResultadoTarifa? _tarifa;
    private decimal _subtotalHospedaje;
    private decimal _subtotalConsumos;
    private decimal _descuento;
    private string? _justificacionDescuento;
    private MetodoPago? _metodoPago;
    private EstadoPago _estadoPago = EstadoPago.Pendiente;

    public FacturaBuilder ConNumero(string numero)
    {
        _numero = numero;
        return this;
    }

    public FacturaBuilder ParaEstadia(int estadiaId, int clienteId)
    {
        _estadiaId = estadiaId;
        _clienteId = clienteId;
        return this;
    }

    public FacturaBuilder EmitidaPor(int usuarioId, DateTime fechaEmision)
    {
        _usuarioEmisionId = usuarioId;
        _fechaEmision = fechaEmision;
        return this;
    }

    /// <summary>Fija el hospedaje a partir del cálculo de la estrategia de tarifa vigente.</summary>
    public FacturaBuilder ConMontoHospedaje(ResultadoTarifa tarifa, string descripcionHabitacion)
    {
        _tarifa = tarifa;
        _subtotalHospedaje = tarifa.MontoTotal;

        _detalles.Add(new DetalleFactura
        {
            Concepto = descripcionHabitacion,
            Cantidad = tarifa.Noches,
            PrecioUnitario = tarifa.TarifaPorNoche,
            Subtotal = tarifa.MontoTotal,
            EsHospedaje = true
        });

        return this;
    }

    /// <summary>
    /// Incorpora los consumos tomando el desglose que produce la cadena de decoradores,
    /// de modo que la factura y la cuenta de la estadía no puedan discrepar.
    /// </summary>
    public FacturaBuilder ConConsumos(IComponenteCuenta cuenta)
    {
        var lineasConsumo = cuenta.ObtenerDetalle().Where(l => !l.EsHospedaje).ToList();

        foreach (var linea in lineasConsumo)
        {
            _detalles.Add(new DetalleFactura
            {
                Concepto = linea.Concepto,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario,
                Subtotal = linea.Subtotal,
                EsHospedaje = false,
                FechaConsumo = linea.Fecha
            });
        }

        _subtotalConsumos = decimal.Round(
            lineasConsumo.Sum(l => l.Subtotal), 2, MidpointRounding.AwayFromZero);

        return this;
    }

    /// <summary>Aplica un descuento. La justificación es obligatoria para la auditoría.</summary>
    public FacturaBuilder ConDescuento(decimal monto, string justificacion)
    {
        if (monto < 0)
        {
            throw new ExcepcionReglaNegocio("El descuento no puede ser negativo.");
        }

        if (monto > 0 && string.IsNullOrWhiteSpace(justificacion))
        {
            throw new ExcepcionReglaNegocio("Todo descuento debe indicar su justificación.");
        }

        _descuento = decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
        _justificacionDescuento = justificacion;
        return this;
    }

    public FacturaBuilder ConPago(MetodoPago metodoPago, EstadoPago estadoPago)
    {
        _metodoPago = metodoPago;
        _estadoPago = estadoPago;
        return this;
    }

    /// <summary>
    /// Valida las precondiciones y materializa la factura con sus totales calculados.
    /// </summary>
    public Factura Construir()
    {
        if (string.IsNullOrWhiteSpace(_numero))
        {
            throw new ExcepcionReglaNegocio("La factura requiere un número de comprobante.");
        }

        if (_tarifa is null)
        {
            throw new ExcepcionReglaNegocio("La factura requiere el detalle de hospedaje.");
        }

        if (_estadiaId <= 0 || _clienteId <= 0)
        {
            throw new ExcepcionReglaNegocio("La factura debe asociarse a una estadía y a un cliente.");
        }

        var descuentoAplicable = _subtotalHospedaje + _subtotalConsumos;
        if (_descuento > descuentoAplicable)
        {
            throw new ExcepcionReglaNegocio(
                "El descuento no puede superar el monto facturado.");
        }

        var factura = new Factura
        {
            Numero = _numero,
            EstadiaId = _estadiaId,
            ClienteId = _clienteId,
            UsuarioEmisionId = _usuarioEmisionId,
            FechaEmision = _fechaEmision,
            Noches = _tarifa.Noches,
            TarifaPorNoche = _tarifa.TarifaPorNoche,
            EstrategiaTarifa = _tarifa.EstrategiaAplicada,
            SubtotalHospedaje = _subtotalHospedaje,
            SubtotalConsumos = _subtotalConsumos,
            Descuento = _descuento,
            JustificacionDescuento = _justificacionDescuento,
            MetodoPago = _metodoPago,
            EstadoPago = _estadoPago,
            FechaPago = _estadoPago == EstadoPago.Pagado ? _fechaEmision : null,
            FechaCreacion = _fechaEmision,
            Detalles = _detalles
        };

        factura.Total = factura.CalcularTotal();
        return factura;
    }
}
