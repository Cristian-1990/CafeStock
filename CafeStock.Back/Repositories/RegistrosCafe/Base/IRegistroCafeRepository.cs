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

    /// <summary>
    /// Los últimos usos de cada uno de los productos indicados (más recientes primero, como mucho
    /// <paramref name="porProducto"/> de cada uno), para rellenar los huecos usados del tablero de café. Los
    /// productos sin ningún registro no aparecen en el diccionario.
    /// </summary>
    Task<IReadOnlyDictionary<int, List<RegistroCafe>>> GetUltimosPorProductoAsync(IReadOnlyCollection<int> productoIds, int porProducto);

    /// <summary>
    /// Deshace un registro: lo borra y devuelve 1 al stock del producto, en la misma transacción (el inverso
    /// de <see cref="RegistrarVentaAsync"/>). Failure RegistroNoEncontrado si ya no existe, así que deshacer dos
    /// veces seguidas no devuelve dos unidades.
    /// </summary>
    Task<Result<RegistroCafe, DomainError>> DeshacerVentaAsync(int registroId);
}
