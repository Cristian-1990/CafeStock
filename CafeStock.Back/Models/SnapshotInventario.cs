namespace CafeStock.Back.Models;

/// <summary>
/// Fotografía del valor total del inventario (StockActual × PrecioUnitario de todos los
/// productos) en una fecha concreta. Como mucho uno por día (ver índice único Fecha en
/// AppDbContext); lo captura SnapshotInventarioService.AsegurarSnapshotDeHoyAsync al cargar
/// Informes, no el usuario.
/// </summary>
public record SnapshotInventario
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal ValorTotal { get; set; }
}
