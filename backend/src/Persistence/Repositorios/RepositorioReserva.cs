using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Persistence.Contexto;
using HotelParadiseResort.Shared.Paginacion;
using Microsoft.EntityFrameworkCore;

namespace HotelParadiseResort.Persistence.Repositorios;

public sealed class RepositorioReserva : RepositorioBase<Reserva>, IRepositorioReserva
{
    public RepositorioReserva(HotelDbContext contexto) : base(contexto)
    {
    }

    public async Task<Reserva?> ObtenerCompletaAsync(int id, CancellationToken cancelacion = default) =>
        await Conjunto
            .Include(r => r.Cliente)
            .Include(r => r.Habitacion)
                .ThenInclude(h => h!.TipoHabitacion)
            .Include(r => r.UsuarioRegistro)
            .Include(r => r.Estadia)
            .FirstOrDefaultAsync(r => r.Id == id, cancelacion);

    public async Task<Reserva?> ObtenerPorCodigoAsync(
        string codigo, CancellationToken cancelacion = default) =>
        await Conjunto
            .Include(r => r.Cliente)
            .Include(r => r.Habitacion)
                .ThenInclude(h => h!.TipoHabitacion)
            .Include(r => r.Estadia)
            .FirstOrDefaultAsync(r => r.Codigo == codigo, cancelacion);

    public async Task<ResultadoPaginado<Reserva>> BuscarAsync(
        ParametrosPaginacion parametros,
        TipoEstadoReserva? estado = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancelacion = default)
    {
        var consulta = Conjunto
            .AsNoTracking()
            .Include(r => r.Cliente)
            .Include(r => r.Habitacion)
                .ThenInclude(h => h!.TipoHabitacion)
            .AsQueryable();

        if (estado.HasValue)
        {
            consulta = consulta.Where(r => r.Estado == estado.Value);
        }

        if (desde.HasValue)
        {
            var fechaDesde = desde.Value.Date;
            consulta = consulta.Where(r => r.FechaSalida >= fechaDesde);
        }

        if (hasta.HasValue)
        {
            var fechaHasta = hasta.Value.Date;
            consulta = consulta.Where(r => r.FechaEntrada <= fechaHasta);
        }

        if (!string.IsNullOrWhiteSpace(parametros.Busqueda))
        {
            var termino = parametros.Busqueda.Trim();
            consulta = consulta.Where(r =>
                EF.Functions.Like(r.Codigo, $"%{termino}%") ||
                EF.Functions.Like(r.Cliente!.Nombre, $"%{termino}%") ||
                EF.Functions.Like(r.Cliente!.Apellidos, $"%{termino}%") ||
                EF.Functions.Like(r.Cliente!.Identificacion, $"%{termino}%") ||
                EF.Functions.Like(r.Habitacion!.Numero, $"%{termino}%"));
        }

        var total = await consulta.CountAsync(cancelacion);

        consulta = parametros.OrdenarPor?.ToLowerInvariant() switch
        {
            "codigo" => parametros.Descendente
                ? consulta.OrderByDescending(r => r.Codigo)
                : consulta.OrderBy(r => r.Codigo),
            "fechasalida" => parametros.Descendente
                ? consulta.OrderByDescending(r => r.FechaSalida)
                : consulta.OrderBy(r => r.FechaSalida),
            "estado" => parametros.Descendente
                ? consulta.OrderByDescending(r => r.Estado)
                : consulta.OrderBy(r => r.Estado),
            _ => parametros.Descendente
                ? consulta.OrderByDescending(r => r.FechaEntrada)
                : consulta.OrderBy(r => r.FechaEntrada)
        };

        var elementos = await consulta
            .Skip(parametros.RegistrosOmitidos)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new ResultadoPaginado<Reserva>(elementos, total, parametros.Pagina, parametros.TamanoPagina);
    }

    public async Task<IReadOnlyList<Reserva>> ObtenerPorClienteAsync(
        int clienteId, CancellationToken cancelacion = default) =>
        await Conjunto
            .AsNoTracking()
            .Include(r => r.Habitacion)
                .ThenInclude(h => h!.TipoHabitacion)
            .Where(r => r.ClienteId == clienteId)
            .OrderByDescending(r => r.FechaEntrada)
            .ToListAsync(cancelacion);

    public async Task<IReadOnlyList<Reserva>> ObtenerLlegadasDelDiaAsync(
        DateTime fecha, CancellationToken cancelacion = default)
    {
        var dia = fecha.Date;

        return await Conjunto
            .AsNoTracking()
            .Include(r => r.Cliente)
            .Include(r => r.Habitacion)
                .ThenInclude(h => h!.TipoHabitacion)
            .Where(r => r.FechaEntrada == dia && r.Estado == TipoEstadoReserva.Confirmada)
            .OrderBy(r => r.Habitacion!.Numero)
            .ToListAsync(cancelacion);
    }

    public async Task<int> ContarPorEstadoAsync(
        TipoEstadoReserva estado, CancellationToken cancelacion = default) =>
        await Conjunto.CountAsync(r => r.Estado == estado, cancelacion);

    public async Task<int> ContarReservasDelDiaAsync(
        DateTime fecha, CancellationToken cancelacion = default)
    {
        var dia = fecha.Date;

        return await Conjunto.CountAsync(
            r => r.FechaEntrada == dia && r.Estado != TipoEstadoReserva.Cancelada,
            cancelacion);
    }

    /// <summary>
    /// Genera el consecutivo de la reserva. Se invoca dentro de la transacción
    /// serializable de creación, de modo que dos peticiones no obtengan el mismo código.
    /// </summary>
    public async Task<string> GenerarCodigoAsync(CancellationToken cancelacion = default)
    {
        var ultimo = await Conjunto
            .OrderByDescending(r => r.Id)
            .Select(r => r.Codigo)
            .FirstOrDefaultAsync(cancelacion);

        var consecutivo = 1;

        if (!string.IsNullOrWhiteSpace(ultimo) &&
            int.TryParse(ultimo.AsSpan(ultimo.LastIndexOf('-') + 1), out var ultimoNumero))
        {
            consecutivo = ultimoNumero + 1;
        }

        return $"RES-{consecutivo:D5}";
    }

    public async Task<IReadOnlyList<Reserva>> ObtenerEnRangoAsync(
        DateTime desde,
        DateTime hasta,
        int? tipoHabitacionId = null,
        CancellationToken cancelacion = default)
    {
        var fechaDesde = desde.Date;
        var fechaHasta = hasta.Date;

        var consulta = Conjunto
            .AsNoTracking()
            .Include(r => r.Habitacion)
            .Where(r => r.Estado != TipoEstadoReserva.Cancelada &&
                        r.FechaEntrada >= fechaDesde &&
                        r.FechaEntrada <= fechaHasta);

        if (tipoHabitacionId.HasValue)
        {
            consulta = consulta.Where(r => r.Habitacion!.TipoHabitacionId == tipoHabitacionId.Value);
        }

        return await consulta.ToListAsync(cancelacion);
    }
}
