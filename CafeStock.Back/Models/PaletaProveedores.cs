namespace CafeStock.Back.Models;

/// <summary>
/// Colores internos de los proveedores. Diez tonos vivos y bien distinguibles entre sí. La paleta
/// NO incluye rojos ni marrones a propósito: esos están reservados para los proveedores cuyo color de
/// marca es ese (Coca-Cola, Alcampo, Puchero; ver la migración RecolorProveedores), de modo que un
/// proveedor nuevo nunca se parezca a ellos.
/// Los proveedores existentes reciben estos colores por orden de Id en las migraciones
/// AddColorProveedorYFichaCafe y RecolorProveedores; los nuevos, el primero que no esté usado.
/// </summary>
public static class PaletaProveedores
{
    public static readonly IReadOnlyList<string> Colores =
    [
        "#E8590C", // naranja
        "#2F9E44", // verde
        "#1C7ED6", // azul
        "#AE3EC9", // violeta
        "#F59F00", // ámbar
        "#0CA678", // turquesa
        "#E64980", // rosa
        "#74B816", // lima
        "#7048E8", // índigo
        "#15AABF", // cian
    ];

    private const string TextoClaro = "#FFFFFF";
    private const string TextoOscuro = "#3E2723";

    /// <summary>El primer color de la paleta que ningún proveedor usa; si se agotan, vuelve a empezar.</summary>
    public static string Siguiente(IEnumerable<string> usados)
    {
        var enUso = usados
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .ToHashSet();

        var libre = Colores.FirstOrDefault(c => !enUso.Contains(c.ToUpperInvariant()));
        return libre ?? Colores[enUso.Count % Colores.Count];
    }

    /// <summary>
    /// Color a mostrar para un proveedor: el suyo si es un hexadecimal válido y, si no, uno de la
    /// paleta según su Id (estable entre recargas). Así las pantallas nunca se quedan sin color.
    /// </summary>
    public static string ColorDe(Proveedor? proveedor)
    {
        if (proveedor is null) return "#6D4C41";
        return EsHexValido(proveedor.Color) ? proveedor.Color.Trim() : Colores[Math.Abs(proveedor.Id) % Colores.Count];
    }

    public static bool EsHexValido(string? valor) =>
        !string.IsNullOrWhiteSpace(valor)
        && valor.Trim().Length == 7
        && valor.Trim()[0] == '#'
        && valor.Trim()[1..].All(Uri.IsHexDigit);

    /// <summary>
    /// Color de texto (blanco o espresso) con el que mejor se lee sobre un fondo de este color: el de
    /// mayor contraste según la fórmula de WCAG. Para poner el nombre del proveedor sobre su color.
    /// </summary>
    public static string TextoSobre(string? fondoHex)
    {
        if (!EsHexValido(fondoHex)) return TextoClaro;

        var luzFondo = LuminanciaRelativa(fondoHex!.Trim());
        var contrasteConBlanco = (1.05) / (luzFondo + 0.05);
        var contrasteConOscuro = (luzFondo + 0.05) / (LuminanciaRelativa(TextoOscuro) + 0.05);
        return contrasteConBlanco >= contrasteConOscuro ? TextoClaro : TextoOscuro;
    }

    /// <summary>
    /// Variables CSS para pintar el nombre de un proveedor sobre su color (clase global
    /// .etiqueta-proveedor en app.css): "--proveedor-color" (fondo) y "--proveedor-texto" (el de mayor
    /// contraste). Se usa en el atributo style del elemento.
    /// </summary>
    public static string VariablesCss(Proveedor? proveedor) => VariablesCss(ColorDe(proveedor));

    public static string VariablesCss(string? colorHex)
    {
        var color = EsHexValido(colorHex) ? colorHex!.Trim() : "#6D4C41";
        return $"--proveedor-color:{color};--proveedor-texto:{TextoSobre(color)}";
    }

    private static double LuminanciaRelativa(string hex)
    {
        static double Canal(string h, int inicio)
        {
            var c = Convert.ToInt32(h.Substring(inicio, 2), 16) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Canal(hex, 1) + 0.7152 * Canal(hex, 3) + 0.0722 * Canal(hex, 5);
    }
}
