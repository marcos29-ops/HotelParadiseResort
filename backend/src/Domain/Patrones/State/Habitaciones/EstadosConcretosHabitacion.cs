using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Domain.Patrones.State.Habitaciones;

/// <summary>Habitación libre: puede reservarse o recibir un huésped sin reserva previa.</summary>
public sealed class EstadoDisponible : EstadoHabitacionBase
{
    public override TipoEstadoHabitacion Tipo => TipoEstadoHabitacion.Disponible;

    public override string Nombre => "Disponible";

    public override bool PermiteReservar => true;

    public override bool PermiteCheckIn => true;

    protected override IReadOnlySet<TipoEstadoHabitacion> TransicionesPermitidas { get; } =
        new HashSet<TipoEstadoHabitacion>
        {
            TipoEstadoHabitacion.Reservada,
            TipoEstadoHabitacion.Ocupada,
            TipoEstadoHabitacion.EnLimpieza,
            TipoEstadoHabitacion.EnMantenimiento
        };
}

/// <summary>
/// Habitación comprometida por una reserva confirmada. Admite el check-in del huésped
/// o volver a estar disponible si la reserva se cancela.
/// </summary>
public sealed class EstadoReservada : EstadoHabitacionBase
{
    public override TipoEstadoHabitacion Tipo => TipoEstadoHabitacion.Reservada;

    public override string Nombre => "Reservada";

    public override bool PermiteReservar => false;

    public override bool PermiteCheckIn => true;

    protected override IReadOnlySet<TipoEstadoHabitacion> TransicionesPermitidas { get; } =
        new HashSet<TipoEstadoHabitacion>
        {
            TipoEstadoHabitacion.Ocupada,
            TipoEstadoHabitacion.Disponible,
            TipoEstadoHabitacion.EnMantenimiento
        };
}

/// <summary>Habitación con huésped hospedado. Solo sale de este estado con el check-out.</summary>
public sealed class EstadoOcupada : EstadoHabitacionBase
{
    public override TipoEstadoHabitacion Tipo => TipoEstadoHabitacion.Ocupada;

    public override string Nombre => "Ocupada";

    public override bool PermiteReservar => false;

    public override bool PermiteCheckIn => false;

    protected override IReadOnlySet<TipoEstadoHabitacion> TransicionesPermitidas { get; } =
        new HashSet<TipoEstadoHabitacion>
        {
            TipoEstadoHabitacion.EnLimpieza,
            TipoEstadoHabitacion.EnMantenimiento
        };
}

/// <summary>Habitación en preparación tras un check-out. No admite huéspedes todavía.</summary>
public sealed class EstadoEnLimpieza : EstadoHabitacionBase
{
    public override TipoEstadoHabitacion Tipo => TipoEstadoHabitacion.EnLimpieza;

    public override string Nombre => "En limpieza";

    public override bool PermiteReservar => false;

    public override bool PermiteCheckIn => false;

    protected override IReadOnlySet<TipoEstadoHabitacion> TransicionesPermitidas { get; } =
        new HashSet<TipoEstadoHabitacion>
        {
            TipoEstadoHabitacion.Disponible,
            TipoEstadoHabitacion.EnMantenimiento
        };
}

/// <summary>Habitación fuera de servicio. Queda excluida del inventario disponible.</summary>
public sealed class EstadoEnMantenimiento : EstadoHabitacionBase
{
    public override TipoEstadoHabitacion Tipo => TipoEstadoHabitacion.EnMantenimiento;

    public override string Nombre => "En mantenimiento";

    public override bool PermiteReservar => false;

    public override bool PermiteCheckIn => false;

    protected override IReadOnlySet<TipoEstadoHabitacion> TransicionesPermitidas { get; } =
        new HashSet<TipoEstadoHabitacion>
        {
            TipoEstadoHabitacion.Disponible,
            TipoEstadoHabitacion.EnLimpieza
        };
}
