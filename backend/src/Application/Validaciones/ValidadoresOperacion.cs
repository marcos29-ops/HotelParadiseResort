using FluentValidation;
using HotelParadiseResort.Application.DTOs.Autenticacion;
using HotelParadiseResort.Application.DTOs.Estadias;
using HotelParadiseResort.Application.DTOs.Facturacion;
using HotelParadiseResort.Application.DTOs.Habitaciones;
using HotelParadiseResort.Application.DTOs.Usuarios;
using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Application.Validaciones;

public sealed class ValidadorInicioSesion : AbstractValidator<SolicitudInicioSesionDto>
{
    public ValidadorInicioSesion()
    {
        RuleFor(s => s.NombreUsuario)
            .NotEmpty().WithMessage("El usuario es obligatorio.")
            .MaximumLength(50).WithMessage("El usuario no puede superar los 50 caracteres.");

        RuleFor(s => s.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}

/// <summary>
/// Política de contraseñas del sistema (RNF01). Se aplica tanto al alta de usuarios
/// como al cambio y restablecimiento de credenciales.
/// </summary>
public static class ReglasContrasena
{
    public const int LongitudMinima = 8;

    public static IRuleBuilderOptions<T, string> AplicarPolitica<T>(
        this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(LongitudMinima)
                .WithMessage($"La contraseña debe tener al menos {LongitudMinima} caracteres.")
            .MaximumLength(128).WithMessage("La contraseña no puede superar los 128 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe incluir al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe incluir al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe incluir al menos un número.");
}

public sealed class ValidadorCambioContrasena : AbstractValidator<CambioContrasenaDto>
{
    public ValidadorCambioContrasena()
    {
        RuleFor(c => c.ContrasenaActual)
            .NotEmpty().WithMessage("Debe indicar su contraseña actual.");

        RuleFor(c => c.ContrasenaNueva)
            .AplicarPolitica()
            .NotEqual(c => c.ContrasenaActual)
                .WithMessage("La contraseña nueva debe ser distinta de la actual.");
    }
}

public sealed class ValidadorCrearUsuario : AbstractValidator<CrearUsuarioDto>
{
    public ValidadorCrearUsuario()
    {
        RuleFor(u => u.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(120).WithMessage("El nombre no puede superar los 120 caracteres.");

        RuleFor(u => u.NombreUsuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MinimumLength(4).WithMessage("El nombre de usuario debe tener al menos 4 caracteres.")
            .MaximumLength(50).WithMessage("El nombre de usuario no puede superar los 50 caracteres.")
            .Matches("^[a-zA-Z0-9._-]+$")
                .WithMessage("El nombre de usuario solo admite letras, números, punto, guion y guion bajo.");

        RuleFor(u => u.Correo)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.")
            .MaximumLength(150).WithMessage("El correo no puede superar los 150 caracteres.");

        RuleFor(u => u.Contrasena).AplicarPolitica();

        RuleFor(u => u.Rol)
            .NotEmpty().WithMessage("El rol es obligatorio.")
            .Must(r => Enum.TryParse<RolUsuario>(r, ignoreCase: true, out _))
            .WithMessage("El rol seleccionado no es válido.");
    }
}

public sealed class ValidadorActualizarUsuario : AbstractValidator<ActualizarUsuarioDto>
{
    public ValidadorActualizarUsuario()
    {
        RuleFor(u => u.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(120).WithMessage("El nombre no puede superar los 120 caracteres.");

        RuleFor(u => u.Correo)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.");

        RuleFor(u => u.Rol)
            .NotEmpty().WithMessage("El rol es obligatorio.")
            .Must(r => Enum.TryParse<RolUsuario>(r, ignoreCase: true, out _))
            .WithMessage("El rol seleccionado no es válido.");
    }
}

public sealed class ValidadorRestablecerContrasena : AbstractValidator<RestablecerContrasenaDto>
{
    public ValidadorRestablecerContrasena() => RuleFor(r => r.ContrasenaNueva).AplicarPolitica();
}

public sealed class ValidadorCrearHabitacion : AbstractValidator<CrearHabitacionDto>
{
    public ValidadorCrearHabitacion()
    {
        RuleFor(h => h.Numero)
            .NotEmpty().WithMessage("El número de habitación es obligatorio.")
            .MaximumLength(10).WithMessage("El número no puede superar los 10 caracteres.");

        RuleFor(h => h.Piso)
            .InclusiveBetween(0, 100).WithMessage("El piso indicado no es válido.");

        RuleFor(h => h.TipoHabitacionId)
            .GreaterThan(0).WithMessage("Debe seleccionar el tipo de habitación.");
    }
}

public sealed class ValidadorActualizarHabitacion : AbstractValidator<ActualizarHabitacionDto>
{
    public ValidadorActualizarHabitacion()
    {
        RuleFor(h => h.Numero)
            .NotEmpty().WithMessage("El número de habitación es obligatorio.")
            .MaximumLength(10).WithMessage("El número no puede superar los 10 caracteres.");

        RuleFor(h => h.Piso)
            .InclusiveBetween(0, 100).WithMessage("El piso indicado no es válido.");

        RuleFor(h => h.TipoHabitacionId)
            .GreaterThan(0).WithMessage("Debe seleccionar el tipo de habitación.");
    }
}

public sealed class ValidadorCambiarEstadoHabitacion : AbstractValidator<CambiarEstadoHabitacionDto>
{
    public ValidadorCambiarEstadoHabitacion() =>
        RuleFor(h => h.NuevoEstado)
            .NotEmpty().WithMessage("Debe indicar el nuevo estado.")
            .Must(e => Enum.TryParse<TipoEstadoHabitacion>(e, ignoreCase: true, out _))
            .WithMessage("El estado seleccionado no es válido.");
}

public sealed class ValidadorCrearTipoHabitacion : AbstractValidator<CrearTipoHabitacionDto>
{
    public ValidadorCrearTipoHabitacion()
    {
        RuleFor(t => t.Nombre)
            .NotEmpty().WithMessage("El nombre del tipo es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(t => t.TarifaBasePorNoche)
            .GreaterThan(0).WithMessage("La tarifa por noche debe ser mayor que cero.")
            .LessThanOrEqualTo(1_000_000).WithMessage("La tarifa indicada no es válida.");

        RuleFor(t => t.CapacidadMaxima)
            .InclusiveBetween(1, 20).WithMessage("La capacidad debe estar entre 1 y 20 huéspedes.");
    }
}

public sealed class ValidadorActualizarTipoHabitacion : AbstractValidator<ActualizarTipoHabitacionDto>
{
    public ValidadorActualizarTipoHabitacion()
    {
        RuleFor(t => t.Nombre)
            .NotEmpty().WithMessage("El nombre del tipo es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(t => t.TarifaBasePorNoche)
            .GreaterThan(0).WithMessage("La tarifa por noche debe ser mayor que cero.");

        RuleFor(t => t.CapacidadMaxima)
            .InclusiveBetween(1, 20).WithMessage("La capacidad debe estar entre 1 y 20 huéspedes.");
    }
}

public sealed class ValidadorCheckIn : AbstractValidator<RegistrarCheckInDto>
{
    public ValidadorCheckIn()
    {
        RuleFor(c => c.ReservaId)
            .GreaterThan(0).WithMessage("Debe indicar la reserva del huésped.");

        RuleFor(c => c.CantidadHuespedes)
            .GreaterThan(0).WithMessage("Debe indicar al menos un huésped.")
            .LessThanOrEqualTo(20).WithMessage("La cantidad de huéspedes indicada no es válida.");

        RuleFor(c => c.Observaciones)
            .MaximumLength(500).WithMessage("Las observaciones no pueden superar los 500 caracteres.")
            .When(c => !string.IsNullOrWhiteSpace(c.Observaciones));
    }
}

public sealed class ValidadorCheckOut : AbstractValidator<RegistrarCheckOutDto>
{
    public ValidadorCheckOut() =>
        RuleFor(c => c.Observaciones)
            .MaximumLength(500).WithMessage("Las observaciones no pueden superar los 500 caracteres.")
            .When(c => !string.IsNullOrWhiteSpace(c.Observaciones));
}

public sealed class ValidadorRegistrarConsumo : AbstractValidator<RegistrarConsumoDto>
{
    public ValidadorRegistrarConsumo()
    {
        RuleFor(c => c.ServicioAdicionalId)
            .GreaterThan(0).WithMessage("Debe seleccionar el servicio consumido.");

        RuleFor(c => c.Descripcion)
            .NotEmpty().WithMessage("La descripción del consumo es obligatoria.")
            .MaximumLength(300).WithMessage("La descripción no puede superar los 300 caracteres.");

        RuleFor(c => c.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.")
            .LessThanOrEqualTo(999).WithMessage("La cantidad indicada no es válida.");

        RuleFor(c => c.PrecioUnitario)
            .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.")
            .When(c => c.PrecioUnitario.HasValue);
    }
}

public sealed class ValidadorCrearServicioAdicional : AbstractValidator<CrearServicioAdicionalDto>
{
    public ValidadorCrearServicioAdicional()
    {
        RuleFor(s => s.Nombre)
            .NotEmpty().WithMessage("El nombre del servicio es obligatorio.")
            .MaximumLength(120).WithMessage("El nombre no puede superar los 120 caracteres.");

        RuleFor(s => s.Tipo)
            .NotEmpty().WithMessage("El tipo de servicio es obligatorio.")
            .Must(t => Enum.TryParse<TipoServicioAdicional>(t, ignoreCase: true, out _))
            .WithMessage("El tipo de servicio seleccionado no es válido.");

        RuleFor(s => s.PrecioBase)
            .GreaterThanOrEqualTo(0).WithMessage("El precio base no puede ser negativo.");
    }
}

public sealed class ValidadorActualizarServicioAdicional : AbstractValidator<ActualizarServicioAdicionalDto>
{
    public ValidadorActualizarServicioAdicional()
    {
        RuleFor(s => s.Nombre)
            .NotEmpty().WithMessage("El nombre del servicio es obligatorio.")
            .MaximumLength(120).WithMessage("El nombre no puede superar los 120 caracteres.");

        RuleFor(s => s.Tipo)
            .NotEmpty().WithMessage("El tipo de servicio es obligatorio.")
            .Must(t => Enum.TryParse<TipoServicioAdicional>(t, ignoreCase: true, out _))
            .WithMessage("El tipo de servicio seleccionado no es válido.");

        RuleFor(s => s.PrecioBase)
            .GreaterThanOrEqualTo(0).WithMessage("El precio base no puede ser negativo.");
    }
}

public sealed class ValidadorGenerarFactura : AbstractValidator<GenerarFacturaDto>
{
    public ValidadorGenerarFactura()
    {
        RuleFor(f => f.EstadiaId)
            .GreaterThan(0).WithMessage("Debe indicar la estadía a facturar.");

        RuleFor(f => f.Descuento)
            .GreaterThanOrEqualTo(0).WithMessage("El descuento no puede ser negativo.")
            .When(f => f.Descuento.HasValue);

        RuleFor(f => f.JustificacionDescuento)
            .NotEmpty().WithMessage("Todo descuento debe indicar su justificación.")
            .MaximumLength(300).WithMessage("La justificación no puede superar los 300 caracteres.")
            .When(f => f.Descuento is > 0);

        RuleFor(f => f.MetodoPago)
            .Must(m => Enum.TryParse<MetodoPago>(m, ignoreCase: true, out _))
            .WithMessage("El método de pago seleccionado no es válido.")
            .When(f => !string.IsNullOrWhiteSpace(f.MetodoPago));

        RuleFor(f => f.MetodoPago)
            .NotEmpty().WithMessage("Debe indicar el método de pago para registrar el cobro.")
            .When(f => f.RegistrarPagoInmediato);
    }
}

public sealed class ValidadorAplicarDescuento : AbstractValidator<AplicarDescuentoDto>
{
    public ValidadorAplicarDescuento()
    {
        RuleFor(d => d.Monto)
            .GreaterThanOrEqualTo(0).WithMessage("El descuento no puede ser negativo.");

        RuleFor(d => d.Justificacion)
            .NotEmpty().WithMessage("Todo descuento debe indicar su justificación.")
            .MaximumLength(300).WithMessage("La justificación no puede superar los 300 caracteres.");
    }
}

public sealed class ValidadorRegistrarPago : AbstractValidator<RegistrarPagoDto>
{
    public ValidadorRegistrarPago() =>
        RuleFor(p => p.MetodoPago)
            .NotEmpty().WithMessage("El método de pago es obligatorio.")
            .Must(m => Enum.TryParse<MetodoPago>(m, ignoreCase: true, out _))
            .WithMessage("El método de pago seleccionado no es válido.");
}
