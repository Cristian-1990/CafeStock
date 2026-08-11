namespace CafeStock.Back.Entity;

public class CompraEntity
{
    public int Id { get; set; }
    public int? ProveedorId { get; set; }
    public ProveedorEntity? Proveedor { get; set; }
    public DateTime Fecha { get; set; }
    public List<LineaCompraEntity> Lineas { get; set; } = [];
}
