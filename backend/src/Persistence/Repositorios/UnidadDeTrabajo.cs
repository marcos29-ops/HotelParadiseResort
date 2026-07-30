using System.Data;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Persistence.Contexto;
using Microsoft.EntityFrameworkCore;

namespace HotelParadiseResort.Persistence.Repositorios;

/// <summary>
/// Agrupa los repositorios sobre un mismo contexto y expone la ejecución
/// transaccional que exige RNF03.
/// </summary>
public sealed class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private readonly HotelDbContext _contexto;

    public UnidadDeTrabajo(HotelDbContext contexto)
    {
        _contexto = contexto;

        Usuarios = new RepositorioUsuario(contexto);
        Clientes = new RepositorioCliente(contexto);
        Habitaciones = new RepositorioHabitacion(contexto);
        TiposHabitacion = new RepositorioTipoHabitacion(contexto);
        Reservas = new RepositorioReserva(contexto);
        Estadias = new RepositorioEstadia(contexto);
        Consumos = new RepositorioConsumo(contexto);
        ServiciosAdicionales = new RepositorioServicioAdicional(contexto);
        Facturas = new RepositorioFactura(contexto);
        HistorialClientes = new RepositorioHistorialCliente(contexto);
    }

    public IRepositorioUsuario Usuarios { get; }

    public IRepositorioCliente Clientes { get; }

    public IRepositorioHabitacion Habitaciones { get; }

    public IRepositorioTipoHabitacion TiposHabitacion { get; }

    public IRepositorioReserva Reservas { get; }

    public IRepositorioEstadia Estadias { get; }

    public IRepositorioConsumo Consumos { get; }

    public IRepositorioServicioAdicional ServiciosAdicionales { get; }

    public IRepositorioFactura Facturas { get; }

    public IRepositorioHistorialCliente HistorialClientes { get; }

    public Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default) =>
        _contexto.SaveChangesAsync(cancelacion);

    /// <summary>
    /// Ejecuta la operación con aislamiento serializable. Es lo que impide que dos
    /// recepcionistas confirmen simultáneamente la misma habitación: la verificación de
    /// disponibilidad y la escritura de la reserva ocurren dentro de la misma transacción.
    /// </summary>
    public async Task<TResultado> EjecutarEnTransaccionAsync<TResultado>(
        Func<CancellationToken, Task<TResultado>> operacion,
        CancellationToken cancelacion = default)
    {
        // Una transacción ya abierta significa que la operación forma parte de un
        // flujo mayor: se respeta la transacción existente en lugar de anidar otra.
        if (_contexto.Database.CurrentTransaction is not null)
        {
            return await operacion(cancelacion);
        }

        var estrategia = _contexto.Database.CreateExecutionStrategy();

        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion =
                await _contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancelacion);

            try
            {
                var resultado = await operacion(cancelacion);
                await _contexto.SaveChangesAsync(cancelacion);
                await transaccion.CommitAsync(cancelacion);
                return resultado;
            }
            catch
            {
                await transaccion.RollbackAsync(cancelacion);
                throw;
            }
        });
    }
}
