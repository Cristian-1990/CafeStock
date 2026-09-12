namespace CafeStock.Back.Models;
/// <summary>
/// Proveedor de productos. De momento solo nombre; se ampliará con datos de contacto
/// cuando se decidan (v2 - listas agrupadas por proveedor).
/// </summary>
public record Proveedor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string PersonaContacto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DiaReparto { get; set; } = string.Empty;
    public string Notas { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string NifCif { get; set; } = string.Empty;
    public bool EsSupermercadoGenerico { get; set; } = false;

    /// <summary>
    /// Cuando está activo, /lista-detallada divide el catálogo de este proveedor en
    /// secciones "Por peso"/"Por unidad" (ver GrupoUnidadMedida). Puramente visual, sin
    /// efecto sobre Compra/Factura. Hoy solo Puchero lo tiene activo (sus dos cafés, Grano
    /// 1kg y Cuarto 250g), pero no depende de Producto.SeguimientoIndividual — son conceptos
    /// distintos que solo coinciden por casualidad en ese proveedor.
    /// </summary>
    public bool AgruparPorTipoUnidad { get; set; } = false;

    /// <summary>
    /// Cuando está activo, el precio unitario de los productos de este proveedor (y de las
    /// líneas de sus compras) se muestra y edita con 4 decimales en vez de los 2 habituales.
    /// Pensado para proveedores donde el precio por unidad sale de dividir un precio de
    /// caja/pack entre muchas unidades (p.ej. Priegola) y dos decimales pierden precisión
    /// real; el resto de proveedores (p.ej. Alcampo) se queda en 2. Semilla inicial solo para
    /// Priegola (ver AsegurarPrecioAltaPrecisionPriegolaAsync), pero editable a mano igual que
    /// EsSupermercadoGenerico/AgruparPorTipoUnidad — no depende de comparar por Nombre en
    /// tiempo de ejecución.
    /// </summary>
    public bool PrecioUnitarioAltaPrecision { get; set; } = false;
};
