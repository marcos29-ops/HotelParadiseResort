using HotelParadiseResort.Domain.Comun;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Persistence.Contexto;
using Microsoft.EntityFrameworkCore;

namespace HotelParadiseResort.Persistence.Repositorios;

/// <summary>
/// Implementación común de acceso a datos. Los repositorios específicos heredan de
/// aquí y añaden únicamente las consultas propias de su agregado.
/// </summary>
public abstract class RepositorioBase<T> : IRepositorioBase<T> where T : EntidadBase
{
    protected RepositorioBase(HotelDbContext contexto)
    {
        Contexto = contexto;
        Conjunto = contexto.Set<T>();
    }

    protected HotelDbContext Contexto { get; }

    protected DbSet<T> Conjunto { get; }

    public virtual async Task<T?> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default) =>
        await Conjunto.FirstOrDefaultAsync(e => e.Id == id, cancelacion);

    public virtual async Task<IReadOnlyList<T>> ObtenerTodosAsync(CancellationToken cancelacion = default) =>
        await Conjunto.AsNoTracking().ToListAsync(cancelacion);

    public virtual async Task AgregarAsync(T entidad, CancellationToken cancelacion = default) =>
        await Conjunto.AddAsync(entidad, cancelacion);

    public virtual void Actualizar(T entidad) => Conjunto.Update(entidad);

    public virtual void Eliminar(T entidad) => Conjunto.Remove(entidad);

    public virtual async Task<bool> ExisteAsync(int id, CancellationToken cancelacion = default) =>
        await Conjunto.AnyAsync(e => e.Id == id, cancelacion);
}
