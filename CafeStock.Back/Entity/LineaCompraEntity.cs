namespace CafeStock.Back.Entity;

public class LineaCompraEntity
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public CompraEntity? Compra { get; set; }
    public int ProductoId { get; set; }
    public ProductoEntity? Producto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    /// <summary>Ver comentario del mismo campo en el modelo LineaCompra.</summary>
    public decimal? StockResultanteTrasRecepcion { get; set; }
    public decimal? StockMaximoEnMomento { get; set; }
}
