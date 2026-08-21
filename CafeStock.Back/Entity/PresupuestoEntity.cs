namespace CafeStock.Back.Entity;

public class PresupuestoEntity
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public ProveedorEntity? Proveedor { get; set; }
    public int Mes { get; set; }
    public int Anio { get; set; }
    public decimal ImporteAsignado { get; set; }
}
