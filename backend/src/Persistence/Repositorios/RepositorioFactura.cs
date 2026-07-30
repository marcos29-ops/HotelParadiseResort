using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Persistence.Contexto;
using HotelParadiseResort.Shared.Paginacion;
using Microsoft.EntityFrameworkCore;

namespace HotelParadiseResort.Persistence.Repositorios;

public sealed class RepositorioFactura : RepositorioBase<Factura>, IRepositorioFactura
{
    public RepositorioFactura(HotelDbContext contexto) : base(contexto)
    {
    }

    public async Task<Factura?> ObtenerCompletaAsync(int id, CancellationToken cancelacion = default) =>
        await Conjunto
            .Include(f => f.Cliente)
            .Include(f => f.Detalles)
            .Include(f => f.UsuarioEmision)
            .Include(f => f.Estadia)
                .ThenInclude(e => e!.Habitacion)
                    .ThenInclude(h => h!.TipoHabitacion)
            .Include(f => f.Estadia)
                .ThenInclude(e => e!.Reserva)
            .FirstOrDefaultAsync(f => f.Id == id, cancelacion);

    public async Task<Factura?> ObtenerPorEstadiaAsync(
        int estadiaId, CancellationToken cancelacion = default) =>
        await Conjunto
            .Include(f => f.Detalles)
            .FirstOrDefaultAsync(f => f.EstadiaId == estadiaId, cancelacion);

    public async Task<Factura?> ObtenerPorNumeroAsync(
        string numero, CancellationToken cancelacion = default) =>
        await Conjunto
            .Include(f => f.Cliente)
            .Include(f => f.Detalles)
            .FirstOrDefaultAsync(f => f.Numero == numero, cancelacion);

    public async Task<ResultadoPaginado<Factura>> BuscarAsync(
        ParametrosPaginacion parametros,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancelacion = default)
    {
        var consulta = Conjunto
            .AsNoTracking()
            .Include(f => f.Cliente)
            .Include(f => f.Estadia)
                .ThenInclude(e => e!.Habitacion)
            .AsQueryable();

        if (desde.HasValue)
        {
            var inicio = desde.Value.Date;
            consulta = consulta.Where(f => f.FechaEmision >= inicio);
        }

        if (hasta.HasValue)
        {
            var fin = hasta.Value.Date.AddDays(1);
            consulta = consulta.Where(f => f.FechaEmision < fin);
        }

        if (!string.IsNullOrWhiteSpace(parametros.Busqueda))
        {
            var termino = parametros.Busqueda.Trim();
            consulta = consulta.Where(f =>
                EF.Functions.Like(f.Numero, $"%{termino}%") ||
                EF.Functions.Like(f.Cliente!.Nombre, $"%{termino}%") ||
                EF.Functions.Like(f.Cliente!.Apellidos, $"%{termino}%") ||
                EF.Functions.Like(f.Cliente!.Identificacion, $"%{termino}%"));
        }

        var total = await consulta.CountAsync(cancelacion);

        consulta = parametros.OrdenarPor?.ToLowerInvariant() switch
        {
            "numero" => parametros.Descendente
                ? consulta.OrderByDescending(f => f.Numero)
                : consulta.OrderBy(f => f.Numero),
            "total" => parametros.Descendente
                ? consulta.OrderByDescending(f => f.Total)
                : consulta.OrderBy(f => f.Total),
            _ => parametros.Descendente
                ? consulta.OrderByDescending(f => f.FechaEmision)
                : consulta.OrderBy(f => f.FechaEmision)
        };

        var elementos = await consulta
            .Skip(parametros.RegistrosOmitidos)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new ResultadoPaginado<Factura>(elementos, total, parametros.Pagina, parametros.TamanoPagina);
    }

    /// <summary>
    /// Consecutivo del comprobante. Se emite dentro de la transacción de facturación
    /// para que dos check-out simultáneos no obtengan el mismo número.
    /// </summary>
    public async Task<string> GenerarNumeroAsync(CancellationToken cancelacion = default)
    {
        var ultimo = await Conjunto
            .OrderByDescending(f => f.Id)
            .Select(f => f.Numero)
            .FirstOrDefaultAsync(cancelacion);

        var consecutivo = 1;

        if (!string.IsNullOrWhiteSpace(ultimo) &&
            int.TryParse(ultimo.AsSpan(ultimo.LastIndexOf('-') + 1), out var ultimoNumero))
        {
            consecutivo = ultimoNumero + 1;
        }

        return $"FAC-{consecutivo:D6}";
    }

    public async Task<decimal> ObtenerIngresosDelDiaAsync(
        DateTime fecha, CancellationToken cancelacion = default)
    {
        var inicio = fecha.Date;
        var fin = inicio.AddDays(1);

        return await Conjunto
            .Where(f => f.FechaEmision >= inicio &&
                        f.FechaEmision < fin &&
                        f.EstadoPago != EstadoPago.Anulado)
            .SumAsync(f => (decimal?)f.Total, cancelacion) ?? 0m;
    }

    public async Task<IReadOnlyList<Factura>> ObtenerEnRangoAsync(
        DateTime desde, DateTime hasta, CancellationToken cancelacion = default)
    {
        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        return await Conjunto
            .AsNoTracking()
            .Where(f => f.FechaEmision >= inicio &&
                        f.FechaEmision < fin &&
                        f.EstadoPago != EstadoPago.Anulado)
            .ToListAsync(cancelacion);
    }
}
