using CafeStock.Back.Entity;
using CafeStock.Back.Models;

namespace CafeStock.Back.Mappers;

public static class CompraMapper
{
    public static CompraEntity ToEntity(this Compra compra)
    {
        return new CompraEntity
        {
            Id = compra.Id,
            ProveedorId = compra.ProveedorId,
            Fecha = compra.Fecha,
            Lineas = compra.Lineas.Select(l => l.ToEntity()).ToList(),
            MetodoPago = compra.MetodoPago,
            NumeroFacturaProveedor = compra.NumeroFacturaProveedor,
            FacturaAdjuntaUrl = compra.FacturaAdjuntaUrl,
            Notas = compra.Notas
        };
    }

    public static Compra ToCompra(this CompraEntity entity)
    {
        return new Compra
        {
            Id = entity.Id,
            ProveedorId = entity.ProveedorId,
            Fecha = entity.Fecha,
            Lineas = entity.Lineas.Select(l => l.ToLineaCompra()).ToList(),
            MetodoPago = entity.MetodoPago,
            NumeroFacturaProveedor = entity.NumeroFacturaProveedor,
            FacturaAdjuntaUrl = entity.FacturaAdjuntaUrl,
            Notas = entity.Notas
        };
    }

    public static LineaCompraEntity ToEntity(this LineaCompra linea)
    {
        return new LineaCompraEntity
        {
            Id = linea.Id,
            CompraId = linea.CompraId,
            ProductoId = linea.ProductoId,
            Cantidad = linea.Cantidad,
            PrecioUnitario = linea.PrecioUnitario
        };
    }

    public static LineaCompra ToLineaCompra(this LineaCompraEntity entity)
    {
        return new LineaCompra
        {
            Id = entity.Id,
            CompraId = entity.CompraId,
            ProductoId = entity.ProductoId,
            Cantidad = entity.Cantidad,
            PrecioUnitario = entity.PrecioUnitario
        };
    }
}
