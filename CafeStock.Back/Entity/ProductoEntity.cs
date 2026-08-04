using CafeStock.Back.Models;

namespace CafeStock.Back.Entity;

public class ProductoEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int StockActual { get; set; }
    public int StockMaximo { get; set; }
    public UnidadMedida Unidad { get; set; } = UnidadMedida.SinEspecificar;
    public string Descripcion { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
}