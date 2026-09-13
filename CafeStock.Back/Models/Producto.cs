namespace CafeStock.Back.Models;
/// <summary>
/// Clase principal
/// </summary>
public record Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty; //String.Empty evita que el compilador se queje por si puede ser null
    public int StockActual { get; set; }
    public int StockMaximo { get; set; }
    public UnidadMedida Unidad { get; set; } = UnidadMedida.SinEspecificar;
    public string Descripcion { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public int? ProveedorId { get; set; }
    public decimal PrecioUnitario { get; set; }
    public int CantidadAComprar => Math.Max(0, StockMaximo - StockActual);//Prodpiedad autocalculada

    /// <summary>
    /// Activa el registro de venta/consumo individual (ver RegistroCafe y la pantalla
    /// Vender): en vez de recontarse semanalmente en Realizar Stock, su StockActual baja de
    /// uno en uno según se van registrando ventas. Pensado para casos muy concretos (los
    /// cafés); el resto de productos sigue funcionando exactamente igual que siempre.
    /// </summary>
    public bool SeguimientoIndividual { get; set; }

    /// <summary>
    /// Marca productos que se venden íntegros (p.ej. una lata de Coca-Cola: se compra una
    /// unidad y se vende esa misma unidad, sin transformación) — de cara a una futura
    /// conciliación con el TPV. Default false es un dato real para todo el historial (a
    /// diferencia del excedente de stock, aquí no hay nada que "fingir": ausencia de este
    /// campo antes de existir significa, correctamente, que no aplicaba).
    /// </summary>
    public bool AplicaConciliacionTpv { get; set; }

    /// <summary>
    /// Nº de unidades que trae cada pack/caja de este producto tal como lo entrega el
    /// proveedor (p.ej. 30 latas por caja). Null o 1 significa "sin conversión": la pantalla
    /// de Recepcionar no ofrece el toggle de packs y todo funciona exactamente como antes.
    /// </summary>
    public int? UnidadesPorPack { get; set; }

    /// <summary>
    /// Pasillo del supermercado donde se encuentra este producto (solo tiene sentido para
    /// proveedores con Proveedor.EsSupermercadoGenerico activo, p.ej. Alcampo) — se usa
    /// ÚNICAMENTE para ordenar la lista de la compra siguiendo el recorrido físico de la
    /// tienda (ver AgrupadorProveedor), nunca se muestra en ningún listado ni ficha; solo es
    /// editable desde el formulario de producto. Convención manual de numeración (no hay más
    /// de 50 pasillos reales en ningún supermercado): productos que deban ir siempre al final
    /// de la lista para no estropearse (p.ej. el helado, que se derrite) usan un número alto
    /// como 100 en vez de su pasillo físico real. Null mientras no se haya rellenado todavía.
    /// </summary>
    public int? Pasillo { get; set; }

    /// <summary>
    /// Posición dentro de su Pasillo, siguiendo el recorrido real al caminar ese pasillo
    /// (ver AgrupadorProveedor) — el orden alfabético no sirve porque no tiene nada que ver
    /// con dónde está cada cosa físicamente. Igual que Pasillo: interno, nunca se muestra en
    /// ningún listado ni ficha, solo editable desde el formulario de producto. Convención
    /// manual con huecos (10, 20, 30...) para poder insertar productos nuevos en medio sin
    /// renumerar los demás. También se usa para colocar una sección que no tiene pasillo
    /// propio (p.ej. Infusiones) justo detrás de todo un pasillo real: se le asigna el mismo
    /// Pasillo que el de justo antes con un valor alto (p.ej. 1000+) para que quede al final
    /// de ese grupo. 0 por defecto (sin definir todavía).
    /// </summary>
    public int OrdenEnPasillo { get; set; }

    /// <summary>
    /// Marca los productos que están en el frontal del pasillo (los que en la lista de la
    /// compra interna se escriben en MAYÚSCULAS) — se usa únicamente para subrayarlos al
    /// imprimir/enviar la lista de la compra (ver PdfService), nunca se muestra en ningún
    /// listado ni ficha. Igual que Pasillo/OrdenEnPasillo: solo tiene sentido para
    /// supermercados genéricos y solo editable desde el formulario de producto.
    /// </summary>
    public bool EsFrontalPasillo { get; set; }

    /// <summary>
    /// Sección especial a la que pertenece el producto (frutería, infusiones...) — ver
    /// SeccionEspecial. Se usa únicamente para colorear su fila en la lista de la compra (ver
    /// PdfService), nunca se muestra en ningún listado ni ficha. Igual que
    /// Pasillo/OrdenEnPasillo/EsFrontalPasillo: solo tiene sentido para supermercados genéricos
    /// y solo editable desde el formulario de producto.
    /// </summary>
    public SeccionEspecial Seccion { get; set; }
};