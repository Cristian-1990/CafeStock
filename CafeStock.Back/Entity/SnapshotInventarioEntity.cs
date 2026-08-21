namespace CafeStock.Back.Entity;

public class SnapshotInventarioEntity
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal ValorTotal { get; set; }
}
