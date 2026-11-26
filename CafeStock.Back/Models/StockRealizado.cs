namespace CafeStock.Back.Models;

/// <summary>
/// Marca de que ya se hizo el stock (el recuento en Realizar stock) de un proveedor y está pendiente de
/// recibir su mercancía: Recepcionar solo enseña los proveedores que tienen esta marca. Se crea al
/// terminar el stock de un proveedor y se borra al confirmar su recepción.
///
/// ProveedorId = 0 significa "Sin proveedor asignado" (no hay FK a propósito: un proveedor eliminado deja
/// una marca huérfana inofensiva, y el grupo sin proveedor también necesita su marca).
/// Como mucho una marca por proveedor (índice único en AppDbContext).
/// </summary>
public record StockRealizado
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public DateTime Fecha { get; set; }

    /// <summary>Valor de ProveedorId para los productos sin proveedor.</summary>
    public const int SinProveedor = 0;
}
