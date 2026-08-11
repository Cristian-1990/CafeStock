using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Services.Compras;

public interface ICompraService
{
    Task<IEnumerable<Compra>> GetAllAsync();
    Task<Result<Compra, DomainError>> CreateAsync(Compra compra);
}
