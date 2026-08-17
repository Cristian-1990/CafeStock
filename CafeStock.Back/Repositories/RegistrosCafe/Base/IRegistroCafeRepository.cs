using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Repositories.RegistrosCafe.Base;

public interface IRegistroCafeRepository
{
    /// <summary>
    /// Últimos registros (más recientes primero), para el historial de Vender.razor.
    /// </summary>
    Task<IEnumerable<RegistroCafe>> GetUltimosAsync(int cantidad);

    /// <summary>
    /// Registra una venta: comprueba que el producto exista y tenga StockActual > 0, resta 1
    /// y crea el RegistroCafe, todo en la misma transacción — igual que ConfirmarRecepcionAsync
    /// mantiene StockActual sincronizado con lo que de verdad ha pasado, sin depender de un
    /// segundo paso aparte que alguien podría olvidar.
    /// </summary>
    Task<Result<RegistroCafe, DomainError>> RegistrarVentaAsync(int productoId, string nombreTrabajador);
}
