namespace CafeStock.Back.Entity;

public class RegistroCafeEntity
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public ProductoEntity? Producto { get; set; }
    public string NombreTrabajador { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int? PaqueteId { get; set; }
    public PaqueteCafeEntity? Paquete { get; set; }
    public int? HojaId { get; set; }
    public HojaCafeEntity? Hoja { get; set; }
}
