namespace CafeStock.Back.Entity;

public class CompraEntity
{
    public int Id { get; set; }
    public int? ProveedorId { get; set; }
    public ProveedorEntity? Proveedor { get; set; }
    public DateTime Fecha { get; set; }
    public List<LineaCompraEntity> Lineas { get; set; } = [];
    public string MetodoPago { get; set; } = string.Empty;
    public string NumeroFacturaProveedor { get; set; } = string.Empty;
    public string FacturaAdjuntaUrl { get; set; } = string.Empty;
}
