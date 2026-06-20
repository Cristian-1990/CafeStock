using CafeStock.Back.Entity;
using CafeStock.Back.Models;
namespace CafeStock.Back.Mappers;

public static class ProductoMapper
{
    public static ProductoEntity ToEntity(this Producto producto)
    {
       return  new ProductoEntity
       {
           Id = producto.Id,
           Nombre = producto.Nombre,
           StockActual = producto.StockActual,
           StockMaximo = producto.StockMaximo,
       };
    }

    public static Producto ToProducto(this ProductoEntity entity)
    {
        return new Producto
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            StockActual = entity.StockActual,
            StockMaximo = entity.StockMaximo
        };
    }
}