using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Persistence.Contexto;
using HotelParadiseResort.Shared.Paginacion;
using Microsoft.EntityFrameworkCore;

namespace HotelParadiseResort.Persistence.Repositorios;

public sealed class RepositorioCliente : RepositorioBase<Cliente>, IRepositorioCliente
{
    public RepositorioCliente(HotelDbContext contexto) : base(contexto)
    {
    }

    public async Task<Cliente?> ObtenerPorIdentificacionAsync(
        string identificacion, CancellationToken cancelacion = default) =>
        await Conjunto.FirstOrDefaultAsync(c => c.Identificacion == identificacion, cancelacion);

    public async Task<bool> ExisteIdentificacionAsync(
        string identificacion, int? idExcluir = null, CancellationToken cancelacion = default) =>
        await Conjunto.AnyAsync(
            c => c.Identificacion == identificacion && (idExcluir == null || c.Id != idExcluir),
            cancelacion);

    public async Task<ResultadoPaginado<Cliente>> BuscarAsync(
        ParametrosPaginacion parametros, CancellationToken cancelacion = default)
    {
        var consulta = Conjunto.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parametros.Busqueda))
        {
            var termino = parametros.Busqueda.Trim();
            consulta = consulta.Where(c =>
                EF.Functions.Like(c.Nombre, $"%{termino}%") ||
                EF.Functions.Like(c.Apellidos, $"%{termino}%") ||
                EF.Functions.Like(c.Identificacion, $"%{termino}%") ||
                (c.Correo != null && EF.Functions.Like(c.Correo, $"%{termino}%")) ||
                (c.Telefono != null && EF.Functions.Like(c.Telefono, $"%{termino}%")));
        }

        var total = await consulta.CountAsync(cancelacion);

        consulta = parametros.OrdenarPor?.ToLowerInvariant() switch
        {
            "identificacion" => parametros.Descendente
                ? consulta.OrderByDescending(c => c.Identificacion)
                : consulta.OrderBy(c => c.Identificacion),
            "nombre" => parametros.Descendente
                ? consulta.OrderByDescending(c => c.Nombre)
                : consulta.OrderBy(c => c.Nombre),
            _ => parametros.Descendente
                ? consulta.OrderByDescending(c => c.Apellidos).ThenByDescending(c => c.Nombre)
                : consulta.OrderBy(c => c.Apellidos).ThenBy(c => c.Nombre)
        };

        var elementos = await consulta
            .Skip(parametros.RegistrosOmitidos)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new ResultadoPaginado<Cliente>(elementos, total, parametros.Pagina, parametros.TamanoPagina);
    }

    /// <summary>
    /// Obtiene el conteo de reservas de varios clientes en una sola consulta, para que
    /// el listado no incurra en el problema N+1 al mostrar la columna "Reservas".
    /// </summary>
    public async Task<IReadOnlyDictionary<int, int>> ContarReservasPorClienteAsync(
        IEnumerable<int> clienteIds, CancellationToken cancelacion = default)
    {
        var ids = clienteIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        return await Contexto.Reservas
            .AsNoTracking()
            .Where(r => ids.Contains(r.ClienteId))
            .GroupBy(r => r.ClienteId)
            .Select(g => new { ClienteId = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.ClienteId, x => x.Cantidad, cancelacion);
    }
}
