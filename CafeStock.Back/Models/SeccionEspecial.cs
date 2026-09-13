namespace CafeStock.Back.Models;

/// <summary>
/// Sección especial a la que pertenece un producto dentro de un supermercado genérico (ver
/// Proveedor.EsSupermercadoGenerico) — se usa ÚNICAMENTE para colorear su fila al imprimir la
/// lista de la compra (ver PdfService.GenerarListaCompra), nunca para nada más. Sigue el mismo
/// patrón que EsSupermercadoGenerico: un campo explícito en vez de comparar por texto contra
/// Producto.Nombre. Ninguna va primero (valor 0) para que los productos ya existentes, guardados
/// antes de que este campo existiera, no hereden por error la primera sección "real" del enum al
/// persistirse como número en SQLite.
/// </summary>
public enum SeccionEspecial
{
    Ninguna,
    Fruteria,
    Infusiones
}
