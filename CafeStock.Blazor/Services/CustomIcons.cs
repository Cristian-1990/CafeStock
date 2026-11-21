namespace CafeStock.Blazor.Services;

/// <summary>
/// Iconos que no existen en el set de Material Icons que trae MudBlazor
/// (<see cref="MudBlazor.Icons.Material"/>). Cada valor es el contenido SVG
/// interno que MudBlazor inyecta dentro de su &lt;svg viewBox="0 0 24 24"&gt;.
/// Los de trazo usan 2 px de grosor y ocupan casi toda la rejilla para pesar lo mismo que los
/// iconos rellenos de Material (Inicio, Informes) con los que conviven en el menú.
/// </summary>
public static class CustomIcons
{
    /// <summary>
    /// Caja abierta con una flecha que entra en ella: "Recepcionar" (lo que llega del proveedor).
    /// </summary>
    public const string DeliveryBox =
        """
        <path d="M3,11 L3,21 L21,21 L21,11" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round" stroke-linecap="round"/>
        <path d="M3,11 L6,7.5 M21,11 L18,7.5" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M12,2 L12,15" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M7.7,10.7 L12,15 L16.3,10.7" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        """;

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
