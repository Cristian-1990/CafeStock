using CafeStock.Back.Entity;
using CafeStock.Back.Models;
namespace CafeStock.Back.Mappers;

public static class ProveedorMapper
{
    public static ProveedorEntity ToEntity(this Proveedor proveedor)
    {
        return new ProveedorEntity
        {
            Id = proveedor.Id,
            Nombre = proveedor.Nombre,
            ImagenUrl = proveedor.ImagenUrl,
        };
    }

    public static Proveedor ToProveedor(this ProveedorEntity entity)
    {
        return new Proveedor
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            ImagenUrl = entity.ImagenUrl
        };
    }
}
