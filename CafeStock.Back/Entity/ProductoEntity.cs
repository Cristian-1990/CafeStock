namespace CafeStock.Back.Entity;

public class ProductoEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int StockActual { get; set; }
    public int StockMaximo { get; set; }
}