namespace CafeStock.Back.Models;

/// <summary>
/// Línea de una Compra: cuánto se pidió de un producto concreto y a qué precio unitario.
/// </summary>
public record LineaCompra
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}
