namespace HotelParadiseResort.Shared.Excepciones;

/// <summary>
/// Se lanza cuando una invariante del dominio resulta violada en un punto donde el
/// flujo no puede continuar. Los fallos de negocio previsibles se comunican mediante
/// <see cref="Resultados.Resultado"/>; esta excepción queda para lo verdaderamente excepcional.
/// </summary>
public class ExcepcionReglaNegocio : Exception
{
    public ExcepcionReglaNegocio(string mensaje)
        : base(mensaje)
    {
    }

    public ExcepcionReglaNegocio(string mensaje, Exception excepcionInterna)
        : base(mensaje, excepcionInterna)
    {
    }
}
