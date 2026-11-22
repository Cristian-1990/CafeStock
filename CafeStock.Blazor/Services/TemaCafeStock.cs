using MudBlazor;

namespace CafeStock.Blazor.Services;

/// <summary>
/// Temas MudBlazor de la app, compartidos por MainLayout y LoginLayout. Cada tema tiene su pareja en
/// app.css (<c>:root[data-tema="..."]</c>), que redefine las variables CSS; aquí viven solo los colores
/// que MudBlazor pinta por su cuenta (botones, cabecera, menú, tablas...).
/// </summary>
public static class TemaCafeStock
{
    public const string IdCafe = "cafe";
    public const string IdBlancoNegro = "blanconegro";

    public static readonly MudTheme Tema = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#6D4C41",
            Secondary = "#8D6E63",
            AppbarBackground = "#6D4C41",
            Background = "#EFE6DA",
            Surface = "#FFFBF5",
            DrawerBackground = "#B8956A",
            DrawerText = "#3E2723",
            DrawerIcon = "#3E2723",
            TextPrimary = "#2C2C2A",
            Error = "#993C1D",
            Dark = "#3E2723"
        }
    };

    /// <summary>Segundo tema: negro, blanco y crema. Sans (Inter) en vez de la serif de los títulos.</summary>
    public static readonly MudTheme TemaBlancoNegro = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#111111",
            Secondary = "#4A4A4A",
            AppbarBackground = "#111111",
            AppbarText = "#FFFFFF",
            Background = "#FFFFFF",
            Surface = "#FFFFFF",
            DrawerBackground = "#F4F1EA",
            DrawerText = "#111111",
            DrawerIcon = "#111111",
            TextPrimary = "#111111",
            TextSecondary = "#4A4A4A",
            Error = "#A32D2D",
            Dark = "#111111",
            ActionDefault = "#111111",
            LinesDefault = "#DDD6C5",
            TableLines = "#DDD6C5"
        }
    };

    public static MudTheme Por(string id) => id == IdBlancoNegro ? TemaBlancoNegro : Tema;
}
