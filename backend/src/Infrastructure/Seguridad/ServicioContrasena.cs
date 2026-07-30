using System.Security.Cryptography;
using HotelParadiseResort.Application.Abstracciones;

namespace HotelParadiseResort.Infrastructure.Seguridad;

/// <summary>
/// Resguardo de contraseñas con PBKDF2-HMAC-SHA256 y sal aleatoria por credencial
/// (RNF01, buenas prácticas OWASP).
///
/// El formato almacenado es <c>iteraciones.sal.hash</c> en Base64, de modo que el
/// número de iteraciones queda registrado junto al hash y puede elevarse en el futuro
/// sin invalidar las contraseñas existentes.
/// </summary>
public sealed class ServicioContrasena : IServicioContrasena
{
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;
    private const int IteracionesActuales = 210_000;

    private static readonly HashAlgorithmName Algoritmo = HashAlgorithmName.SHA256;

    public string Hashear(string contrasena)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contrasena);

        var sal = RandomNumberGenerator.GetBytes(TamanoSal);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            contrasena, sal, IteracionesActuales, Algoritmo, TamanoHash);

        return $"{IteracionesActuales}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string contrasena, string hashAlmacenado)
    {
        if (string.IsNullOrWhiteSpace(contrasena) || string.IsNullOrWhiteSpace(hashAlmacenado))
        {
            return false;
        }

        var partes = hashAlmacenado.Split('.', 3);

        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteraciones))
        {
            return false;
        }

        try
        {
            var sal = Convert.FromBase64String(partes[1]);
            var hashEsperado = Convert.FromBase64String(partes[2]);

            var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
                contrasena, sal, iteraciones, Algoritmo, hashEsperado.Length);

            // Comparación en tiempo constante: evita filtrar información por el tiempo
            // que tarda en detectarse la diferencia.
            return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
