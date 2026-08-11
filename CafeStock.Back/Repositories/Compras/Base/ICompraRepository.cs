using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Repositories.Compras.Base;

public interface ICompraRepository
{
    Task<IEnumerable<Compra>> GetAllAsync();
    Task<Result<Compra, DomainError>> CreateAsync(Compra compra);
}
