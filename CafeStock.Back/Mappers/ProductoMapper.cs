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
           Unidad = producto.Unidad,
           Descripcion = producto.Descripcion,
           ImagenUrl = producto.ImagenUrl,
           ProveedorId = producto.ProveedorId,
           PrecioUnitario = producto.PrecioUnitario,
           SeguimientoIndividual = producto.SeguimientoIndividual,
           AplicaConciliacionTpv = producto.AplicaConciliacionTpv,
           UnidadesPorPack = producto.UnidadesPorPack,
           Pasillo = producto.Pasillo,
           OrdenEnPasillo = producto.OrdenEnPasillo,
           EsFrontalPasillo = producto.EsFrontalPasillo,
           Armario = producto.Armario,
           Seccion = producto.Seccion,
           Pais = producto.Pais,
           Region = producto.Region,
           Finca = producto.Finca,
           Altitud = producto.Altitud,
           Variedad = producto.Variedad,
           Proceso = producto.Proceso,
           NotasCata = producto.NotasCata,
       };
    }

    public static Producto ToProducto(this ProductoEntity entity)
    {
        return new Producto
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            StockActual = SinCerosFinales(entity.StockActual),
            StockMaximo = SinCerosFinales(entity.StockMaximo),
            Unidad = entity.Unidad,
            Descripcion = entity.Descripcion,
            ImagenUrl = entity.ImagenUrl,
            ProveedorId = entity.ProveedorId,
            PrecioUnitario = entity.PrecioUnitario,
            SeguimientoIndividual = entity.SeguimientoIndividual,
            AplicaConciliacionTpv = entity.AplicaConciliacionTpv,
            UnidadesPorPack = entity.UnidadesPorPack,
            Pasillo = entity.Pasillo,
            OrdenEnPasillo = entity.OrdenEnPasillo,
            EsFrontalPasillo = entity.EsFrontalPasillo,
            Armario = entity.Armario,
            Seccion = entity.Seccion,
            Pais = entity.Pais,
            Region = entity.Region,
            Finca = entity.Finca,
            Altitud = entity.Altitud,
            Variedad = entity.Variedad,
            Proceso = entity.Proceso,
            NotasCata = entity.NotasCata
        };
    }

    /// <summary>
    /// Quita los ceros finales de un decimal (0,50 pasa a 0,5 y 2,00 a 2), para que el stock se muestre igual
    /// en todas las pantallas sin tener que formatearlo en cada una.
    /// </summary>
    private static decimal SinCerosFinales(decimal valor) => valor / 1.0000000000000000000000000000m;
}
