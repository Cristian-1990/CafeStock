namespace CafeStock.Back.Models;

/// <summary>
/// Importe asignado a un proveedor para un mes/año concreto. Uno por combinación
/// ProveedorId+Mes+Anio (ver índice único en AppDbContext y la comprobación de duplicados
/// en PresupuestosEfRepository).
/// </summary>
public record Presupuesto
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public int Mes { get; set; }
    public int Anio { get; set; }
    public decimal ImporteAsignado { get; set; }

    /// <summary>
    /// Nombre del proveedor ya resuelto, para listados de solo lectura que no quieren
    /// obligar a la vista a hacer una búsqueda aparte. Null si no se ha resuelto.
    /// </summary>
    public string? ProveedorNombre { get; set; }
}
