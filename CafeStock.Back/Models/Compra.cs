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

    /// <summary>
    /// Nombre del proveedor ya resuelto, para consultas de solo lectura (Facturas) que no
    /// quieren obligar a la vista a hacer una búsqueda aparte. Null si no se ha resuelto
    /// (p.ej. al crear la compra) o si la compra no tiene proveedor asignado.
    /// </summary>
    public string? ProveedorNombre { get; set; }
}
