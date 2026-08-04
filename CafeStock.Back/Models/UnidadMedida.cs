namespace CafeStock.Back.Models;

/// <summary>
/// Unidad en la que se mide el stock de un producto.
/// SinEspecificar va primero (valor 0) para que los productos existentes,
/// guardados antes de que este campo existiera, no hereden por error la
/// primera unidad "real" del enum al persistirse como número en SQLite.
/// </summary>
public enum UnidadMedida
{
    SinEspecificar,
    Kg,
    Gramos,
    Litros,
    Unidades,
    Bote,
    Bolsa
}
