using HotelParadiseResort.Application.Abstracciones;
using HotelParadiseResort.Application.DTOs.Estadias;
using HotelParadiseResort.Application.Mapeo;
using HotelParadiseResort.Application.Servicios.Interfaces;
using HotelParadiseResort.Application.Tarifas;
using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Patrones.Decorator;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Shared.Excepciones;
using HotelParadiseResort.Shared.Paginacion;
using HotelParadiseResort.Shared.Resultados;
using Microsoft.Extensions.Logging;

namespace HotelParadiseResort.Application.Servicios;

/// <summary>
/// COMPONENTE 4 — Gestión de Estadías y Consumos (RF05, RF06, RF07).
///
/// Administra el check-in, el check-out y la acumulación de consumos. Depende de
/// Gestión de Habitaciones y de Gestión de Reservas. Aplica el patrón Decorator para
/// consolidar la cuenta de la estadía.
/// </summary>
public sealed class ServicioEstadias : IServicioEstadias
{
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly ISelectorEstrategiaTarifa _selectorTarifa;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IProveedorFechaHora _reloj;
    private readonly ILogger<ServicioEstadias> _registro;

    public ServicioEstadias(
        IUnidadDeTrabajo unidadDeTrabajo,
        ISelectorEstrategiaTarifa selectorTarifa,
        IUsuarioActual usuarioActual,
        IProveedorFechaHora reloj,
        ILogger<ServicioEstadias> registro)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _selectorTarifa = selectorTarifa;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
        _registro = registro;
    }

    public async Task<Resultado<ResultadoPaginado<EstadiaDto>>> ListarAsync(
        ParametrosPaginacion parametros, string? estado = null, CancellationToken cancelacion = default)
    {
        TipoEstadoEstadia? estadoFiltro = null;

        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.TryParse<TipoEstadoEstadia>(estado, ignoreCase: true, out var estadoParseado))
            {
                return Resultado.Fallo<ResultadoPaginado<EstadiaDto>>(
                    $"El estado '{estado}' no es válido.", TipoError.Validacion);
            }

            estadoFiltro = estadoParseado;
        }

        var pagina = await _unidadDeTrabajo.Estadias.BuscarAsync(parametros, estadoFiltro, cancelacion);

        var elementos = pagina.Elementos.Select(MapeadorEstadias.AEstadiaDto).ToList();

        return Resultado.Exitoso(new ResultadoPaginado<EstadiaDto>(
            elementos, pagina.TotalRegistros, pagina.Pagina, pagina.TamanoPagina));
    }

    public async Task<Resultado<EstadiaDto>> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default)
    {
        var estadia = await _unidadDeTrabajo.Estadias.ObtenerCompletaAsync(id, cancelacion);

        return estadia is null
            ? Resultado.Fallo<EstadiaDto>("La estadía indicada no existe.", TipoError.NoEncontrado)
            : Resultado.Exitoso(MapeadorEstadias.AEstadiaDto(estadia));
    }

    public async Task<Resultado<EstadiaDto>> BuscarActivaPorHabitacionAsync(
        string numeroHabitacion, CancellationToken cancelacion = default)
    {
        var habitacion = await _unidadDeTrabajo.Habitaciones
            .ObtenerPorNumeroAsync(numeroHabitacion.Trim(), cancelacion);

        if (habitacion is null)
        {
            return Resultado.Fallo<EstadiaDto>(
                $"No existe la habitación '{numeroHabitacion}'.", TipoError.NoEncontrado);
        }

        var estadia = await _unidadDeTrabajo.Estadias
            .ObtenerActivaPorHabitacionAsync(habitacion.Id, cancelacion);

        return estadia is null
            ? Resultado.Fallo<EstadiaDto>(
                $"La habitación {habitacion.Numero} no tiene una estadía en curso.", TipoError.NoEncontrado)
            : Resultado.Exitoso(MapeadorEstadias.AEstadiaDto(estadia));
    }

    /// <summary>
    /// RF05 — Check-in. Solo procede sobre una reserva confirmada; al registrarlo, la
    /// habitación pasa a "Ocupada", conforme al aviso del wireframe.
    /// </summary>
    public async Task<Resultado<EstadiaDto>> RegistrarCheckInAsync(
        RegistrarCheckInDto solicitud, CancellationToken cancelacion = default)
    {
        var resultado = await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var reserva = await _unidadDeTrabajo.Reservas.ObtenerCompletaAsync(solicitud.ReservaId, ct);

            if (reserva is null)
            {
                return Resultado.Fallo<int>("La reserva indicada no existe.", TipoError.NoEncontrado);
            }

            if (!reserva.ObtenerEstado().PermiteCheckIn)
            {
                return Resultado.Fallo<int>(
                    $"No es posible registrar el check-in de una reserva en estado " +
                    $"'{reserva.ObtenerEstado().Nombre}'. La reserva debe estar confirmada.",
                    TipoError.ReglaNegocio);
            }

            var existente = await _unidadDeTrabajo.Estadias.ObtenerPorReservaAsync(reserva.Id, ct);

            if (existente is not null)
            {
                return Resultado.Fallo<int>(
                    "La reserva ya tiene un check-in registrado.", TipoError.Conflicto);
            }

            var habitacion = await _unidadDeTrabajo.Habitaciones.ObtenerConTipoAsync(reserva.HabitacionId, ct);

            if (habitacion is null)
            {
                return Resultado.Fallo<int>(
                    "La habitación de la reserva no existe.", TipoError.NoEncontrado);
            }

            if (!habitacion.ObtenerEstado().PermiteCheckIn)
            {
                return Resultado.Fallo<int>(
                    $"La habitación {habitacion.Numero} se encuentra " +
                    $"'{habitacion.ObtenerEstado().Nombre}' y no admite un check-in.",
                    TipoError.ReglaNegocio);
            }

            if (solicitud.CantidadHuespedes > habitacion.TipoHabitacion!.CapacidadMaxima)
            {
                return Resultado.Fallo<int>(
                    $"La habitación {habitacion.Numero} admite un máximo de " +
                    $"{habitacion.TipoHabitacion.CapacidadMaxima} huésped(es).",
                    TipoError.ReglaNegocio);
            }

            var fechaCheckIn = solicitud.FechaCheckIn ?? _reloj.Ahora;

            // El check-in debe caer dentro del período reservado. Sin esta comprobación
            // podría registrarse la llegada de una reserva de meses después, lo que
            // desalinearía la estadía de lo contratado.
            if (fechaCheckIn.Date < reserva.FechaEntrada.Date)
            {
                return Resultado.Fallo<int>(
                    $"La reserva {reserva.Codigo} inicia el {reserva.FechaEntrada:dd/MM/yyyy}. " +
                    "No es posible registrar el check-in antes de esa fecha.",
                    TipoError.ReglaNegocio);
            }

            if (fechaCheckIn.Date > reserva.FechaSalida.Date)
            {
                return Resultado.Fallo<int>(
                    $"La reserva {reserva.Codigo} finalizó el {reserva.FechaSalida:dd/MM/yyyy}. " +
                    "No es posible registrar el check-in después de esa fecha.",
                    TipoError.ReglaNegocio);
            }

            var estadia = new Estadia
            {
                ReservaId = reserva.Id,
                HabitacionId = reserva.HabitacionId,
                FechaCheckIn = fechaCheckIn,
                CantidadHuespedes = solicitud.CantidadHuespedes,
                UsuarioCheckInId = _usuarioActual.Id ?? 0,
                Estado = TipoEstadoEstadia.EnCurso,
                Observaciones = solicitud.Observaciones?.Trim(),
                FechaCreacion = _reloj.Ahora
            };

            await _unidadDeTrabajo.Estadias.AgregarAsync(estadia, ct);

            habitacion.CambiarEstado(TipoEstadoHabitacion.Ocupada);
            _unidadDeTrabajo.Habitaciones.Actualizar(habitacion);

            await _unidadDeTrabajo.GuardarCambiosAsync(ct);

            return Resultado.Exitoso(estadia.Id);
        }, cancelacion);

        if (resultado.EsFallido)
        {
            return Resultado.Fallo<EstadiaDto>(resultado.Error!, resultado.TipoError);
        }

        var creada = await _unidadDeTrabajo.Estadias.ObtenerCompletaAsync(resultado.Valor, cancelacion);

        _registro.LogInformation(
            "Se registró el check-in {EstadiaId} de la reserva {ReservaId}.",
            resultado.Valor, solicitud.ReservaId);

        return Resultado.Exitoso(MapeadorEstadias.AEstadiaDto(creada!));
    }

    /// <summary>
    /// RF06 — Check-out. Cierra la estadía, libera la habitación hacia limpieza y deja
    /// la reserva completada, dejando la información lista para facturación.
    /// </summary>
    public async Task<Resultado<EstadiaDto>> RegistrarCheckOutAsync(
        int estadiaId, RegistrarCheckOutDto solicitud, CancellationToken cancelacion = default)
    {
        var resultado = await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var estadia = await _unidadDeTrabajo.Estadias.ObtenerCompletaAsync(estadiaId, ct);

            if (estadia is null)
            {
                return Resultado.Fallo<int>("La estadía indicada no existe.", TipoError.NoEncontrado);
            }

            if (!estadia.EstaAbierta())
            {
                return Resultado.Fallo<int>(
                    "La estadía ya fue cerrada.", TipoError.Conflicto);
            }

            var fechaCheckOut = solicitud.FechaCheckOut ?? _reloj.Ahora;

            if (fechaCheckOut < estadia.FechaCheckIn)
            {
                return Resultado.Fallo<int>(
                    "La fecha de check-out no puede ser anterior a la de check-in.", TipoError.Validacion);
            }

            estadia.FechaCheckOut = fechaCheckOut;
            estadia.Estado = TipoEstadoEstadia.Finalizada;
            estadia.UsuarioCheckOutId = _usuarioActual.Id;

            if (!string.IsNullOrWhiteSpace(solicitud.Observaciones))
            {
                estadia.Observaciones = string.IsNullOrWhiteSpace(estadia.Observaciones)
                    ? solicitud.Observaciones.Trim()
                    : $"{estadia.Observaciones} · {solicitud.Observaciones.Trim()}";
            }

            _unidadDeTrabajo.Estadias.Actualizar(estadia);

            var habitacion = await _unidadDeTrabajo.Habitaciones.ObtenerPorIdAsync(estadia.HabitacionId, ct);

            if (habitacion is not null && habitacion.PuedeCambiarA(TipoEstadoHabitacion.EnLimpieza))
            {
                habitacion.CambiarEstado(TipoEstadoHabitacion.EnLimpieza);
                _unidadDeTrabajo.Habitaciones.Actualizar(habitacion);
            }

            var reserva = await _unidadDeTrabajo.Reservas.ObtenerPorIdAsync(estadia.ReservaId, ct);

            if (reserva is not null && reserva.PuedeCambiarA(TipoEstadoReserva.Completada))
            {
                reserva.CambiarEstado(TipoEstadoReserva.Completada);
                _unidadDeTrabajo.Reservas.Actualizar(reserva);
            }

            await _unidadDeTrabajo.GuardarCambiosAsync(ct);

            return Resultado.Exitoso(estadiaId);
        }, cancelacion);

        if (resultado.EsFallido)
        {
            return Resultado.Fallo<EstadiaDto>(resultado.Error!, resultado.TipoError);
        }

        var cerrada = await _unidadDeTrabajo.Estadias.ObtenerCompletaAsync(estadiaId, cancelacion);

        _registro.LogInformation("Se registró el check-out de la estadía {EstadiaId}.", estadiaId);

        return Resultado.Exitoso(MapeadorEstadias.AEstadiaDto(cerrada!));
    }

    /// <summary>
    /// RF07 — Registro de un consumo. Los cargos se imputan a la estadía, que es el
    /// vínculo que evita las fugas por consumos no cobrados.
    /// </summary>
    public async Task<Resultado<ConsumoDto>> RegistrarConsumoAsync(
        int estadiaId, RegistrarConsumoDto solicitud, CancellationToken cancelacion = default)
    {
        var estadia = await _unidadDeTrabajo.Estadias.ObtenerPorIdAsync(estadiaId, cancelacion);

        if (estadia is null)
        {
            return Resultado.Fallo<ConsumoDto>("La estadía indicada no existe.", TipoError.NoEncontrado);
        }

        if (!estadia.EstaAbierta())
        {
            return Resultado.Fallo<ConsumoDto>(
                "No es posible registrar consumos en una estadía ya cerrada.", TipoError.ReglaNegocio);
        }

        var servicio = await _unidadDeTrabajo.ServiciosAdicionales
            .ObtenerPorIdAsync(solicitud.ServicioAdicionalId, cancelacion);

        if (servicio is null)
        {
            return Resultado.Fallo<ConsumoDto>(
                "El servicio adicional indicado no existe.", TipoError.NoEncontrado);
        }

        if (!servicio.Activo)
        {
            return Resultado.Fallo<ConsumoDto>(
                $"El servicio '{servicio.Nombre}' no está disponible.", TipoError.ReglaNegocio);
        }

        var consumo = new Consumo
        {
            EstadiaId = estadiaId,
            ServicioAdicionalId = servicio.Id,
            Descripcion = solicitud.Descripcion.Trim(),
            Cantidad = solicitud.Cantidad,
            // Si no se indica precio, se toma el del catálogo vigente en este momento.
            PrecioUnitario = solicitud.PrecioUnitario ?? servicio.PrecioBase,
            FechaConsumo = solicitud.FechaConsumo ?? _reloj.Ahora,
            UsuarioRegistroId = _usuarioActual.Id ?? 0,
            FechaCreacion = _reloj.Ahora
        };

        await _unidadDeTrabajo.Consumos.AgregarAsync(consumo, cancelacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        var registrado = await _unidadDeTrabajo.Consumos.ObtenerConServicioAsync(consumo.Id, cancelacion);

        _registro.LogInformation(
            "Se registró el consumo {ConsumoId} sobre la estadía {EstadiaId}.", consumo.Id, estadiaId);

        return Resultado.Exitoso(MapeadorEstadias.AConsumoDto(registrado!));
    }

    public async Task<Resultado> EliminarConsumoAsync(
        int estadiaId, int consumoId, CancellationToken cancelacion = default)
    {
        var consumo = await _unidadDeTrabajo.Consumos.ObtenerPorIdAsync(consumoId, cancelacion);

        if (consumo is null || consumo.EstadiaId != estadiaId)
        {
            return Resultado.Fallo("El consumo indicado no existe en esta estadía.", TipoError.NoEncontrado);
        }

        var estadia = await _unidadDeTrabajo.Estadias.ObtenerPorIdAsync(estadiaId, cancelacion);

        if (estadia is null || !estadia.EstaAbierta())
        {
            return Resultado.Fallo(
                "No es posible eliminar consumos de una estadía ya cerrada.", TipoError.ReglaNegocio);
        }

        _unidadDeTrabajo.Consumos.Eliminar(consumo);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "Se eliminó el consumo {ConsumoId} de la estadía {EstadiaId}.", consumoId, estadiaId);

        return Resultado.Exitoso();
    }

    /// <summary>
    /// Cuenta consolidada de la estadía.
    ///
    /// PATRÓN DECORATOR — La cuenta base de hospedaje se envuelve con un decorador por
    /// cada consumo; el total y el desglose salen de recorrer esa cadena.
    /// </summary>
    public async Task<Resultado<CuentaEstadiaDto>> ConsultarCuentaAsync(
        int estadiaId, CancellationToken cancelacion = default)
    {
        var estadia = await _unidadDeTrabajo.Estadias.ObtenerCompletaAsync(estadiaId, cancelacion);

        if (estadia is null)
        {
            return Resultado.Fallo<CuentaEstadiaDto>(
                "La estadía indicada no existe.", TipoError.NoEncontrado);
        }

        var cuenta = ConstruirCuenta(estadia, out var tarifa, out var descripcionHabitacion);

        var lineas = cuenta.ObtenerDetalle()
            .Select(l => new LineaCuentaDto(
                l.Concepto, l.Cantidad, l.PrecioUnitario, l.Subtotal, l.EsHospedaje, l.Fecha))
            .ToList();

        var subtotalConsumos = decimal.Round(
            lineas.Where(l => !l.EsHospedaje).Sum(l => l.Subtotal), 2, MidpointRounding.AwayFromZero);

        var dto = new CuentaEstadiaDto(
            estadia.Id,
            descripcionHabitacion,
            tarifa.Noches,
            tarifa.TarifaPorNoche,
            tarifa.MontoTotal,
            subtotalConsumos,
            cuenta.ObtenerMonto(),
            tarifa.EstrategiaAplicada,
            lineas);

        return Resultado.Exitoso(dto);
    }

    public async Task<Resultado<IReadOnlyList<ServicioAdicionalDto>>> ListarServiciosAsync(
        CancellationToken cancelacion = default)
    {
        var servicios = await _unidadDeTrabajo.ServiciosAdicionales.ObtenerTodosAsync(cancelacion);

        IReadOnlyList<ServicioAdicionalDto> resultado = servicios
            .OrderBy(s => s.Tipo)
            .ThenBy(s => s.Nombre)
            .Select(MapeadorEstadias.AServicioDto)
            .ToList();

        return Resultado.Exitoso(resultado);
    }

    public async Task<Resultado<ServicioAdicionalDto>> CrearServicioAsync(
        CrearServicioAdicionalDto solicitud, CancellationToken cancelacion = default)
    {
        if (!Enum.TryParse<TipoServicioAdicional>(solicitud.Tipo, ignoreCase: true, out var tipo))
        {
            return Resultado.Fallo<ServicioAdicionalDto>(
                $"El tipo de servicio '{solicitud.Tipo}' no es válido.", TipoError.Validacion);
        }

        var nombre = solicitud.Nombre.Trim();

        if (await _unidadDeTrabajo.ServiciosAdicionales.ExisteNombreAsync(nombre, null, cancelacion))
        {
            return Resultado.Fallo<ServicioAdicionalDto>(
                $"Ya existe un servicio llamado '{nombre}'.", TipoError.Conflicto);
        }

        var servicio = new ServicioAdicional
        {
            Nombre = nombre,
            Tipo = tipo,
            Descripcion = solicitud.Descripcion?.Trim(),
            PrecioBase = solicitud.PrecioBase,
            Activo = true
        };

        await _unidadDeTrabajo.ServiciosAdicionales.AgregarAsync(servicio, cancelacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation("Se creó el servicio adicional {ServicioId}.", servicio.Id);

        return Resultado.Exitoso(MapeadorEstadias.AServicioDto(servicio));
    }

    public async Task<Resultado<ServicioAdicionalDto>> ActualizarServicioAsync(
        int id, ActualizarServicioAdicionalDto solicitud, CancellationToken cancelacion = default)
    {
        var servicio = await _unidadDeTrabajo.ServiciosAdicionales.ObtenerPorIdAsync(id, cancelacion);

        if (servicio is null)
        {
            return Resultado.Fallo<ServicioAdicionalDto>(
                "El servicio indicado no existe.", TipoError.NoEncontrado);
        }

        if (!Enum.TryParse<TipoServicioAdicional>(solicitud.Tipo, ignoreCase: true, out var tipo))
        {
            return Resultado.Fallo<ServicioAdicionalDto>(
                $"El tipo de servicio '{solicitud.Tipo}' no es válido.", TipoError.Validacion);
        }

        var nombre = solicitud.Nombre.Trim();

        if (await _unidadDeTrabajo.ServiciosAdicionales.ExisteNombreAsync(nombre, id, cancelacion))
        {
            return Resultado.Fallo<ServicioAdicionalDto>(
                $"Ya existe otro servicio llamado '{nombre}'.", TipoError.Conflicto);
        }

        servicio.Nombre = nombre;
        servicio.Tipo = tipo;
        servicio.Descripcion = solicitud.Descripcion?.Trim();
        servicio.PrecioBase = solicitud.PrecioBase;
        servicio.Activo = solicitud.Activo;

        _unidadDeTrabajo.ServiciosAdicionales.Actualizar(servicio);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation("Se actualizó el servicio adicional {ServicioId}.", id);

        return Resultado.Exitoso(MapeadorEstadias.AServicioDto(servicio));
    }

    /// <summary>
    /// Arma la cadena de decoradores de la estadía. Se comparte entre la consulta de
    /// cuenta y la emisión de la factura para que ambas cifras no puedan discrepar.
    /// </summary>
    internal IComponenteCuenta ConstruirCuenta(
        Estadia estadia,
        out Domain.Patrones.Strategy.ResultadoTarifa tarifa,
        out string descripcionHabitacion)
    {
        var tipoHabitacion = estadia.Habitacion?.TipoHabitacion
            ?? throw new ExcepcionReglaNegocio(
                "La estadía no tiene cargada la información de la habitación.");

        // El hospedaje se cobra por el período contratado en la reserva, no por los
        // sellos de tiempo reales de entrada y salida. Sobre ese mismo rango se
        // resuelve la estrategia de tarifa, de modo que el importe coincida con el
        // que se le informó al huésped al reservar.
        var entrada = estadia.Reserva?.FechaEntrada ?? estadia.FechaCheckIn;
        var salida = estadia.Reserva?.FechaSalida ?? estadia.FechaCheckOut ?? estadia.FechaCheckIn;

        tarifa = _selectorTarifa.Calcular(tipoHabitacion.TarifaBasePorNoche, entrada, salida);

        descripcionHabitacion =
            $"Habitación {estadia.Habitacion.Numero} — {tipoHabitacion.Nombre}";

        return EnsambladorCuenta.Ensamblar(tarifa, descripcionHabitacion, estadia.Consumos);
    }
}
