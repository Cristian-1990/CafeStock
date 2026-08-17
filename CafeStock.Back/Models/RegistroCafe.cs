namespace CafeStock.Back.Models;

/// <summary>
/// Registro histórico de una venta/consumo de un producto con SeguimientoIndividual activo
/// (pantalla Vender): quién lo registró y cuándo. Se crea uno por cada unidad vendida, a la
/// vez que se resta 1 de StockActual del producto.
/// </summary>
public record RegistroCafe
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string NombreTrabajador { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }

    /// <summary>
    /// Nombre del producto ya resuelto, para el historial de Vender.razor (no obliga a la
    /// vista a hacer una búsqueda aparte). Null si no se ha resuelto.
    /// </summary>
    public string? ProductoNombre { get; set; }
}
