namespace CafeStock.Back.Models;

/// <summary>
/// Línea de una Compra: cuánto se pidió de un producto concreto y a qué precio unitario.
/// </summary>
public record LineaCompra
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    /// <summary>
    /// Nombre del producto ya resuelto, para consultas de solo lectura (Facturas) que no
    /// quieren obligar a la vista a hacer una búsqueda aparte. Null si no se ha resuelto
    /// (p.ej. al crear la compra).
    /// </summary>
    public string? ProductoNombre { get; set; }

    /// <summary>
    /// Captura silenciosa del excedente sobre StockMaximo al recepcionar (ver
    /// ProcesarRecepcionGrupoAsync en ListaCompra.razor): StockActual de Producto justo después
    /// de sumar esta línea, y StockMaximo de ese producto en ese mismo instante. Se guardan los
    /// dos valores en crudo, no un booleano ni el excedente ya restado, para no perder
    /// información — el excedente (si lo hay) se deriva después como
    /// StockResultanteTrasRecepcion - StockMaximoEnMomento. No bloquea ni avisa nada en el
    /// momento de la recepción, solo queda registrado para poder consultarlo más adelante.
    /// Nullable: las líneas creadas antes de este campo no tienen este dato — deben quedar en
    /// null, nunca fingir un 0 que parecería un valor real.
    /// </summary>
    public decimal? StockResultanteTrasRecepcion { get; set; }
    public decimal? StockMaximoEnMomento { get; set; }
}
