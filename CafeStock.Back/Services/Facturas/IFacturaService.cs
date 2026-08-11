using CafeStock.Back.Models;

namespace CafeStock.Back.Services.Facturas;

/// <summary>
/// Consulta de solo lectura sobre el histórico de Compras, presentado como "Factura" de cara
/// al usuario. No añade persistencia propia: reutiliza Compra/LineaCompra ya existentes y les
/// resuelve el nombre de proveedor/producto para que la vista no tenga que hacer búsquedas aparte.
/// </summary>
public interface IFacturaService
{
    Task<IEnumerable<Compra>> GetAllAsync();
    Task<IEnumerable<Compra>> GetByProveedorAsync(int proveedorId);
}
