using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Domain.Patrones.State.Reservas;

/// <summary>
/// Estado inicial de toda reserva. Según el diagrama de actividades, la reserva se
/// crea como pendiente y requiere confirmación explícita del recepcionista.
/// </summary>
public sealed class EstadoPendiente : EstadoReservaBase
{
    public override TipoEstadoReserva Tipo => TipoEstadoReserva.Pendiente;

    public override string Nombre => "Pendiente";

    public override bool PermiteModificacion => true;

    public override bool PermiteCheckIn => false;

    public override bool ComprometeHabitacion => true;

    protected override IReadOnlySet<TipoEstadoReserva> TransicionesPermitidas { get; } =
        new HashSet<TipoEstadoReserva>
        {
            TipoEstadoReserva.Confirmada,
            TipoEstadoReserva.Cancelada
        };
}

/// <summary>Reserva confirmada: es la única situación que habilita el check-in.</summary>
public sealed class EstadoConfirmada : EstadoReservaBase
{
    public override TipoEstadoReserva Tipo => TipoEstadoReserva.Confirmada;

    public override string Nombre => "Confirmada";

    public override bool PermiteModificacion => true;

    public override bool PermiteCheckIn => true;

    public override bool ComprometeHabitacion => true;

    protected override IReadOnlySet<TipoEstadoReserva> TransicionesPermitidas { get; } =
        new HashSet<TipoEstadoReserva>
        {
            TipoEstadoReserva.Completada,
            TipoEstadoReserva.Cancelada
        };
}

/// <summary>Reserva anulada. Libera la habitación y es un estado terminal.</summary>
public sealed class EstadoCancelada : EstadoReservaBase
{
    public override TipoEstadoReserva Tipo => TipoEstadoReserva.Cancelada;

    public override string Nombre => "Cancelada";

    public override bool PermiteModificacion => false;

    public override bool PermiteCheckIn => false;

    public override bool ComprometeHabitacion => false;

    protected override IReadOnlySet<TipoEstadoReserva> TransicionesPermitidas { get; } =
        new HashSet<TipoEstadoReserva>();
}

/// <summary>Reserva cuya estadía ya se cumplió. Estado terminal.</summary>
public sealed class EstadoCompletada : EstadoReservaBase
{
    public override TipoEstadoReserva Tipo => TipoEstadoReserva.Completada;

    public override string Nombre => "Completada";

    public override bool PermiteModificacion => false;

    public override bool PermiteCheckIn => false;

    public override bool ComprometeHabitacion => false;

    protected override IReadOnlySet<TipoEstadoReserva> TransicionesPermitidas { get; } =
        new HashSet<TipoEstadoReserva>();
}
