using HotelParadiseResort.Application.Abstracciones;
using HotelParadiseResort.Application.DTOs.Clientes;
using HotelParadiseResort.Application.DTOs.Reservas;
using HotelParadiseResort.Application.Mapeo;
using HotelParadiseResort.Application.Servicios.Interfaces;
using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Shared.Paginacion;
using HotelParadiseResort.Shared.Resultados;
using Microsoft.Extensions.Logging;

namespace HotelParadiseResort.Application.Servicios;

/// <summary>
/// COMPONENTE 1 — Gestión de Clientes (RF01).
///
/// Administra el ciclo de vida de la ficha del huésped y mantiene su historial de
/// cambios. No depende de ningún otro componente de negocio.
/// </summary>
public sealed class ServicioClientes : IServicioClientes
{
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IProveedorFechaHora _reloj;
    private readonly ILogger<ServicioClientes> _registro;

    public ServicioClientes(
        IUnidadDeTrabajo unidadDeTrabajo,
        IUsuarioActual usuarioActual,
        IProveedorFechaHora reloj,
        ILogger<ServicioClientes> registro)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
        _registro = registro;
    }

    public async Task<Resultado<ResultadoPaginado<ClienteDto>>> ListarAsync(
        ParametrosPaginacion parametros, CancellationToken cancelacion = default)
    {
        var pagina = await _unidadDeTrabajo.Clientes.BuscarAsync(parametros, cancelacion);

        // El conteo de reservas se resuelve en una sola consulta para toda la página,
        // evitando el problema N+1 en la columna "Reservas" del listado.
        var conteos = await _unidadDeTrabajo.Clientes.ContarReservasPorClienteAsync(
            pagina.Elementos.Select(c => c.Id), cancelacion);

        var elementos = pagina.Elementos
            .Select(c => MapeadorClientes.AClienteDto(c, conteos.GetValueOrDefault(c.Id)))
            .ToList();

        return Resultado.Exitoso(new ResultadoPaginado<ClienteDto>(
            elementos, pagina.TotalRegistros, pagina.Pagina, pagina.TamanoPagina));
    }

    public async Task<Resultado<ClienteDto>> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default)
    {
        var cliente = await _unidadDeTrabajo.Clientes.ObtenerPorIdAsync(id, cancelacion);

        if (cliente is null)
        {
            return Resultado.Fallo<ClienteDto>("El cliente indicado no existe.", TipoError.NoEncontrado);
        }

        var conteos = await _unidadDeTrabajo.Clientes.ContarReservasPorClienteAsync([id], cancelacion);

        return Resultado.Exitoso(MapeadorClientes.AClienteDto(cliente, conteos.GetValueOrDefault(id)));
    }

    public async Task<Resultado<ClienteDto>> BuscarPorIdentificacionAsync(
        string identificacion, CancellationToken cancelacion = default)
    {
        var cliente = await _unidadDeTrabajo.Clientes
            .ObtenerPorIdentificacionAsync(identificacion.Trim(), cancelacion);

        if (cliente is null)
        {
            return Resultado.Fallo<ClienteDto>(
                $"No existe un cliente con la identificación '{identificacion}'.", TipoError.NoEncontrado);
        }

        var conteos = await _unidadDeTrabajo.Clientes
            .ContarReservasPorClienteAsync([cliente.Id], cancelacion);

        return Resultado.Exitoso(
            MapeadorClientes.AClienteDto(cliente, conteos.GetValueOrDefault(cliente.Id)));
    }

    public async Task<Resultado<ClienteDto>> RegistrarAsync(
        CrearClienteDto solicitud, CancellationToken cancelacion = default)
    {
        var identificacion = solicitud.Identificacion.Trim();

        if (await _unidadDeTrabajo.Clientes.ExisteIdentificacionAsync(identificacion, null, cancelacion))
        {
            return Resultado.Fallo<ClienteDto>(
                $"Ya existe un cliente registrado con la identificación '{identificacion}'.",
                TipoError.Conflicto);
        }

        var cliente = new Cliente
        {
            Identificacion = identificacion,
            Nombre = solicitud.Nombre.Trim(),
            Apellidos = solicitud.Apellidos.Trim(),
            Correo = solicitud.Correo?.Trim(),
            Telefono = solicitud.Telefono?.Trim(),
            Nacionalidad = solicitud.Nacionalidad?.Trim(),
            FechaNacimiento = solicitud.FechaNacimiento,
            Activo = true
        };

        await _unidadDeTrabajo.Clientes.AgregarAsync(cliente, cancelacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        await RegistrarHistorialAsync(
            cliente.Id, "Registro", $"Cliente registrado con identificación {identificacion}.", cancelacion);

        _registro.LogInformation("Se registró el cliente {ClienteId}.", cliente.Id);

        return Resultado.Exitoso(MapeadorClientes.AClienteDto(cliente, 0));
    }

    public async Task<Resultado<ClienteDto>> ActualizarAsync(
        int id, ActualizarClienteDto solicitud, CancellationToken cancelacion = default)
    {
        var cliente = await _unidadDeTrabajo.Clientes.ObtenerPorIdAsync(id, cancelacion);

        if (cliente is null)
        {
            return Resultado.Fallo<ClienteDto>("El cliente indicado no existe.", TipoError.NoEncontrado);
        }

        var cambios = DescribirCambios(cliente, solicitud);

        cliente.Nombre = solicitud.Nombre.Trim();
        cliente.Apellidos = solicitud.Apellidos.Trim();
        cliente.Correo = solicitud.Correo?.Trim();
        cliente.Telefono = solicitud.Telefono?.Trim();
        cliente.Nacionalidad = solicitud.Nacionalidad?.Trim();
        cliente.FechaNacimiento = solicitud.FechaNacimiento;
        cliente.Activo = solicitud.Activo;

        _unidadDeTrabajo.Clientes.Actualizar(cliente);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        if (cambios.Count > 0)
        {
            await RegistrarHistorialAsync(
                cliente.Id, "Actualización", string.Join(" · ", cambios), cancelacion);
        }

        var conteos = await _unidadDeTrabajo.Clientes.ContarReservasPorClienteAsync([id], cancelacion);

        _registro.LogInformation("Se actualizó el cliente {ClienteId}.", id);

        return Resultado.Exitoso(MapeadorClientes.AClienteDto(cliente, conteos.GetValueOrDefault(id)));
    }

    public async Task<Resultado<IReadOnlyList<HistorialClienteDto>>> ConsultarHistorialAsync(
        int clienteId, CancellationToken cancelacion = default)
    {
        if (!await _unidadDeTrabajo.Clientes.ExisteAsync(clienteId, cancelacion))
        {
            return Resultado.Fallo<IReadOnlyList<HistorialClienteDto>>(
                "El cliente indicado no existe.", TipoError.NoEncontrado);
        }

        var historial = await _unidadDeTrabajo.HistorialClientes
            .ObtenerPorClienteAsync(clienteId, cancelacion);

        IReadOnlyList<HistorialClienteDto> resultado = historial
            .Select(h => new HistorialClienteDto(
                h.Id, h.Accion, h.Detalle, h.Usuario?.Nombre ?? "Sistema", h.FechaRegistro))
            .ToList();

        return Resultado.Exitoso(resultado);
    }

    public async Task<Resultado<IReadOnlyList<ReservaDto>>> ConsultarReservasAsync(
        int clienteId, CancellationToken cancelacion = default)
    {
        if (!await _unidadDeTrabajo.Clientes.ExisteAsync(clienteId, cancelacion))
        {
            return Resultado.Fallo<IReadOnlyList<ReservaDto>>(
                "El cliente indicado no existe.", TipoError.NoEncontrado);
        }

        var reservas = await _unidadDeTrabajo.Reservas.ObtenerPorClienteAsync(clienteId, cancelacion);

        IReadOnlyList<ReservaDto> resultado = reservas.Select(MapeadorReservas.AReservaDto).ToList();

        return Resultado.Exitoso(resultado);
    }

    private async Task RegistrarHistorialAsync(
        int clienteId, string accion, string detalle, CancellationToken cancelacion)
    {
        var entrada = new HistorialCliente
        {
            ClienteId = clienteId,
            Accion = accion,
            Detalle = detalle,
            UsuarioId = _usuarioActual.Id ?? 0,
            FechaRegistro = _reloj.Ahora
        };

        await _unidadDeTrabajo.HistorialClientes.AgregarAsync(entrada, cancelacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);
    }

    private static List<string> DescribirCambios(Cliente cliente, ActualizarClienteDto solicitud)
    {
        var cambios = new List<string>();

        void Comparar(string campo, string? anterior, string? nuevo)
        {
            if (!string.Equals(anterior?.Trim(), nuevo?.Trim(), StringComparison.Ordinal))
            {
                cambios.Add($"{campo}: '{anterior}' → '{nuevo}'");
            }
        }

        Comparar("Nombre", cliente.Nombre, solicitud.Nombre);
        Comparar("Apellidos", cliente.Apellidos, solicitud.Apellidos);
        Comparar("Correo", cliente.Correo, solicitud.Correo);
        Comparar("Teléfono", cliente.Telefono, solicitud.Telefono);
        Comparar("Nacionalidad", cliente.Nacionalidad, solicitud.Nacionalidad);

        if (cliente.Activo != solicitud.Activo)
        {
            cambios.Add($"Estado: {(cliente.Activo ? "activo" : "inactivo")} → " +
                        $"{(solicitud.Activo ? "activo" : "inactivo")}");
        }

        return cambios;
    }
}
