using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;

namespace CafeStock.Back.Services.Productos;

public interface IProductoService
{
    Task<IEnumerable<Producto>> GetAllAsync();
    Task<Result<Producto, DomainError>> GetByIdAsync(int id);
    Task<Result<Producto, DomainError>> CreateAsync(Producto producto);
    Task<Result<Producto, DomainError>> UpdateAsync(int id, Producto producto);
    Task<Result<Producto, DomainError>> DeleteAsync(int id);
    Task<IEnumerable<Producto>> GetProductosBajoMinimoAsync();
}