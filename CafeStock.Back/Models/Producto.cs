namespace CafeStock.Back.Models;
/// <summary>
/// Clase principal
/// </summary>
public record Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty; //String.Empty evita que el compilador se queje por si puede ser null
    public decimal StockActual { get; set; }
    public decimal StockMaximo { get; set; }
    public UnidadMedida Unidad { get; set; } = UnidadMedida.SinEspecificar;
    public string Descripcion { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public int? ProveedorId { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal CantidadAComprar => Math.Max(0m, StockMaximo - StockActual);//Prodpiedad autocalculada

    /// <summary>
    /// Activa el registro de venta/consumo individual (ver RegistroCafe y la pantalla
    /// Vender): en vez de recontarse semanalmente en Realizar Stock, su StockActual baja de
    /// uno en uno según se van registrando ventas. Pensado para casos muy concretos (los
    /// cafés); el resto de productos sigue funcionando exactamente igual que siempre.
    /// </summary>
    public bool SeguimientoIndividual { get; set; }

    /// <summary>
    /// Grupo y formato del café en el tablero de la pantalla Café (ver <see cref="GrupoCafe"/> y
    /// <see cref="FormatoCafe"/>). Solo tienen sentido juntos: un producto sale en el tablero si tiene los dos.
    /// Asignar un grupo lo pasa a SeguimientoIndividual (cada uso se registra de uno en uno, igual que en Venta).
    /// </summary>
    public GrupoCafe GrupoCafe { get; set; }
    public FormatoCafe FormatoCafe { get; set; }

    /// <summary>
    /// Solo para editar: el StockActual que tenía el producto cuando se abrió el formulario de
    /// edición. Permite a UpdateAsync distinguir "el usuario no tocó el stock" de "lo cambió a
    /// propósito": en el primer caso no lo escribe, para no pisar ventas o recepciones hechas
    /// mientras el formulario estaba abierto. Null (por defecto) = no se sabe, y UpdateAsync se
    /// comporta como siempre (escribe StockActual). Ningún mapper lo rellena: lo informa quien
    /// edita.
    /// </summary>
    public decimal? StockActualAlLeer { get; set; }

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
    /// Armario (o zona de la despensa) donde se guarda este producto en el local — se usa para dividir
    /// por armarios la pantalla Realizar stock de los supermercados genéricos (p.ej. Alcampo), de modo
    /// que se cuente armario a armario. Nada que ver con Pasillo (que es del supermercado donde se
    /// compra): este es el sitio donde se almacena en la cafetería. Numeración manual (1, 2, 3...), solo
    /// editable desde el formulario de producto. Null mientras no se haya asignado: esos productos
    /// salen al final, en "Sin armario".
    /// </summary>
    public int? Armario { get; set; }

    /// <summary>
    /// Sección especial a la que pertenece el producto (frutería, infusiones...) — ver
    /// SeccionEspecial. Se usa únicamente para colorear su fila en la lista de la compra (ver
    /// PdfService), nunca se muestra en ningún listado ni ficha. Igual que
    /// Pasillo/OrdenEnPasillo/EsFrontalPasillo: solo tiene sentido para supermercados genéricos
    /// y solo editable desde el formulario de producto.
    /// </summary>
    public SeccionEspecial Seccion { get; set; }

    // ---- Ficha de café (solo tiene sentido para productos con SeguimientoIndividual) ----
    // Texto libre y opcional; se muestra en la pantalla Venta junto a la imagen del producto
    // (ImagenUrl) para que quien vende pueda explicar el café al cliente. Vacío = no informado.
    // Pensado a partir de la ficha de producto de un tostador de especialidad (país, región,
    // finca, altitud, variedad, proceso y notas de cata).
    public string Pais { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Finca { get; set; } = string.Empty;
    public string Altitud { get; set; } = string.Empty;
    public string Variedad { get; set; } = string.Empty;
    public string Proceso { get; set; } = string.Empty;
    public string NotasCata { get; set; } = string.Empty;
};