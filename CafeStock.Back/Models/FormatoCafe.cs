namespace CafeStock.Back.Models;

/// <summary>
/// Formato de un café del tablero: paquete (el grande, de 1 o 2 kg, que se abre para el molino) o cuarto
/// (250 g, que se vende al cliente). Un paquete abierto se da por usado entero: no hay paquetes a medias.
/// SinFormato va primero (valor 0) por la misma razón que SinGrupo (ver <see cref="GrupoCafe"/>).
/// </summary>
public enum FormatoCafe
{
    SinFormato,
    Paquete,
    Cuarto
}
