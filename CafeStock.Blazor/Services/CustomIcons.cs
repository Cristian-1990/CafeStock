namespace CafeStock.Blazor.Services;

/// <summary>
/// Iconos que no existen en el set de Material Icons que trae MudBlazor
/// (<see cref="MudBlazor.Icons.Material"/>). Cada valor es el contenido SVG
/// interno que MudBlazor inyecta dentro de su &lt;svg viewBox="0 0 24 24"&gt;.
/// </summary>
public static class CustomIcons
{
    /// <summary>
    /// Grano de café: óvalo con la hendidura central, en trazo para que se
    /// lea igual sobre cualquier fondo (menú, botones...).
    /// </summary>
    public const string CoffeeBean =
        """
        <ellipse cx="12" cy="12" rx="9" ry="6" fill="none" stroke="currentColor" stroke-width="1.5" transform="rotate(-45 12 12)"/>
        <path d="M4,12 C8,8 8,16 12,12 C16,8 16,16 20,12" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" transform="rotate(-45 12 12)"/>
        """;
}
