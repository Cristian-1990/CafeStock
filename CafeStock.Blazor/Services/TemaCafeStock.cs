using MudBlazor;

namespace CafeStock.Blazor.Services;

/// <summary>Tema MudBlazor de la app (paleta café), compartido por MainLayout y LoginLayout.</summary>
public static class TemaCafeStock
{
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
}
