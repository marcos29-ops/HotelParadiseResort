using HotelParadiseResort.Domain.Comun;
using HotelParadiseResort.Domain.Enumeraciones;

namespace HotelParadiseResort.Domain.Entidades;

/// <summary>
/// Personal del hotel con acceso al sistema. Corresponde a la clase Usuario del
/// diagrama de clases y al actor Empleado del diagrama de casos de uso.
/// </summary>
public class Usuario : EntidadBase
{
    public required string Nombre { get; set; }

    public required string NombreUsuario { get; set; }

    /// <summary>Hash de la contraseña. Nunca se almacena el valor en claro.</summary>
    public required string ContrasenaHash { get; set; }

    public required string Correo { get; set; }

    public RolUsuario Rol { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime? UltimoAcceso { get; set; }

    /// <summary>Reservas gestionadas por este usuario (Usuario 1 → 0..* Reserva).</summary>
    public ICollection<Reserva> ReservasGestionadas { get; set; } = new List<Reserva>();

    public bool EsAdministrador => Rol == RolUsuario.Administrador;

    public void RegistrarAcceso(DateTime momento) => UltimoAcceso = momento;
}
