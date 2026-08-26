using CafeStock.Back.Models;

namespace CafeStock.Blazor.Services;

/// <summary>
/// Agrupación puramente visual de las unidades de medida, usada para dividir
/// listados de productos en pestañas/secciones "Por peso" / "Por unidad".
/// No tiene ningún efecto sobre el dominio ni sobre Compra/Factura: una
/// compra sigue siendo una única factura por proveedor con líneas mezcladas.
/// </summary>
public enum GrupoUnidadMedida
{
    Peso,
    Unidad,
    SinClasificar
}

public static class UnidadMedidaExtensions
{
    public static GrupoUnidadMedida Grupo(this UnidadMedida unidad) => unidad switch
    {
        UnidadMedida.Kg or UnidadMedida.Gramos or UnidadMedida.Litros => GrupoUnidadMedida.Peso,
        UnidadMedida.Unidades or UnidadMedida.Bote or UnidadMedida.Bolsa => GrupoUnidadMedida.Unidad,
        _ => GrupoUnidadMedida.SinClasificar
    };
}

/// <summary>
/// Etiquetas de las secciones "Por peso"/"Por unidad" para el proveedor que distingue uso
/// interno de cafetería vs. venta al público (hoy, solo Puchero — ver
/// Proveedor.AgruparPorTipoUnidad). Compartidas entre ListaDetallada.razor y
/// ProveedorDetalle.razor para no duplicar el texto literal en dos sitios.
/// </summary>
public static class SeccionesUnidadLabels
{
    public const string UsoCafeteria = "Uso cafetería";
    public const string VentaAlPublico = "Venta al público";
}
