using System.Globalization;
using System.Text;

namespace CafeStock.Back.Models;

/// <summary>
/// Orden alfabético "de persona": sin distinguir mayúsculas ni tildes ("Álvaro" va con la A, no
/// después de la Z). Se calcula a mano en vez de usar la cultura del sistema porque en la
/// Raspberry Pi la globalización puede no tener es-ES (o estar en modo invariante, que ordena por
/// código de carácter); así el resultado es el mismo en todas las máquinas.
/// </summary>
public static class OrdenAlfabetico
{
    public static string Clave(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        var descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    public static IOrderedEnumerable<T> OrdenarAlfabeticamente<T>(this IEnumerable<T> origen, Func<T, string?> texto) =>
        origen.OrderBy(x => Clave(texto(x)), StringComparer.Ordinal);

    public static IOrderedEnumerable<T> LuegoAlfabeticamente<T>(this IOrderedEnumerable<T> origen, Func<T, string?> texto) =>
        origen.ThenBy(x => Clave(texto(x)), StringComparer.Ordinal);
}
