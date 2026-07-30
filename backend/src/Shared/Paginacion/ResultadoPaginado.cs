namespace HotelParadiseResort.Shared.Paginacion;

/// <summary>
/// Página de resultados junto con los metadatos que la tabla del frontend necesita
/// para renderizar su paginador sin realizar consultas adicionales.
/// </summary>
public sealed class ResultadoPaginado<T>
{
    public ResultadoPaginado(IReadOnlyList<T> elementos, int totalRegistros, int pagina, int tamanoPagina)
    {
        Elementos = elementos;
        TotalRegistros = totalRegistros;
        Pagina = pagina;
        TamanoPagina = tamanoPagina;
    }

    public IReadOnlyList<T> Elementos { get; }

    public int TotalRegistros { get; }

    public int Pagina { get; }

    public int TamanoPagina { get; }

    public int TotalPaginas => TamanoPagina <= 0 ? 0 : (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina);

    public bool TienePaginaAnterior => Pagina > 1;

    public bool TienePaginaSiguiente => Pagina < TotalPaginas;
}
