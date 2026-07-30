using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Persistence.Contexto;
using Microsoft.EntityFrameworkCore;

namespace HotelParadiseResort.Persistence.Repositorios;

public sealed class RepositorioTipoHabitacion : RepositorioBase<TipoHabitacion>, IRepositorioTipoHabitacion
{
    public RepositorioTipoHabitacion(HotelDbContext contexto) : base(contexto)
    {
    }

    public async Task<IReadOnlyList<TipoHabitacion>> ObtenerActivosAsync(
        CancellationToken cancelacion = default) =>
        await Conjunto
            .AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .ToListAsync(cancelacion);

    public async Task<bool> TieneHabitacionesAsociadasAsync(
        int id, CancellationToken cancelacion = default) =>
        await Contexto.Habitaciones.AnyAsync(h => h.TipoHabitacionId == id, cancelacion);

    public async Task<bool> ExisteNombreAsync(
        string nombre, int? idExcluir = null, CancellationToken cancelacion = default) =>
        await Conjunto.AnyAsync(
            t => t.Nombre == nombre && (idExcluir == null || t.Id != idExcluir),
            cancelacion);
}

public sealed class RepositorioServicioAdicional
    : RepositorioBase<ServicioAdicional>, IRepositorioServicioAdicional
{
    public RepositorioServicioAdicional(HotelDbContext contexto) : base(contexto)
    {
    }

    public async Task<IReadOnlyList<ServicioAdicional>> ObtenerActivosAsync(
        CancellationToken cancelacion = default) =>
        await Conjunto
            .AsNoTracking()
            .Where(s => s.Activo)
            .OrderBy(s => s.Tipo)
            .ThenBy(s => s.Nombre)
            .ToListAsync(cancelacion);

    public async Task<bool> TieneConsumosAsociadosAsync(
        int id, CancellationToken cancelacion = default) =>
        await Contexto.Consumos.AnyAsync(c => c.ServicioAdicionalId == id, cancelacion);

    public async Task<bool> ExisteNombreAsync(
        string nombre, int? idExcluir = null, CancellationToken cancelacion = default) =>
        await Conjunto.AnyAsync(
            s => s.Nombre == nombre && (idExcluir == null || s.Id != idExcluir),
            cancelacion);
}

public sealed class RepositorioConsumo : RepositorioBase<Consumo>, IRepositorioConsumo
{
    public RepositorioConsumo(HotelDbContext contexto) : base(contexto)
    {
    }

    public async Task<IReadOnlyList<Consumo>> ObtenerPorEstadiaAsync(
        int estadiaId, CancellationToken cancelacion = default) =>
        await Conjunto
            .AsNoTracking()
            .Include(c => c.ServicioAdicional)
            .Where(c => c.EstadiaId == estadiaId)
            .OrderBy(c => c.FechaConsumo)
            .ToListAsync(cancelacion);

    public async Task<Consumo?> ObtenerConServicioAsync(int id, CancellationToken cancelacion = default) =>
        await Conjunto
            .Include(c => c.ServicioAdicional)
            .FirstOrDefaultAsync(c => c.Id == id, cancelacion);

    /// <summary>Suma los consumos en la base de datos, sin materializar la colección.</summary>
    public async Task<decimal> ObtenerTotalPorEstadiaAsync(
        int estadiaId, CancellationToken cancelacion = default) =>
        await Conjunto
            .Where(c => c.EstadiaId == estadiaId)
            .SumAsync(c => (decimal?)(c.PrecioUnitario * c.Cantidad), cancelacion) ?? 0m;
}

public sealed class RepositorioHistorialCliente
    : RepositorioBase<HistorialCliente>, IRepositorioHistorialCliente
{
    public RepositorioHistorialCliente(HotelDbContext contexto) : base(contexto)
    {
    }

    public async Task<IReadOnlyList<HistorialCliente>> ObtenerPorClienteAsync(
        int clienteId, CancellationToken cancelacion = default) =>
        await Conjunto
            .AsNoTracking()
            .Include(h => h.Usuario)
            .Where(h => h.ClienteId == clienteId)
            .OrderByDescending(h => h.FechaRegistro)
            .ToListAsync(cancelacion);
}
