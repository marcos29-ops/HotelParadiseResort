using FluentValidation;
using HotelParadiseResort.Application.DTOs.Reservas;
using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Application.Validaciones;

/// <summary>
/// Reglas de entrada de la pantalla "Nueva reserva". La disponibilidad efectiva no se
/// valida aquí: se comprueba de forma transaccional en el servicio (RNF03).
/// </summary>
public sealed class ValidadorCrearReserva : AbstractValidator<CrearReservaDto>
{
    public ValidadorCrearReserva()
    {
        RuleFor(r => r)
            .Must(r => r.ClienteId is > 0 || r.ClienteNuevo is not null)
            .WithMessage("Debe seleccionar un cliente existente o registrar uno nuevo.");

        RuleFor(r => r.ClienteNuevo!)
            .SetValidator(new ValidadorCrearCliente())
            .When(r => r.ClienteNuevo is not null);

        RuleFor(r => r.HabitacionId)
            .GreaterThan(0).WithMessage("Debe seleccionar una habitación.");

        RuleFor(r => r.FechaEntrada)
            .NotEmpty().WithMessage("La fecha de entrada es obligatoria.");

        RuleFor(r => r.FechaSalida)
            .NotEmpty().WithMessage("La fecha de salida es obligatoria.")
            .GreaterThanOrEqualTo(r => r.FechaEntrada)
            .WithMessage("La fecha de salida no puede ser anterior a la de entrada.");

        RuleFor(r => r.CantidadHuespedes)
            .GreaterThan(0).WithMessage("Debe indicar al menos un huésped.")
            .LessThanOrEqualTo(20).WithMessage("La cantidad de huéspedes indicada no es válida.");

        RuleFor(r => r.CanalOrigen)
            .NotEmpty().WithMessage("El canal de origen es obligatorio.")
            .Must(SerCanalValido)
            .WithMessage("El canal de origen seleccionado no es válido.");

        RuleFor(r => r.Observaciones)
            .MaximumLength(500).WithMessage("Las observaciones no pueden superar los 500 caracteres.")
            .When(r => !string.IsNullOrWhiteSpace(r.Observaciones));
    }

    private static bool SerCanalValido(string canal) =>
        Enum.TryParse<CanalOrigenReserva>(canal, ignoreCase: true, out _);
}

public sealed class ValidadorActualizarReserva : AbstractValidator<ActualizarReservaDto>
{
    public ValidadorActualizarReserva()
    {
        RuleFor(r => r.HabitacionId)
            .GreaterThan(0).WithMessage("Debe seleccionar una habitación.");

        RuleFor(r => r.FechaSalida)
            .GreaterThanOrEqualTo(r => r.FechaEntrada)
            .WithMessage("La fecha de salida no puede ser anterior a la de entrada.");

        RuleFor(r => r.CantidadHuespedes)
            .GreaterThan(0).WithMessage("Debe indicar al menos un huésped.")
            .LessThanOrEqualTo(20).WithMessage("La cantidad de huéspedes indicada no es válida.");

        RuleFor(r => r.CanalOrigen)
            .NotEmpty().WithMessage("El canal de origen es obligatorio.")
            .Must(c => Enum.TryParse<CanalOrigenReserva>(c, ignoreCase: true, out _))
            .WithMessage("El canal de origen seleccionado no es válido.");

        RuleFor(r => r.Observaciones)
            .MaximumLength(500).WithMessage("Las observaciones no pueden superar los 500 caracteres.")
            .When(r => !string.IsNullOrWhiteSpace(r.Observaciones));
    }
}

public sealed class ValidadorCancelarReserva : AbstractValidator<CancelarReservaDto>
{
    public ValidadorCancelarReserva()
    {
        RuleFor(r => r.Motivo)
            .NotEmpty().WithMessage("Debe indicar el motivo de la cancelación.")
            .MaximumLength(300).WithMessage("El motivo no puede superar los 300 caracteres.");
    }
}
