using HotelParadiseResort.Application.Abstracciones;
using HotelParadiseResort.Application.DTOs.Reservas;
using HotelParadiseResort.Application.Mapeo;
using HotelParadiseResort.Application.Servicios.Interfaces;
using HotelParadiseResort.Application.Tarifas;
using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Shared.Excepciones;
using HotelParadiseResort.Shared.Paginacion;
using HotelParadiseResort.Shared.Resultados;
using Microsoft.Extensions.Logging;

namespace HotelParadiseResort.Application.Servicios;

/// <summary>
/// COMPONENTE 3 — Gestión de Reservas (RF04).
///
/// Centraliza la verificación de disponibilidad para impedir duplicidades, que es el
/// objetivo principal del sistema. Depende de Gestión de Habitaciones y de Gestión de
/// Clientes, conforme al diagrama de componentes. Aplica el patrón State.
/// </summary>
public sealed class ServicioReservas : IServicioReservas
{
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly ISelectorEstrategiaTarifa _selectorTarifa;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IProveedorFechaHora _reloj;
    private readonly ILogger<ServicioReservas> _registro;

    public ServicioReservas(
        IUnidadDeTrabajo unidadDeTrabajo,
        ISelectorEstrategiaTarifa selectorTarifa,
        IUsuarioActual usuarioActual,
        IProveedorFechaHora reloj,
        ILogger<ServicioReservas> registro)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _selectorTarifa = selectorTarifa;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
        _registro = registro;
    }

    public async Task<Resultado<ResultadoPaginado<ReservaDto>>> ListarAsync(
        ParametrosPaginacion parametros,
        string? estado = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancelacion = default)
    {
        TipoEstadoReserva? estadoFiltro = null;

        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.TryParse<TipoEstadoReserva>(estado, ignoreCase: true, out var estadoParseado))
            {
                return Resultado.Fallo<ResultadoPaginado<ReservaDto>>(
                    $"El estado '{estado}' no es válido.", TipoError.Validacion);
            }

            estadoFiltro = estadoParseado;
        }

        var pagina = await _unidadDeTrabajo.Reservas
            .BuscarAsync(parametros, estadoFiltro, desde, hasta, cancelacion);

        var elementos = pagina.Elementos.Select(MapeadorReservas.AReservaDto).ToList();

        return Resultado.Exitoso(new ResultadoPaginado<ReservaDto>(
            elementos, pagina.TotalRegistros, pagina.Pagina, pagina.TamanoPagina));
    }

    public async Task<Resultado<ReservaDto>> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default)
    {
        var reserva = await _unidadDeTrabajo.Reservas.ObtenerCompletaAsync(id, cancelacion);

        return reserva is null
            ? Resultado.Fallo<ReservaDto>("La reserva indicada no existe.", TipoError.NoEncontrado)
            : Resultado.Exitoso(MapeadorReservas.AReservaDto(reserva));
    }

    /// <summary>
    /// RF04 — Creación de la reserva.
    ///
    /// La verificación de disponibilidad y la escritura ocurren dentro de una misma
    /// transacción serializable: es lo que impide que dos recepcionistas confirmen a la
    /// vez la misma habitación para fechas solapadas (RNF03).
    /// </summary>
    public async Task<Resultado<ReservaDto>> CrearAsync(
        CrearReservaDto solicitud, CancellationToken cancelacion = default)
    {
        var validacion = ValidarFechas(solicitud.FechaEntrada, solicitud.FechaSalida);
        if (validacion.EsFallido)
        {
            return Resultado.Fallo<ReservaDto>(validacion.Error!, validacion.TipoError);
        }

        if (!Enum.TryParse<CanalOrigenReserva>(solicitud.CanalOrigen, ignoreCase: true, out var canal))
        {
            return Resultado.Fallo<ReservaDto>(
                $"El canal de origen '{solicitud.CanalOrigen}' no es válido.", TipoError.Validacion);
        }

        var resultado = await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var cliente = await ResolverClienteAsync(solicitud, ct);
            if (cliente.EsFallido)
            {
                return Resultado.Fallo<int>(cliente.Error!, cliente.TipoError);
            }

            var habitacion = await _unidadDeTrabajo.Habitaciones
                .ObtenerConTipoAsync(solicitud.HabitacionId, ct);

            if (habitacion is null)
            {
                return Resultado.Fallo<int>(
                    "La habitación indicada no existe.", TipoError.NoEncontrado);
            }

            if (habitacion.TipoHabitacion!.CapacidadMaxima < solicitud.CantidadHuespedes)
            {
                return Resultado.Fallo<int>(
                    $"La habitación {habitacion.Numero} admite un máximo de " +
                    $"{habitacion.TipoHabitacion.CapacidadMaxima} huésped(es).",
                    TipoError.ReglaNegocio);
            }

            var disponible = await _unidadDeTrabajo.Habitaciones.EstaDisponibleAsync(
                solicitud.HabitacionId, solicitud.FechaEntrada, solicitud.FechaSalida, null, ct);

            if (!disponible)
            {
                _registro.LogWarning(
                    "Se rechazó una reserva sobre la habitación {HabitacionId} por falta de disponibilidad " +
                    "entre {Entrada:d} y {Salida:d}.",
                    solicitud.HabitacionId, solicitud.FechaEntrada, solicitud.FechaSalida);

                return Resultado.Fallo<int>(
                    $"La habitación {habitacion.Numero} no está disponible en las fechas seleccionadas.",
                    TipoError.Conflicto);
            }

            var tarifa = _selectorTarifa.Calcular(
                habitacion.TipoHabitacion.TarifaBasePorNoche, solicitud.FechaEntrada, solicitud.FechaSalida);

            var reserva = new Reserva
            {
                Codigo = await _unidadDeTrabajo.Reservas.GenerarCodigoAsync(ct),
                ClienteId = cliente.Valor!.Id,
                HabitacionId = solicitud.HabitacionId,
                UsuarioRegistroId = _usuarioActual.Id ?? 0,
                FechaEntrada = solicitud.FechaEntrada.Date,
                FechaSalida = solicitud.FechaSalida.Date,
                CantidadHuespedes = solicitud.CantidadHuespedes,
                CanalOrigen = canal,
                // El diagrama de actividades establece que la reserva nace pendiente
                // y requiere confirmación explícita.
                Estado = TipoEstadoReserva.Pendiente,
                MontoEstimado = tarifa.MontoTotal,
                Observaciones = solicitud.Observaciones?.Trim(),
                FechaCreacion = _reloj.Ahora
            };

            await _unidadDeTrabajo.Reservas.AgregarAsync(reserva, ct);

            // La habitación queda comprometida desde el momento de la reserva.
            if (habitacion.PuedeCambiarA(TipoEstadoHabitacion.Reservada))
            {
                habitacion.CambiarEstado(TipoEstadoHabitacion.Reservada);
                _unidadDeTrabajo.Habitaciones.Actualizar(habitacion);
            }

            // Se confirma dentro de la transacción para que el identificador quede
            // asignado antes de salir del ámbito transaccional.
            await _unidadDeTrabajo.GuardarCambiosAsync(ct);

            return Resultado.Exitoso(reserva.Id);
        }, cancelacion);

        if (resultado.EsFallido)
        {
            return Resultado.Fallo<ReservaDto>(resultado.Error!, resultado.TipoError);
        }

        var creada = await _unidadDeTrabajo.Reservas.ObtenerCompletaAsync(resultado.Valor, cancelacion);

        _registro.LogInformation(
            "Se creó la reserva {ReservaId} sobre la habitación {HabitacionId}.",
            resultado.Valor, solicitud.HabitacionId);

        return Resultado.Exitoso(MapeadorReservas.AReservaDto(creada!));
    }

    public async Task<Resultado<ReservaDto>> ActualizarAsync(
        int id, ActualizarReservaDto solicitud, CancellationToken cancelacion = default)
    {
        var validacion = ValidarFechas(solicitud.FechaEntrada, solicitud.FechaSalida);
        if (validacion.EsFallido)
        {
            return Resultado.Fallo<ReservaDto>(validacion.Error!, validacion.TipoError);
        }

        if (!Enum.TryParse<CanalOrigenReserva>(solicitud.CanalOrigen, ignoreCase: true, out var canal))
        {
            return Resultado.Fallo<ReservaDto>(
                $"El canal de origen '{solicitud.CanalOrigen}' no es válido.", TipoError.Validacion);
        }

        var resultado = await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var reserva = await _unidadDeTrabajo.Reservas.ObtenerCompletaAsync(id, ct);

            if (reserva is null)
            {
                return Resultado.Fallo<int>("La reserva indicada no existe.", TipoError.NoEncontrado);
            }

            if (!reserva.ObtenerEstado().PermiteModificacion)
            {
                return Resultado.Fallo<int>(
                    $"Una reserva en estado '{reserva.ObtenerEstado().Nombre}' no admite modificaciones.",
                    TipoError.ReglaNegocio);
            }

            var habitacion = await _unidadDeTrabajo.Habitaciones
                .ObtenerConTipoAsync(solicitud.HabitacionId, ct);

            if (habitacion is null)
            {
                return Resultado.Fallo<int>("La habitación indicada no existe.", TipoError.NoEncontrado);
            }

            if (habitacion.TipoHabitacion!.CapacidadMaxima < solicitud.CantidadHuespedes)
            {
                return Resultado.Fallo<int>(
                    $"La habitación {habitacion.Numero} admite un máximo de " +
                    $"{habitacion.TipoHabitacion.CapacidadMaxima} huésped(es).",
                    TipoError.ReglaNegocio);
            }

            // Se excluye la propia reserva del control de solapamiento: de lo contrario
            // se detectaría a sí misma como conflicto.
            var disponible = await _unidadDeTrabajo.Habitaciones.EstaDisponibleAsync(
                solicitud.HabitacionId, solicitud.FechaEntrada, solicitud.FechaSalida, id, ct);

            if (!disponible)
            {
                return Resultado.Fallo<int>(
                    $"La habitación {habitacion.Numero} no está disponible en las fechas seleccionadas.",
                    TipoError.Conflicto);
            }

            var habitacionAnteriorId = reserva.HabitacionId;

            var tarifa = _selectorTarifa.Calcular(
                habitacion.TipoHabitacion.TarifaBasePorNoche, solicitud.FechaEntrada, solicitud.FechaSalida);

            reserva.HabitacionId = solicitud.HabitacionId;
            reserva.FechaEntrada = solicitud.FechaEntrada.Date;
            reserva.FechaSalida = solicitud.FechaSalida.Date;
            reserva.CantidadHuespedes = solicitud.CantidadHuespedes;
            reserva.CanalOrigen = canal;
            reserva.Observaciones = solicitud.Observaciones?.Trim();
            reserva.MontoEstimado = tarifa.MontoTotal;
            reserva.FechaModificacion = _reloj.Ahora;

            _unidadDeTrabajo.Reservas.Actualizar(reserva);

            if (habitacionAnteriorId != solicitud.HabitacionId)
            {
                await LiberarHabitacionSiCorrespondeAsync(habitacionAnteriorId, id, ct);

                if (habitacion.PuedeCambiarA(TipoEstadoHabitacion.Reservada))
                {
                    habitacion.CambiarEstado(TipoEstadoHabitacion.Reservada);
                    _unidadDeTrabajo.Habitaciones.Actualizar(habitacion);
                }
            }

            return Resultado.Exitoso(id);
        }, cancelacion);

        if (resultado.EsFallido)
        {
            return Resultado.Fallo<ReservaDto>(resultado.Error!, resultado.TipoError);
        }

        var actualizada = await _unidadDeTrabajo.Reservas.ObtenerCompletaAsync(id, cancelacion);

        _registro.LogInformation("Se actualizó la reserva {ReservaId}.", id);

        return Resultado.Exitoso(MapeadorReservas.AReservaDto(actualizada!));
    }

    public async Task<Resultado<ReservaDto>> ConfirmarAsync(int id, CancellationToken cancelacion = default)
    {
        var reserva = await _unidadDeTrabajo.Reservas.ObtenerCompletaAsync(id, cancelacion);

        if (reserva is null)
        {
            return Resultado.Fallo<ReservaDto>("La reserva indicada no existe.", TipoError.NoEncontrado);
        }

        try
        {
            reserva.CambiarEstado(TipoEstadoReserva.Confirmada);
        }
        catch (ExcepcionTransicionEstadoInvalida excepcion)
        {
            return Resultado.Fallo<ReservaDto>(excepcion.Message, TipoError.ReglaNegocio);
        }

        _unidadDeTrabajo.Reservas.Actualizar(reserva);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation("Se confirmó la reserva {ReservaId}.", id);

        return Resultado.Exitoso(MapeadorReservas.AReservaDto(reserva));
    }

    public async Task<Resultado<ReservaDto>> CancelarAsync(
        int id, CancelarReservaDto solicitud, CancellationToken cancelacion = default)
    {
        var resultado = await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var reserva = await _unidadDeTrabajo.Reservas.ObtenerCompletaAsync(id, ct);

            if (reserva is null)
            {
                return Resultado.Fallo<int>("La reserva indicada no existe.", TipoError.NoEncontrado);
            }

            if (reserva.Estadia is not null)
            {
                return Resultado.Fallo<int>(
                    "No es posible cancelar una reserva con check-in registrado.", TipoError.ReglaNegocio);
            }

            try
            {
                reserva.CambiarEstado(TipoEstadoReserva.Cancelada);
            }
            catch (ExcepcionTransicionEstadoInvalida excepcion)
            {
                return Resultado.Fallo<int>(excepcion.Message, TipoError.ReglaNegocio);
            }

            reserva.FechaCancelacion = _reloj.Ahora;
            reserva.MotivoCancelacion = solicitud.Motivo.Trim();
            _unidadDeTrabajo.Reservas.Actualizar(reserva);

            await LiberarHabitacionSiCorrespondeAsync(reserva.HabitacionId, id, ct);

            return Resultado.Exitoso(id);
        }, cancelacion);

        if (resultado.EsFallido)
        {
            return Resultado.Fallo<ReservaDto>(resultado.Error!, resultado.TipoError);
        }

        var cancelada = await _unidadDeTrabajo.Reservas.ObtenerCompletaAsync(id, cancelacion);

        _registro.LogInformation("Se canceló la reserva {ReservaId}.", id);

        return Resultado.Exitoso(MapeadorReservas.AReservaDto(cancelada!));
    }

    public async Task<Resultado<IReadOnlyList<ReservaDto>>> ConsultarLlegadasDelDiaAsync(
        DateTime? fecha = null, CancellationToken cancelacion = default)
    {
        var dia = fecha?.Date ?? _reloj.FechaOperativa;

        var reservas = await _unidadDeTrabajo.Reservas.ObtenerLlegadasDelDiaAsync(dia, cancelacion);

        IReadOnlyList<ReservaDto> resultado = reservas.Select(MapeadorReservas.AReservaDto).ToList();

        return Resultado.Exitoso(resultado);
    }

    /// <summary>
    /// Devuelve la habitación a disponible únicamente si ninguna otra reserva vigente
    /// la mantiene comprometida y no hay una estadía en curso.
    /// </summary>
    private async Task LiberarHabitacionSiCorrespondeAsync(
        int habitacionId, int reservaExcluidaId, CancellationToken cancelacion)
    {
        var habitacion = await _unidadDeTrabajo.Habitaciones.ObtenerPorIdAsync(habitacionId, cancelacion);

        if (habitacion is null || habitacion.Estado != TipoEstadoHabitacion.Reservada)
        {
            return;
        }

        var estadiaActiva = await _unidadDeTrabajo.Estadias
            .ObtenerActivaPorHabitacionAsync(habitacionId, cancelacion);

        if (estadiaActiva is not null)
        {
            return;
        }

        var hoy = _reloj.FechaOperativa;

        // Si al ignorar esta reserva la habitación sigue sin estar disponible, es que
        // otra reserva vigente la mantiene comprometida y no debe liberarse.
        var sigueComprometida = !await _unidadDeTrabajo.Habitaciones.EstaDisponibleAsync(
            habitacionId, hoy, hoy.AddYears(1), reservaExcluidaId, cancelacion);

        if (!sigueComprometida && habitacion.PuedeCambiarA(TipoEstadoHabitacion.Disponible))
        {
            habitacion.CambiarEstado(TipoEstadoHabitacion.Disponible);
            _unidadDeTrabajo.Habitaciones.Actualizar(habitacion);
        }
    }

    private async Task<Resultado<Cliente>> ResolverClienteAsync(
        CrearReservaDto solicitud, CancellationToken cancelacion)
    {
        if (solicitud.ClienteId is > 0)
        {
            var existente = await _unidadDeTrabajo.Clientes
                .ObtenerPorIdAsync(solicitud.ClienteId.Value, cancelacion);

            return existente is null
                ? Resultado.Fallo<Cliente>("El cliente indicado no existe.", TipoError.NoEncontrado)
                : Resultado.Exitoso(existente);
        }

        if (solicitud.ClienteNuevo is null)
        {
            return Resultado.Fallo<Cliente>(
                "Debe indicar un cliente existente o los datos de uno nuevo.", TipoError.Validacion);
        }

        var identificacion = solicitud.ClienteNuevo.Identificacion.Trim();

        // Registrar al huésped en el mismo paso es lo que hace la pantalla de nueva
        // reserva; si ya existe, se reutiliza en lugar de fallar.
        var yaRegistrado = await _unidadDeTrabajo.Clientes
            .ObtenerPorIdentificacionAsync(identificacion, cancelacion);

        if (yaRegistrado is not null)
        {
            return Resultado.Exitoso(yaRegistrado);
        }

        var cliente = new Cliente
        {
            Identificacion = identificacion,
            Nombre = solicitud.ClienteNuevo.Nombre.Trim(),
            Apellidos = solicitud.ClienteNuevo.Apellidos.Trim(),
            Correo = solicitud.ClienteNuevo.Correo?.Trim(),
            Telefono = solicitud.ClienteNuevo.Telefono?.Trim(),
            Nacionalidad = solicitud.ClienteNuevo.Nacionalidad?.Trim(),
            FechaNacimiento = solicitud.ClienteNuevo.FechaNacimiento,
            Activo = true,
            FechaCreacion = _reloj.Ahora
        };

        await _unidadDeTrabajo.Clientes.AgregarAsync(cliente, cancelacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Resultado.Exitoso(cliente);
    }

    private Resultado ValidarFechas(DateTime entrada, DateTime salida)
    {
        if (salida.Date < entrada.Date)
        {
            return Resultado.Fallo(
                "La fecha de salida no puede ser anterior a la fecha de entrada.", TipoError.Validacion);
        }

        if (entrada.Date < _reloj.FechaOperativa)
        {
            return Resultado.Fallo(
                "No es posible registrar una reserva con fecha de entrada anterior a hoy.",
                TipoError.Validacion);
        }

        return Resultado.Exitoso();
    }
}
