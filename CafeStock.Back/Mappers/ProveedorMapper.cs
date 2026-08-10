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
            Telefono = proveedor.Telefono,
            PersonaContacto = proveedor.PersonaContacto,
            Email = proveedor.Email,
            DiaReparto = proveedor.DiaReparto,
            Notas = proveedor.Notas,
            EsSupermercadoGenerico = proveedor.EsSupermercadoGenerico,
        };
    }

    public static Proveedor ToProveedor(this ProveedorEntity entity)
    {
        return new Proveedor
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            ImagenUrl = entity.ImagenUrl,
            Telefono = entity.Telefono,
            PersonaContacto = entity.PersonaContacto,
            Email = entity.Email,
            DiaReparto = entity.DiaReparto,
            Notas = entity.Notas,
            EsSupermercadoGenerico = entity.EsSupermercadoGenerico
        };
    }
}
