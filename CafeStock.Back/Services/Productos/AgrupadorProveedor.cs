using CafeStock.Back.Models;

namespace CafeStock.Back.Services.Productos;

/// <summary>
/// Agrupa productos por proveedor, dejando los que no tienen proveedor asignado (o cuyo
/// proveedor ya no existe) en un grupo aparte al final. Único punto de esta lógica de
/// agrupado: la reutilizan tanto el informe de pedido en PDF como la página de Compra.
/// </summary>
public static class AgrupadorProveedor
{
    public static IEnumerable<(Proveedor? Proveedor, List<Producto> Productos)> AgruparPorProveedor(
        IEnumerable<Producto> productos, IEnumerable<Proveedor> proveedores)
    {
        var proveedoresPorId = proveedores.ToDictionary(p => p.Id);

        return productos
            .GroupBy(p => p.ProveedorId.HasValue && proveedoresPorId.ContainsKey(p.ProveedorId.Value) ? p.ProveedorId : null)
            .OrderBy(g => g.Key is null)
            .ThenBy(g => g.Key.HasValue ? proveedoresPorId[g.Key.Value].Nombre : string.Empty)
            .Select(g => (
                Proveedor: g.Key.HasValue ? proveedoresPorId[g.Key.Value] : null,
                Productos: g.ToList()));
    }
}
