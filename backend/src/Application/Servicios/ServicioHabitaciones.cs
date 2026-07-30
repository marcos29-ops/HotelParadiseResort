using HotelParadiseResort.Application.DTOs.Habitaciones;
using HotelParadiseResort.Application.Mapeo;
using HotelParadiseResort.Application.Servicios.Interfaces;
using HotelParadiseResort.Application.Tarifas;
using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Shared.Excepciones;
using HotelParadiseResort.Shared.Resultados;
using Microsoft.Extensions.Logging;

namespace HotelParadiseResort.Application.Servicios;

/// <summary>
/// COMPONENTE 2 — Gestión de Habitaciones y Disponibilidad (RF02, RF03).
///
/// Administra el catálogo, el estado en tiempo real y la consulta de disponibilidad.
/// Aplica los patrones State (transiciones) y Strategy (tarifas). No depende de otros
/// componentes de negocio.
/// </summary>
public sealed class ServicioHabitaciones : IServicioHabitaciones
{
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly ISelectorEstrategiaTarifa _selectorTarifa;
    private readonly ILogger<ServicioHabitaciones> _registro;

    public ServicioHabitaciones(
        IUnidadDeTrabajo unidadDeTrabajo,
        ISelectorEstrategiaTarifa selectorTarifa,
        ILogger<ServicioHabitaciones> registro)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _selectorTarifa = selectorTarifa;
        _registro = registro;
    }

    public async Task<Resultado<IReadOnlyList<HabitacionDto>>> ListarAsync(
        CancellationToken cancelacion = default)
    {
        var habitaciones = await _unidadDeTrabajo.Habitaciones.ObtenerTodasConTipoAsync(cancelacion);

        IReadOnlyList<HabitacionDto> resultado = habitaciones
            .Select(MapeadorHabitaciones.AHabitacionDto)
            .ToList();

        return Resultado.Exitoso(resultado);
    }

    public async Task<Resultado<HabitacionDto>> ObtenerPorIdAsync(
        int id, CancellationToken cancelacion = default)
    {
        var habitacion = await _unidadDeTrabajo.Habitaciones.ObtenerConTipoAsync(id, cancelacion);

        return habitacion is null
            ? Resultado.Fallo<HabitacionDto>("La habitación indicada no existe.", TipoError.NoEncontrado)
            : Resultado.Exitoso(MapeadorHabitaciones.AHabitacionDto(habitacion));
    }

    public async Task<Resultado<HabitacionDto>> CrearAsync(
        CrearHabitacionDto solicitud, CancellationToken cancelacion = default)
    {
        var numero = solicitud.Numero.Trim();

        if (await _unidadDeTrabajo.Habitaciones.ExisteNumeroAsync(numero, null, cancelacion))
        {
            return Resultado.Fallo<HabitacionDto>(
                $"Ya existe una habitación con el número '{numero}'.", TipoError.Conflicto);
        }

        if (!await _unidadDeTrabajo.TiposHabitacion.ExisteAsync(solicitud.TipoHabitacionId, cancelacion))
        {
            return Resultado.Fallo<HabitacionDto>(
                "El tipo de habitación indicado no existe.", TipoError.Validacion);
        }

        var habitacion = new Habitacion
        {
            Numero = numero,
            Piso = solicitud.Piso,
            TipoHabitacionId = solicitud.TipoHabitacionId,
            Observaciones = solicitud.Observaciones?.Trim(),
            Estado = TipoEstadoHabitacion.Disponible,
            Activo = true
        };

        await _unidadDeTrabajo.Habitaciones.AgregarAsync(habitacion, cancelacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        var creada = await _unidadDeTrabajo.Habitaciones.ObtenerConTipoAsync(habitacion.Id, cancelacion);

        _registro.LogInformation("Se creó la habitación {HabitacionId} ({Numero}).", habitacion.Id, numero);

        return Resultado.Exitoso(MapeadorHabitaciones.AHabitacionDto(creada!));
    }

    public async Task<Resultado<HabitacionDto>> ActualizarAsync(
        int id, ActualizarHabitacionDto solicitud, CancellationToken cancelacion = default)
    {
        var habitacion = await _unidadDeTrabajo.Habitaciones.ObtenerPorIdAsync(id, cancelacion);

        if (habitacion is null)
        {
            return Resultado.Fallo<HabitacionDto>("La habitación indicada no existe.", TipoError.NoEncontrado);
        }

        var numero = solicitud.Numero.Trim();

        if (await _unidadDeTrabajo.Habitaciones.ExisteNumeroAsync(numero, id, cancelacion))
        {
            return Resultado.Fallo<HabitacionDto>(
                $"Ya existe otra habitación con el número '{numero}'.", TipoError.Conflicto);
        }

        if (!await _unidadDeTrabajo.TiposHabitacion.ExisteAsync(solicitud.TipoHabitacionId, cancelacion))
        {
            return Resultado.Fallo<HabitacionDto>(
                "El tipo de habitación indicado no existe.", TipoError.Validacion);
        }

        habitacion.Numero = numero;
        habitacion.Piso = solicitud.Piso;
        habitacion.TipoHabitacionId = solicitud.TipoHabitacionId;
        habitacion.Observaciones = solicitud.Observaciones?.Trim();
        habitacion.Activo = solicitud.Activo;

        _unidadDeTrabajo.Habitaciones.Actualizar(habitacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        var actualizada = await _unidadDeTrabajo.Habitaciones.ObtenerConTipoAsync(id, cancelacion);

        _registro.LogInformation("Se actualizó la habitación {HabitacionId}.", id);

        return Resultado.Exitoso(MapeadorHabitaciones.AHabitacionDto(actualizada!));
    }

    /// <summary>
    /// Cambia el estado delegando la validación en el patrón State: la habitación es
    /// quien conoce las transiciones que admite desde su estado actual.
    /// </summary>
    public async Task<Resultado<HabitacionDto>> CambiarEstadoAsync(
        int id, CambiarEstadoHabitacionDto solicitud, CancellationToken cancelacion = default)
    {
        var habitacion = await _unidadDeTrabajo.Habitaciones.ObtenerConTipoAsync(id, cancelacion);

        if (habitacion is null)
        {
            return Resultado.Fallo<HabitacionDto>("La habitación indicada no existe.", TipoError.NoEncontrado);
        }

        if (!Enum.TryParse<TipoEstadoHabitacion>(solicitud.NuevoEstado, ignoreCase: true, out var nuevoEstado))
        {
            return Resultado.Fallo<HabitacionDto>(
                $"El estado '{solicitud.NuevoEstado}' no es válido.", TipoError.Validacion);
        }

        if (habitacion.Estado == nuevoEstado)
        {
            return Resultado.Exitoso(MapeadorHabitaciones.AHabitacionDto(habitacion));
        }

        try
        {
            habitacion.CambiarEstado(nuevoEstado);
        }
        catch (ExcepcionTransicionEstadoInvalida excepcion)
        {
            return Resultado.Fallo<HabitacionDto>(excepcion.Message, TipoError.ReglaNegocio);
        }

        _unidadDeTrabajo.Habitaciones.Actualizar(habitacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "La habitación {HabitacionId} pasó al estado {Estado}.", id, nuevoEstado);

        return Resultado.Exitoso(MapeadorHabitaciones.AHabitacionDto(habitacion));
    }

    /// <summary>
    /// RF03 — Consulta de disponibilidad. Devuelve las habitaciones libres con la
    /// tarifa ya resuelta por la estrategia aplicable al período, de modo que la
    /// pantalla de reserva muestre el precio sin una segunda llamada.
    /// </summary>
    public async Task<Resultado<IReadOnlyList<HabitacionDisponibleDto>>> ConsultarDisponibilidadAsync(
        ConsultaDisponibilidadDto solicitud, CancellationToken cancelacion = default)
    {
        if (solicitud.FechaSalida.Date < solicitud.FechaEntrada.Date)
        {
            return Resultado.Fallo<IReadOnlyList<HabitacionDisponibleDto>>(
                "La fecha de salida no puede ser anterior a la fecha de entrada.", TipoError.Validacion);
        }

        var habitaciones = await _unidadDeTrabajo.Habitaciones.ObtenerDisponiblesAsync(
            solicitud.FechaEntrada, solicitud.FechaSalida, solicitud.TipoHabitacionId, null, cancelacion);

        if (solicitud.CantidadHuespedes is > 0)
        {
            habitaciones = habitaciones
                .Where(h => h.TipoHabitacion?.CapacidadMaxima >= solicitud.CantidadHuespedes)
                .ToList();
        }

        IReadOnlyList<HabitacionDisponibleDto> resultado = habitaciones
            .Select(h =>
            {
                var tarifa = _selectorTarifa.Calcular(
                    h.TipoHabitacion!.TarifaBasePorNoche, solicitud.FechaEntrada, solicitud.FechaSalida);

                return new HabitacionDisponibleDto(
                    h.Id,
                    h.Numero,
                    h.Piso,
                    h.TipoHabitacion.Nombre,
                    h.TipoHabitacion.CapacidadMaxima,
                    tarifa.Noches,
                    tarifa.TarifaPorNoche,
                    tarifa.MontoTotal,
                    tarifa.EstrategiaAplicada,
                    tarifa.Descripcion);
            })
            .ToList();

        return Resultado.Exitoso(resultado);
    }

    public async Task<Resultado<IReadOnlyList<TipoHabitacionDto>>> ListarTiposAsync(
        CancellationToken cancelacion = default)
    {
        var tipos = await _unidadDeTrabajo.TiposHabitacion.ObtenerTodosAsync(cancelacion);
        var habitaciones = await _unidadDeTrabajo.Habitaciones.ObtenerTodasConTipoAsync(cancelacion);

        var conteos = habitaciones
            .GroupBy(h => h.TipoHabitacionId)
            .ToDictionary(g => g.Key, g => g.Count());

        IReadOnlyList<TipoHabitacionDto> resultado = tipos
            .OrderBy(t => t.Nombre)
            .Select(t => MapeadorHabitaciones.ATipoHabitacionDto(t, conteos.GetValueOrDefault(t.Id)))
            .ToList();

        return Resultado.Exitoso(resultado);
    }

    public async Task<Resultado<TipoHabitacionDto>> CrearTipoAsync(
        CrearTipoHabitacionDto solicitud, CancellationToken cancelacion = default)
    {
        var nombre = solicitud.Nombre.Trim();

        if (await _unidadDeTrabajo.TiposHabitacion.ExisteNombreAsync(nombre, null, cancelacion))
        {
            return Resultado.Fallo<TipoHabitacionDto>(
                $"Ya existe un tipo de habitación llamado '{nombre}'.", TipoError.Conflicto);
        }

        var tipo = new TipoHabitacion
        {
            Nombre = nombre,
            Descripcion = solicitud.Descripcion?.Trim(),
            TarifaBasePorNoche = solicitud.TarifaBasePorNoche,
            CapacidadMaxima = solicitud.CapacidadMaxima,
            Activo = true
        };

        await _unidadDeTrabajo.TiposHabitacion.AgregarAsync(tipo, cancelacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation("Se creó el tipo de habitación {TipoId}.", tipo.Id);

        return Resultado.Exitoso(MapeadorHabitaciones.ATipoHabitacionDto(tipo, 0));
    }

    public async Task<Resultado<TipoHabitacionDto>> ActualizarTipoAsync(
        int id, ActualizarTipoHabitacionDto solicitud, CancellationToken cancelacion = default)
    {
        var tipo = await _unidadDeTrabajo.TiposHabitacion.ObtenerPorIdAsync(id, cancelacion);

        if (tipo is null)
        {
            return Resultado.Fallo<TipoHabitacionDto>(
                "El tipo de habitación indicado no existe.", TipoError.NoEncontrado);
        }

        var nombre = solicitud.Nombre.Trim();

        if (await _unidadDeTrabajo.TiposHabitacion.ExisteNombreAsync(nombre, id, cancelacion))
        {
            return Resultado.Fallo<TipoHabitacionDto>(
                $"Ya existe otro tipo de habitación llamado '{nombre}'.", TipoError.Conflicto);
        }

        if (!solicitud.Activo &&
            await _unidadDeTrabajo.TiposHabitacion.TieneHabitacionesAsociadasAsync(id, cancelacion))
        {
            return Resultado.Fallo<TipoHabitacionDto>(
                "No es posible desactivar un tipo con habitaciones asociadas.", TipoError.ReglaNegocio);
        }

        tipo.Nombre = nombre;
        tipo.Descripcion = solicitud.Descripcion?.Trim();
        tipo.TarifaBasePorNoche = solicitud.TarifaBasePorNoche;
        tipo.CapacidadMaxima = solicitud.CapacidadMaxima;
        tipo.Activo = solicitud.Activo;

        _unidadDeTrabajo.TiposHabitacion.Actualizar(tipo);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        var habitaciones = await _unidadDeTrabajo.Habitaciones.ObtenerTodasConTipoAsync(cancelacion);
        var cantidad = habitaciones.Count(h => h.TipoHabitacionId == id);

        _registro.LogInformation("Se actualizó el tipo de habitación {TipoId}.", id);

        return Resultado.Exitoso(MapeadorHabitaciones.ATipoHabitacionDto(tipo, cantidad));
    }
}
