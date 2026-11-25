using CafeStock.Back.Entity;
using CafeStock.Back.Models;

namespace CafeStock.Back.Mappers;

public static class StockRealizadoMapper
{
    public static StockRealizadoEntity ToEntity(this StockRealizado stock) => new()
    {
        Id = stock.Id,
        ProveedorId = stock.ProveedorId,
        Fecha = stock.Fecha
    };

    public static StockRealizado ToStockRealizado(this StockRealizadoEntity entity) => new()
    {
        Id = entity.Id,
        ProveedorId = entity.ProveedorId,
        Fecha = entity.Fecha
    };
}
