namespace CafeStock.Back.Models;

/// <summary>
/// Colores internos de los proveedores. Diez tonos bien distinguibles entre sí y con buen contraste
/// sobre el crema de la app (todos son oscuros/medios: sirven de fondo con texto blanco y de
/// borde/etiqueta). Los proveedores existentes los reciben en la migración
/// AddColorProveedorYFichaCafe con el mismo orden (por Id); los nuevos, el primero que no esté usado.
/// </summary>
public static class PaletaProveedores
{
    public static readonly IReadOnlyList<string> Colores =
    [
        "#A8481F", // terracota
        "#3F6B2E", // verde hoja
        "#1F5F8B", // azul
        "#8E4585", // ciruela
        "#C17817", // dorado
        "#2E7D7A", // turquesa
        "#B23A48", // granate
        "#5D6D2B", // oliva
        "#6D4C41", // marrón café
        "#4A5FA5", // índigo
    ];

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
}
