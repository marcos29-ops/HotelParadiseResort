using HotelParadiseResort.Application.Abstracciones;
using HotelParadiseResort.Application.DTOs.Facturacion;
using HotelParadiseResort.Application.Mapeo;
using HotelParadiseResort.Application.Servicios.Interfaces;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.Builder;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Shared.Excepciones;
using HotelParadiseResort.Shared.Paginacion;
using HotelParadiseResort.Shared.Resultados;
using Microsoft.Extensions.Logging;

namespace HotelParadiseResort.Application.Servicios;

/// <summary>
/// COMPONENTE 5 — Facturación (RF08).
///
/// Consolida hospedaje y consumos en el comprobante final. Depende de Gestión de
/// Estadías y de Gestión de Habitaciones. Aplica el patrón Builder para ensamblar la
/// factura paso a paso.
/// </summary>
public sealed class ServicioFacturacion : IServicioFacturacion
{
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly ServicioEstadias _servicioEstadias;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IProveedorFechaHora _reloj;
    private readonly ILogger<ServicioFacturacion> _registro;

    public ServicioFacturacion(
        IUnidadDeTrabajo unidadDeTrabajo,
        ServicioEstadias servicioEstadias,
        IUsuarioActual usuarioActual,
        IProveedorFechaHora reloj,
        ILogger<ServicioFacturacion> registro)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _servicioEstadias = servicioEstadias;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
        _registro = registro;
    }

    public async Task<Resultado<ResultadoPaginado<FacturaDto>>> ListarAsync(
        ParametrosPaginacion parametros,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancelacion = default)
    {
        var pagina = await _unidadDeTrabajo.Facturas.BuscarAsync(parametros, desde, hasta, cancelacion);

        var elementos = pagina.Elementos.Select(MapeadorFacturas.AFacturaDto).ToList();

        return Resultado.Exitoso(new ResultadoPaginado<FacturaDto>(
            elementos, pagina.TotalRegistros, pagina.Pagina, pagina.TamanoPagina));
    }

    public async Task<Resultado<FacturaDto>> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default)
    {
        var factura = await _unidadDeTrabajo.Facturas.ObtenerCompletaAsync(id, cancelacion);

        return factura is null
            ? Resultado.Fallo<FacturaDto>("La factura indicada no existe.", TipoError.NoEncontrado)
            : Resultado.Exitoso(MapeadorFacturas.AFacturaDto(factura));
    }

    public async Task<Resultado<FacturaDto>> ObtenerPorEstadiaAsync(
        int estadiaId, CancellationToken cancelacion = default)
    {
        var factura = await _unidadDeTrabajo.Facturas.ObtenerPorEstadiaAsync(estadiaId, cancelacion);

        if (factura is null)
        {
            return Resultado.Fallo<FacturaDto>(
                "La estadía indicada no tiene factura emitida.", TipoError.NoEncontrado);
        }

        var completa = await _unidadDeTrabajo.Facturas.ObtenerCompletaAsync(factura.Id, cancelacion);

        return Resultado.Exitoso(MapeadorFacturas.AFacturaDto(completa!));
    }

    /// <summary>
    /// RF08 — Emisión de la factura consolidada.
    ///
    /// PATRÓN BUILDER — El comprobante se arma por pasos: hospedaje, consumos,
    /// descuento y pago. Los consumos provienen de la misma cadena de decoradores que
    /// alimenta la vista previa de la cuenta, de modo que ambos importes coincidan.
    /// </summary>
    public async Task<Resultado<FacturaDto>> GenerarAsync(
        GenerarFacturaDto solicitud, CancellationToken cancelacion = default)
    {
        var resultado = await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var estadia = await _unidadDeTrabajo.Estadias.ObtenerCompletaAsync(solicitud.EstadiaId, ct);

            if (estadia is null)
            {
                return Resultado.Fallo<int>("La estadía indicada no existe.", TipoError.NoEncontrado);
            }

            if (!estadia.PuedeFacturarse())
            {
                // Estadía aún abierta: falta cumplir un paso previo (422).
                // Estadía ya facturada: conflicto con el estado actual del recurso (409).
                return estadia.EstaAbierta()
                    ? Resultado.Fallo<int>(
                        "Debe registrarse el check-out antes de facturar la estadía.",
                        TipoError.ReglaNegocio)
                    : Resultado.Fallo<int>(
                        "La estadía ya fue facturada.", TipoError.Conflicto);
            }

            var facturaExistente = await _unidadDeTrabajo.Facturas
                .ObtenerPorEstadiaAsync(estadia.Id, ct);

            if (facturaExistente is not null)
            {
                return Resultado.Fallo<int>(
                    $"La estadía ya cuenta con la factura {facturaExistente.Numero}.", TipoError.Conflicto);
            }

            MetodoPago? metodoPago = null;

            if (!string.IsNullOrWhiteSpace(solicitud.MetodoPago))
            {
                if (!Enum.TryParse<MetodoPago>(solicitud.MetodoPago, ignoreCase: true, out var metodo))
                {
                    return Resultado.Fallo<int>(
                        $"El método de pago '{solicitud.MetodoPago}' no es válido.", TipoError.Validacion);
                }

                metodoPago = metodo;
            }

            if (solicitud.RegistrarPagoInmediato && metodoPago is null)
            {
                return Resultado.Fallo<int>(
                    "Debe indicar el método de pago para registrar el cobro.", TipoError.Validacion);
            }

            var cuenta = _servicioEstadias.ConstruirCuenta(estadia, out var tarifa, out var descripcion);

            var constructor = new FacturaBuilder()
                .ConNumero(await _unidadDeTrabajo.Facturas.GenerarNumeroAsync(ct))
                .ParaEstadia(estadia.Id, estadia.Reserva!.ClienteId)
                .EmitidaPor(_usuarioActual.Id ?? 0, _reloj.Ahora)
                .ConMontoHospedaje(tarifa, descripcion)
                .ConConsumos(cuenta);

            if (solicitud.Descuento is > 0)
            {
                if (string.IsNullOrWhiteSpace(solicitud.JustificacionDescuento))
                {
                    return Resultado.Fallo<int>(
                        "Todo descuento debe indicar su justificación.", TipoError.Validacion);
                }

                constructor.ConDescuento(solicitud.Descuento.Value, solicitud.JustificacionDescuento);
            }

            if (metodoPago is not null)
            {
                constructor.ConPago(
                    metodoPago.Value,
                    solicitud.RegistrarPagoInmediato ? EstadoPago.Pagado : EstadoPago.Pendiente);
            }

            Domain.Entidades.Factura factura;

            try
            {
                factura = constructor.Construir();
            }
            catch (ExcepcionReglaNegocio excepcion)
            {
                return Resultado.Fallo<int>(excepcion.Message, TipoError.ReglaNegocio);
            }

            await _unidadDeTrabajo.Facturas.AgregarAsync(factura, ct);

            estadia.Estado = TipoEstadoEstadia.Facturada;
            _unidadDeTrabajo.Estadias.Actualizar(estadia);

            await _unidadDeTrabajo.GuardarCambiosAsync(ct);

            return Resultado.Exitoso(factura.Id);
        }, cancelacion);

        if (resultado.EsFallido)
        {
            return Resultado.Fallo<FacturaDto>(resultado.Error!, resultado.TipoError);
        }

        var emitida = await _unidadDeTrabajo.Facturas.ObtenerCompletaAsync(resultado.Valor, cancelacion);

        _registro.LogInformation(
            "Se emitió la factura {FacturaId} de la estadía {EstadiaId} por {Total}.",
            resultado.Valor, solicitud.EstadiaId, emitida!.Total);

        return Resultado.Exitoso(MapeadorFacturas.AFacturaDto(emitida));
    }

    public async Task<Resultado<FacturaDto>> AplicarDescuentoAsync(
        int id, AplicarDescuentoDto solicitud, CancellationToken cancelacion = default)
    {
        var factura = await _unidadDeTrabajo.Facturas.ObtenerCompletaAsync(id, cancelacion);

        if (factura is null)
        {
            return Resultado.Fallo<FacturaDto>("La factura indicada no existe.", TipoError.NoEncontrado);
        }

        if (factura.EstadoPago != EstadoPago.Pendiente)
        {
            return Resultado.Fallo<FacturaDto>(
                "Solo es posible aplicar un descuento sobre una factura pendiente de pago.",
                TipoError.ReglaNegocio);
        }

        if (solicitud.Monto < 0)
        {
            return Resultado.Fallo<FacturaDto>(
                "El descuento no puede ser negativo.", TipoError.Validacion);
        }

        if (string.IsNullOrWhiteSpace(solicitud.Justificacion))
        {
            return Resultado.Fallo<FacturaDto>(
                "Todo descuento debe indicar su justificación.", TipoError.Validacion);
        }

        var maximo = factura.SubtotalHospedaje + factura.SubtotalConsumos;

        if (solicitud.Monto > maximo)
        {
            return Resultado.Fallo<FacturaDto>(
                $"El descuento no puede superar el monto facturado ({maximo:C2}).", TipoError.ReglaNegocio);
        }

        factura.Descuento = decimal.Round(solicitud.Monto, 2, MidpointRounding.AwayFromZero);
        factura.JustificacionDescuento = solicitud.Justificacion.Trim();
        factura.Total = factura.CalcularTotal();
        factura.FechaModificacion = _reloj.Ahora;

        _unidadDeTrabajo.Facturas.Actualizar(factura);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "Se aplicó un descuento de {Descuento} sobre la factura {FacturaId}.",
            factura.Descuento, id);

        return Resultado.Exitoso(MapeadorFacturas.AFacturaDto(factura));
    }

    public async Task<Resultado<FacturaDto>> RegistrarPagoAsync(
        int id, RegistrarPagoDto solicitud, CancellationToken cancelacion = default)
    {
        var factura = await _unidadDeTrabajo.Facturas.ObtenerCompletaAsync(id, cancelacion);

        if (factura is null)
        {
            return Resultado.Fallo<FacturaDto>("La factura indicada no existe.", TipoError.NoEncontrado);
        }

        if (factura.EstadoPago == EstadoPago.Pagado)
        {
            return Resultado.Fallo<FacturaDto>(
                "La factura ya se encuentra pagada.", TipoError.Conflicto);
        }

        if (factura.EstadoPago == EstadoPago.Anulado)
        {
            return Resultado.Fallo<FacturaDto>(
                "No es posible registrar el pago de una factura anulada.", TipoError.ReglaNegocio);
        }

        if (!Enum.TryParse<MetodoPago>(solicitud.MetodoPago, ignoreCase: true, out var metodoPago))
        {
            return Resultado.Fallo<FacturaDto>(
                $"El método de pago '{solicitud.MetodoPago}' no es válido.", TipoError.Validacion);
        }

        factura.RegistrarPago(metodoPago, _reloj.Ahora);

        _unidadDeTrabajo.Facturas.Actualizar(factura);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "Se registró el pago de la factura {FacturaId} por {Total} mediante {MetodoPago}.",
            id, factura.Total, metodoPago);

        return Resultado.Exitoso(MapeadorFacturas.AFacturaDto(factura));
    }
}
