namespace CafeStock.Back.Entity;

public class RegistroCafeEntity
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public ProductoEntity? Producto { get; set; }
    public string NombreTrabajador { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
