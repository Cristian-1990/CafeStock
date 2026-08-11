namespace CafeStock.Back.Models;

/// <summary>
/// Registro histórico de una recepción confirmada: qué proveedor, cuándo, y qué líneas
/// (producto, cantidad y precio unitario) se recibieron en ese momento.
/// </summary>
public record Compra
{
    public int Id { get; set; }
    public int? ProveedorId { get; set; }
    public DateTime Fecha { get; set; }
    public List<LineaCompra> Lineas { get; set; } = [];
}
