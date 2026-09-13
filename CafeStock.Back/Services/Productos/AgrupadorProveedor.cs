using CafeStock.Back.Models;

namespace CafeStock.Back.Services.Productos;

/// <summary>
/// Agrupa productos por proveedor, dejando los que no tienen proveedor asignado (o cuyo
/// proveedor ya no existe) en un grupo aparte al final. Único punto de esta lógica de
/// agrupado: la reutilizan tanto el informe de pedido en PDF como la página de Compra.
/// </summary>
public static class AgrupadorProveedor
{
    /// <summary>
    /// Clave de orden para Producto.Pasillo cuando todavía no se ha asignado (null). Se queda
    /// por debajo de cualquier valor "va siempre último" (p.ej. 100 para el helado, ver
    /// Producto.Pasillo) pero por encima de cualquier pasillo físico real (no hay más de 50 en
    /// ningún supermercado) — así un producto sin pasillo asignado nunca salta antes de uno que
    /// sí lo tiene, ni después del que debe ir el último.
    /// </summary>
    private const int PasilloSinAsignar = 90;

    public static IEnumerable<(Proveedor? Proveedor, List<Producto> Productos)> AgruparPorProveedor(
        IEnumerable<Producto> productos, IEnumerable<Proveedor> proveedores)
    {
        var proveedoresPorId = proveedores.ToDictionary(p => p.Id);

        return productos
            .GroupBy(p => p.ProveedorId.HasValue && proveedoresPorId.ContainsKey(p.ProveedorId.Value) ? p.ProveedorId : null)
            .OrderBy(g => g.Key is null)
            .ThenBy(g => g.Key.HasValue ? proveedoresPorId[g.Key.Value].Nombre : string.Empty)
            .Select(g =>
            {
                var proveedor = g.Key.HasValue ? proveedoresPorId[g.Key.Value] : null;
                return (Proveedor: proveedor, Productos: OrdenarProductosDelGrupo(g, proveedor));
            });
    }

    /// <summary>
    /// Para proveedores "supermercado genérico" (compra suelta caminando por la tienda, p.ej.
    /// Alcampo — ver Proveedor.EsSupermercadoGenerico), ordena por Pasillo y, dentro de él,
    /// por OrdenEnPasillo para seguir el recorrido físico exacto (no alfabético) y no ir de un
    /// lado a otro; el resto de proveedores (reparto por distribuidor) mantiene el orden de
    /// siempre, sin cambios. Nombre como último desempate, solo por si dos productos comparten
    /// Pasillo y OrdenEnPasillo sin haberse terminado de ordenar todavía.
    /// </summary>
    private static List<Producto> OrdenarProductosDelGrupo(IEnumerable<Producto> productosDelGrupo, Proveedor? proveedor) =>
        proveedor?.EsSupermercadoGenerico == true
            ? productosDelGrupo.OrderBy(p => p.Pasillo ?? PasilloSinAsignar).ThenBy(p => p.OrdenEnPasillo).ThenBy(p => p.Nombre).ToList()
            : productosDelGrupo.ToList();
}
