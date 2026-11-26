namespace CafeStock.Back.Entity;

public class HojaCafeEntity
{
    public int Id { get; set; }
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
}

public class PaqueteCafeEntity
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public ProductoEntity? Producto { get; set; }
    public int Grupo { get; set; }
    public int Formato { get; set; }
    public int HojaOrigenId { get; set; }
    public HojaCafeEntity? HojaOrigen { get; set; }
    public DateTime? FechaTueste { get; set; }
    public string RecepcionadoPor { get; set; } = string.Empty;
    public DateTime FechaRecepcion { get; set; }
}
