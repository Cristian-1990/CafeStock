namespace CafeStock.Back.Models;

/// <summary>
/// Grupo de café al que pertenece un producto en el tablero de la pantalla Café: el de la casa (siempre el
/// mismo, hoy el Brasil de 2 kg), el especial (el de la semana, que va rotando) y el descafeinado (siempre
/// el mismo). Un campo explícito en vez de deducirlo del nombre del producto. SinGrupo va primero (valor 0)
/// para que los productos que no son café, y los ya existentes, no hereden por error un grupo real al
/// persistirse como número en SQLite; los valores nuevos se añaden siempre al final.
/// </summary>
public enum GrupoCafe
{
    SinGrupo,
    Casa,
    Especial,
    Descafeinado
}
