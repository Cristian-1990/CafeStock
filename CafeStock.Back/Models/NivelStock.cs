namespace CafeStock.Back.Models;

/// <summary>
/// Nivel de llenado de un producto (stock actual sobre su máximo) en 5 tramos, para colorear las
/// barras de progreso: del verde llamativo (50 % o más) bajando por los colores cálidos
/// (amarillo, naranja, naranja fuerte) hasta el rojo del último tramo.
/// Solo presentación: no interviene en ninguna regla de negocio (esas siguen usando
/// <see cref="Producto.CantidadAComprar"/>).
/// </summary>
public static class NivelStock
{
    /// <summary>Porcentaje 0-100 de llenado. Sin máximo definido cuenta como 0.</summary>
    public static int Porcentaje(int stockActual, int stockMaximo)
    {
        if (stockMaximo <= 0) return 0;
        return Math.Clamp(stockActual * 100 / stockMaximo, 0, 100);
    }

    /// <summary>
    /// Tramo del 1 (el peor, rojo) al 5 (el mejor, verde):
    /// 50-100 % → 5 · 38-49 % → 4 · 25-37 % → 3 · 13-24 % → 2 · 0-12 % → 1.
    /// </summary>
    public static int Tramo(int porcentaje) => porcentaje switch
    {
        >= 50 => 5,
        >= 38 => 4,
        >= 25 => 3,
        >= 13 => 2,
        _ => 1
    };

    public static int Tramo(int stockActual, int stockMaximo) => Tramo(Porcentaje(stockActual, stockMaximo));

    /// <summary>Clase CSS del medidor ("n1".."n5"); los colores viven en app.css (--nivel-N).</summary>
    public static string Clase(int stockActual, int stockMaximo) => $"n{Tramo(stockActual, stockMaximo)}";
}
