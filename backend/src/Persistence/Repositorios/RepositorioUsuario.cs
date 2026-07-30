using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Persistence.Contexto;
using HotelParadiseResort.Shared.Paginacion;
using Microsoft.EntityFrameworkCore;

namespace HotelParadiseResort.Persistence.Repositorios;

public sealed class RepositorioUsuario : RepositorioBase<Usuario>, IRepositorioUsuario
{
    public RepositorioUsuario(HotelDbContext contexto) : base(contexto)
    {
    }

    public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(
        string nombreUsuario, CancellationToken cancelacion = default) =>
        await Conjunto.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, cancelacion);

    public async Task<bool> ExisteNombreUsuarioAsync(
        string nombreUsuario, int? idExcluir = null, CancellationToken cancelacion = default) =>
        await Conjunto.AnyAsync(
            u => u.NombreUsuario == nombreUsuario && (idExcluir == null || u.Id != idExcluir),
            cancelacion);

    public async Task<ResultadoPaginado<Usuario>> BuscarAsync(
        ParametrosPaginacion parametros, CancellationToken cancelacion = default)
    {
        var consulta = Conjunto.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parametros.Busqueda))
        {
            var termino = parametros.Busqueda.Trim();
            consulta = consulta.Where(u =>
                EF.Functions.Like(u.Nombre, $"%{termino}%") ||
                EF.Functions.Like(u.NombreUsuario, $"%{termino}%") ||
                EF.Functions.Like(u.Correo, $"%{termino}%"));
        }

        var total = await consulta.CountAsync(cancelacion);

        consulta = parametros.OrdenarPor?.ToLowerInvariant() switch
        {
            "nombreusuario" => parametros.Descendente
                ? consulta.OrderByDescending(u => u.NombreUsuario)
                : consulta.OrderBy(u => u.NombreUsuario),
            "rol" => parametros.Descendente
                ? consulta.OrderByDescending(u => u.Rol)
                : consulta.OrderBy(u => u.Rol),
            _ => parametros.Descendente
                ? consulta.OrderByDescending(u => u.Nombre)
                : consulta.OrderBy(u => u.Nombre)
        };

        var elementos = await consulta
            .Skip(parametros.RegistrosOmitidos)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new ResultadoPaginado<Usuario>(elementos, total, parametros.Pagina, parametros.TamanoPagina);
    }
}
