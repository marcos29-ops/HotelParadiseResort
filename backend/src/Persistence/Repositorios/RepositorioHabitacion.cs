using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Persistence.Contexto;
using Microsoft.EntityFrameworkCore;

namespace HotelParadiseResort.Persistence.Repositorios;

public sealed class RepositorioHabitacion : RepositorioBase<Habitacion>, IRepositorioHabitacion
{
    /// <summary>
    /// Estados de reserva que mantienen comprometida una habitación. Coincide con la
    /// propiedad <c>ComprometeHabitacion</c> del patrón State; se replica aquí como
    /// arreglo porque el proveedor de EF Core debe traducirlo a SQL.
    /// </summary>
    private static readonly TipoEstadoReserva[] EstadosQueComprometen =
    [
        TipoEstadoReserva.Pendiente,
        TipoEstadoReserva.Confirmada
    ];

    /// <summary>
    /// Estados que retiran la habitación del inventario reservable.
    ///
    /// Solo el mantenimiento cuenta: es una condición persistente de fuera de servicio.
    /// "Ocupada" y "En limpieza" describen el instante actual y no deben impedir una
    /// reserva para fechas posteriores —un hotel toma reservas con meses de antelación
    /// sobre habitaciones ocupadas hoy—. El solapamiento con otras reservas es lo que
    /// determina la disponibilidad de un rango, y el estado real se vuelve a verificar
    /// en el check-in, que es el momento en que la habitación debe estar lista.
    /// </summary>
    private static readonly TipoEstadoHabitacion[] EstadosNoOperativos =
    [
        TipoEstadoHabitacion.EnMantenimiento
    ];

    public RepositorioHabitacion(HotelDbContext contexto) : base(contexto)
    {
    }

    public async Task<Habitacion?> ObtenerPorNumeroAsync(
        string numero, CancellationToken cancelacion = default) =>
        await Conjunto.FirstOrDefaultAsync(h => h.Numero == numero, cancelacion);

    public async Task<Habitacion?> ObtenerConTipoAsync(int id, CancellationToken cancelacion = default) =>
        await Conjunto
            .Include(h => h.TipoHabitacion)
            .FirstOrDefaultAsync(h => h.Id == id, cancelacion);

    public async Task<IReadOnlyList<Habitacion>> ObtenerTodasConTipoAsync(
        CancellationToken cancelacion = default) =>
        await Conjunto
            .AsNoTracking()
            .Include(h => h.TipoHabitacion)
            .Where(h => h.Activo)
            .OrderBy(h => h.Piso)
            .ThenBy(h => h.Numero)
            .ToListAsync(cancelacion);

    /// <summary>
    /// Habitaciones libres en el rango. Descarta las no operativas y aquellas con una
    /// reserva vigente que se solape. Dos estadías que se tocan en un extremo no se
    /// solapan: quien sale libera la habitación el mismo día en que entra el siguiente.
    /// </summary>
    public async Task<IReadOnlyList<Habitacion>> ObtenerDisponiblesAsync(
        DateTime fechaEntrada,
        DateTime fechaSalida,
        int? tipoHabitacionId = null,
        int? reservaExcluidaId = null,
        CancellationToken cancelacion = default)
    {
        var entrada = fechaEntrada.Date;
        var salida = fechaSalida.Date;

        var consulta = Conjunto
            .AsNoTracking()
            .Include(h => h.TipoHabitacion)
            .Where(h => h.Activo && !EstadosNoOperativos.Contains(h.Estado));

        if (tipoHabitacionId.HasValue)
        {
            consulta = consulta.Where(h => h.TipoHabitacionId == tipoHabitacionId.Value);
        }

        consulta = consulta.Where(h => !Contexto.Reservas.Any(r =>
            r.HabitacionId == h.Id &&
            EstadosQueComprometen.Contains(r.Estado) &&
            (reservaExcluidaId == null || r.Id != reservaExcluidaId) &&
            r.FechaEntrada < salida &&
            entrada < r.FechaSalida));

        return await consulta
            .OrderBy(h => h.Piso)
            .ThenBy(h => h.Numero)
            .ToListAsync(cancelacion);
    }

    /// <summary>
    /// Verificación puntual de disponibilidad. Se ejecuta dentro de la transacción
    /// serializable que abre el servicio de reservas, de modo que el resultado siga
    /// siendo válido en el momento de escribir (RNF03).
    /// </summary>
    public async Task<bool> EstaDisponibleAsync(
        int habitacionId,
        DateTime fechaEntrada,
        DateTime fechaSalida,
        int? reservaExcluidaId = null,
        CancellationToken cancelacion = default)
    {
        var entrada = fechaEntrada.Date;
        var salida = fechaSalida.Date;

        var habitacionOperativa = await Conjunto.AnyAsync(
            h => h.Id == habitacionId && h.Activo && !EstadosNoOperativos.Contains(h.Estado),
            cancelacion);

        if (!habitacionOperativa)
        {
            return false;
        }

        var tieneSolapamiento = await Contexto.Reservas.AnyAsync(r =>
                r.HabitacionId == habitacionId &&
                EstadosQueComprometen.Contains(r.Estado) &&
                (reservaExcluidaId == null || r.Id != reservaExcluidaId) &&
                r.FechaEntrada < salida &&
                entrada < r.FechaSalida,
            cancelacion);

        return !tieneSolapamiento;
    }

    public async Task<IReadOnlyDictionary<TipoEstadoHabitacion, int>> ContarPorEstadoAsync(
        CancellationToken cancelacion = default) =>
        await Conjunto
            .AsNoTracking()
            .Where(h => h.Activo)
            .GroupBy(h => h.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Estado, x => x.Cantidad, cancelacion);

    public async Task<int> ContarActivasAsync(CancellationToken cancelacion = default) =>
        await Conjunto.CountAsync(h => h.Activo, cancelacion);

    public async Task<bool> ExisteNumeroAsync(
        string numero, int? idExcluir = null, CancellationToken cancelacion = default) =>
        await Conjunto.AnyAsync(
            h => h.Numero == numero && (idExcluir == null || h.Id != idExcluir),
            cancelacion);
}
