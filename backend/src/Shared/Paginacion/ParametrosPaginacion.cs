namespace HotelParadiseResort.Shared.Paginacion;

/// <summary>
/// Parámetros de paginación y ordenamiento comunes a todas las consultas de listado.
/// Acota el tamaño de página para impedir que una petición degrade el tiempo de
/// respuesta exigido por RNF02.
/// </summary>
public class ParametrosPaginacion
{
    public const int TamanoPaginaMaximo = 100;

    private int _pagina = 1;
    private int _tamanoPagina = 10;

    public int Pagina
    {
        get => _pagina;
        set => _pagina = value < 1 ? 1 : value;
    }

    public int TamanoPagina
    {
        get => _tamanoPagina;
        set => _tamanoPagina = value switch
        {
            < 1 => 10,
            > TamanoPaginaMaximo => TamanoPaginaMaximo,
            _ => value
        };
    }

    /// <summary>Texto libre para filtrar el listado. Opcional.</summary>
    public string? Busqueda { get; set; }

    /// <summary>Nombre del campo por el cual ordenar. Opcional.</summary>
    public string? OrdenarPor { get; set; }

    /// <summary>Indica si el ordenamiento es descendente.</summary>
    public bool Descendente { get; set; }

    public int RegistrosOmitidos => (Pagina - 1) * TamanoPagina;
}
