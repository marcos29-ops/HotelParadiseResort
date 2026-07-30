namespace HotelParadiseResort.Shared.Resultados;

/// <summary>
/// Representa el desenlace de una operación de negocio sin recurrir a excepciones
/// para el flujo de control esperado (por ejemplo, una habitación no disponible).
/// </summary>
public class Resultado
{
    protected Resultado(bool esExitoso, string? error, TipoError tipoError)
    {
        EsExitoso = esExitoso;
        Error = error;
        TipoError = tipoError;
    }

    public bool EsExitoso { get; }

    public bool EsFallido => !EsExitoso;

    public string? Error { get; }

    public TipoError TipoError { get; }

    public static Resultado Exitoso() => new(true, null, TipoError.Ninguno);

    public static Resultado Fallo(string error, TipoError tipoError = TipoError.Validacion) =>
        new(false, error, tipoError);

    public static Resultado<T> Exitoso<T>(T valor) => new(valor, true, null, TipoError.Ninguno);

    public static Resultado<T> Fallo<T>(string error, TipoError tipoError = TipoError.Validacion) =>
        new(default, false, error, tipoError);
}

/// <summary>
/// Resultado de una operación que devuelve un valor cuando finaliza correctamente.
/// </summary>
public sealed class Resultado<T> : Resultado
{
    internal Resultado(T? valor, bool esExitoso, string? error, TipoError tipoError)
        : base(esExitoso, error, tipoError)
    {
        Valor = valor;
    }

    public T? Valor { get; }
}
