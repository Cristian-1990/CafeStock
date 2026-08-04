namespace CafeStock.Back.Models;
/// <summary>
/// Clase principal
/// </summary>
public record Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty; //String.Empty evita que el compilador se queje por si puede ser null
    public int StockActual { get; set; }
    public int StockMaximo { get; set; }
    public UnidadMedida Unidad { get; set; } = UnidadMedida.SinEspecificar;
    public string Descripcion { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public int? ProveedorId { get; set; }
    public int CantidadAComprar => Math.Max(0, StockMaximo - StockActual);//Prodpiedad autocalculada

};