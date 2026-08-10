using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Persistence.Contexto;
using HotelParadiseResort.Shared.Paginacion;
using Microsoft.EntityFrameworkCore;

namespace HotelParadiseResort.Persistence.Repositorios;

public sealed class RepositorioEstadia : RepositorioBase<Estadia>, IRepositorioEstadia
{
    public RepositorioEstadia(HotelDbContext contexto) : base(contexto)
    {
    }

    public async Task<Estadia?> ObtenerCompletaAsync(int id, CancellationToken cancelacion = default) =>
        await Conjunto
            .Include(e => e.Reserva)
                .ThenInclude(r => r!.Cliente)
            .Include(e => e.Habitacion)
                .ThenInclude(h => h!.TipoHabitacion)
            .Include(e => e.Consumos)
                .ThenInclude(c => c.ServicioAdicional)
            .Include(e => e.Factura)
            .FirstOrDefaultAsync(e => e.Id == id, cancelacion);

    public async Task<Estadia?> ObtenerPorReservaAsync(
        int reservaId, CancellationToken cancelacion = default) =>
        await Conjunto
            .Include(e => e.Habitacion)
                .ThenInclude(h => h!.TipoHabitacion)
            .Include(e => e.Consumos)
                .ThenInclude(c => c.ServicioAdicional)
            .FirstOrDefaultAsync(e => e.ReservaId == reservaId, cancelacion);

    public async Task<Estadia?> ObtenerActivaPorHabitacionAsync(
        int habitacionId, CancellationToken cancelacion = default) =>
        await Conjunto
            .Include(e => e.Reserva)
                .ThenInclude(r => r!.Cliente)
            .Include(e => e.Habitacion)
                .ThenInclude(h => h!.TipoHabitacion)
            .Include(e => e.Consumos)
                .ThenInclude(c => c.ServicioAdicional)
            .FirstOrDefaultAsync(
                e => e.HabitacionId == habitacionId && e.Estado == TipoEstadoEstadia.EnCurso,
                cancelacion);

    public async Task<ResultadoPaginado<Estadia>> BuscarAsync(
        ParametrosPaginacion parametros,
        TipoEstadoEstadia? estado = null,
        CancellationToken cancelacion = default)
    {
        // Consumos y Factura entran en la proyección porque el DTO expone
        // TotalConsumos y TieneFactura: sin ellos el listado devolvía siempre 0 y
        // false, y la pantalla de facturación mostraba «$0.00» en estadías que sí
        // tenían consumos. Se usa consulta dividida para que la paginación no
        // multiplique filas al unir la colección de consumos.
        var consulta = Conjunto
            .AsNoTracking()
            .Include(e => e.Reserva)
                .ThenInclude(r => r!.Cliente)
            .Include(e => e.Habitacion)
                .ThenInclude(h => h!.TipoHabitacion)
            .Include(e => e.Consumos)
                .ThenInclude(c => c.ServicioAdicional)
            .Include(e => e.Factura)
            .AsSplitQuery()
            .AsQueryable();

        if (estado.HasValue)
        {
            consulta = consulta.Where(e => e.Estado == estado.Value);
        }

        if (!string.IsNullOrWhiteSpace(parametros.Busqueda))
        {
            var termino = parametros.Busqueda.Trim();
            consulta = consulta.Where(e =>
                EF.Functions.Like(e.Habitacion!.Numero, $"%{termino}%") ||
                EF.Functions.Like(e.Reserva!.Codigo, $"%{termino}%") ||
                EF.Functions.Like(e.Reserva!.Cliente!.Nombre, $"%{termino}%") ||
                EF.Functions.Like(e.Reserva!.Cliente!.Apellidos, $"%{termino}%") ||
                EF.Functions.Like(e.Reserva!.Cliente!.Identificacion, $"%{termino}%"));
        }

        var total = await consulta.CountAsync(cancelacion);

        consulta = parametros.Descendente
            ? consulta.OrderByDescending(e => e.FechaCheckIn)
            : consulta.OrderBy(e => e.FechaCheckIn);

        var elementos = await consulta
            .Skip(parametros.RegistrosOmitidos)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new ResultadoPaginado<Estadia>(elementos, total, parametros.Pagina, parametros.TamanoPagina);
    }

    public async Task<int> ContarCheckOutsDelDiaAsync(
        DateTime fecha, CancellationToken cancelacion = default)
    {
        var inicio = fecha.Date;
        var fin = inicio.AddDays(1);

        return await Conjunto.CountAsync(
            e => e.FechaCheckOut >= inicio && e.FechaCheckOut < fin,
            cancelacion);
    }

    public async Task<int> ContarCheckInsDelDiaAsync(
        DateTime fecha, CancellationToken cancelacion = default)
    {
        var inicio = fecha.Date;
        var fin = inicio.AddDays(1);

        return await Conjunto.CountAsync(
            e => e.FechaCheckIn >= inicio && e.FechaCheckIn < fin,
            cancelacion);
    }

    public async Task<IReadOnlyList<Estadia>> ObtenerEnRangoAsync(
        DateTime desde,
        DateTime hasta,
        int? tipoHabitacionId = null,
        CancellationToken cancelacion = default)
    {
        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        var consulta = Conjunto
            .AsNoTracking()
            .Include(e => e.Habitacion)
            .Include(e => e.Reserva)
            .Where(e => e.FechaCheckIn >= inicio && e.FechaCheckIn < fin);

        if (tipoHabitacionId.HasValue)
        {
            consulta = consulta.Where(e => e.Habitacion!.TipoHabitacionId == tipoHabitacionId.Value);
        }

        return await consulta.ToListAsync(cancelacion);
    }
}
