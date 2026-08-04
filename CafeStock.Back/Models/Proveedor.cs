namespace CafeStock.Back.Models;
/// <summary>
/// Proveedor de productos. De momento solo nombre; se ampliará con datos de contacto
/// cuando se decidan (v2 - listas agrupadas por proveedor).
/// </summary>
public record Proveedor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
};
