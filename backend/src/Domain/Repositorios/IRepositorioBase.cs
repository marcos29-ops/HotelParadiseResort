using HotelParadiseResort.Domain.Comun;

namespace HotelParadiseResort.Domain.Repositorios;

/// <summary>
/// Operaciones comunes de acceso a datos.
///
/// PRINCIPIO DIP — La interfaz vive en el dominio y la implementación en la capa de
/// persistencia: el negocio no conoce Entity Framework ni el motor de base de datos.
/// </summary>
public interface IRepositorioBase<T> where T : EntidadBase
{
    Task<T?> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default);

    Task<IReadOnlyList<T>> ObtenerTodosAsync(CancellationToken cancelacion = default);

    Task AgregarAsync(T entidad, CancellationToken cancelacion = default);

    void Actualizar(T entidad);

    void Eliminar(T entidad);

    Task<bool> ExisteAsync(int id, CancellationToken cancelacion = default);
}
