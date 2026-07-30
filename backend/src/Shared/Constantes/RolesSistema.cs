namespace HotelParadiseResort.Shared.Constantes;

/// <summary>
/// Roles definidos en el diagrama de casos de uso: Administrador y Recepcionista,
/// ambos especializaciones del actor Empleado. El Cliente es una entidad de negocio
/// gestionada por el sistema, no un usuario que inicia sesión.
/// </summary>
public static class RolesSistema
{
    public const string Administrador = nameof(Administrador);

    public const string Recepcionista = nameof(Recepcionista);

    /// <summary>Política que admite a cualquier empleado autenticado.</summary>
    public const string Empleado = $"{Administrador},{Recepcionista}";
}
