using FluentValidation;
using HotelParadiseResort.Application.DTOs.Clientes;

namespace HotelParadiseResort.Application.Validaciones;

/// <summary>
/// Reglas de entrada del registro de huéspedes. Los mensajes están redactados para el
/// usuario final: nunca exponen detalles técnicos.
/// </summary>
public sealed class ValidadorCrearCliente : AbstractValidator<CrearClienteDto>
{
    public ValidadorCrearCliente()
    {
        RuleFor(c => c.Identificacion)
            .NotEmpty().WithMessage("La identificación es obligatoria.")
            .MaximumLength(30).WithMessage("La identificación no puede superar los 30 caracteres.");

        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(80).WithMessage("El nombre no puede superar los 80 caracteres.");

        RuleFor(c => c.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(120).WithMessage("Los apellidos no pueden superar los 120 caracteres.");

        RuleFor(c => c.Correo)
            .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.")
            .MaximumLength(150).WithMessage("El correo no puede superar los 150 caracteres.")
            .When(c => !string.IsNullOrWhiteSpace(c.Correo));

        RuleFor(c => c.Telefono)
            .MaximumLength(30).WithMessage("El teléfono no puede superar los 30 caracteres.")
            .When(c => !string.IsNullOrWhiteSpace(c.Telefono));

        RuleFor(c => c.Nacionalidad)
            .MaximumLength(80).WithMessage("La nacionalidad no puede superar los 80 caracteres.")
            .When(c => !string.IsNullOrWhiteSpace(c.Nacionalidad));

        RuleFor(c => c.FechaNacimiento)
            .LessThan(DateTime.UtcNow.Date)
            .WithMessage("La fecha de nacimiento debe ser anterior al día de hoy.")
            .When(c => c.FechaNacimiento.HasValue);
    }
}

public sealed class ValidadorActualizarCliente : AbstractValidator<ActualizarClienteDto>
{
    public ValidadorActualizarCliente()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(80).WithMessage("El nombre no puede superar los 80 caracteres.");

        RuleFor(c => c.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(120).WithMessage("Los apellidos no pueden superar los 120 caracteres.");

        RuleFor(c => c.Correo)
            .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.")
            .MaximumLength(150).WithMessage("El correo no puede superar los 150 caracteres.")
            .When(c => !string.IsNullOrWhiteSpace(c.Correo));

        RuleFor(c => c.Telefono)
            .MaximumLength(30).WithMessage("El teléfono no puede superar los 30 caracteres.")
            .When(c => !string.IsNullOrWhiteSpace(c.Telefono));

        RuleFor(c => c.FechaNacimiento)
            .LessThan(DateTime.UtcNow.Date)
            .WithMessage("La fecha de nacimiento debe ser anterior al día de hoy.")
            .When(c => c.FechaNacimiento.HasValue);
    }
}
