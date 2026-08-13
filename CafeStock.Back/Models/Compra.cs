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
    /// Datos de facturación, opcionales y normalmente rellenados a posteriori (cuando llega
    /// la factura en papel del proveedor, no en el momento de confirmar la recepción).
    /// MetodoPago: Efectivo / Transferencia / Domiciliado (o vacío si no se ha indicado).
    /// NumeroFacturaProveedor: el número que el PROVEEDOR le puso a su factura, texto libre.
    /// FacturaAdjuntaUrl: ruta a una foto o PDF de la factura real, mismo patrón que ImagenUrl.
    /// No incluye datos del emisor (esta cafetería) ni genera numeración propia: es solo el
    /// registro de lo que el proveedor entregó, no una factura emitida por nosotros.
    /// </summary>
    public string MetodoPago { get; set; } = string.Empty;
    public string NumeroFacturaProveedor { get; set; } = string.Empty;
    public string FacturaAdjuntaUrl { get; set; } = string.Empty;

    /// <summary>
    /// Notas libres sobre esta compra/factura, opcionales.
    /// </summary>
    public string Notas { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del proveedor ya resuelto, para consultas de solo lectura (Facturas) que no
    /// quieren obligar a la vista a hacer una búsqueda aparte. Null si no se ha resuelto
    /// (p.ej. al crear la compra) o si la compra no tiene proveedor asignado.
    /// </summary>
    public string? ProveedorNombre { get; set; }
}
