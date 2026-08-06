using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;
namespace CafeStock.Back.Repositories.Productos.Base;

public interface IProductoRepository
{
    Task<IEnumerable<Producto>> GetAllAsync();
    Task<Result<Producto, DomainError>> GetByIdAsync(int id);
    Task<Result<Producto, DomainError>> CreateAsync(Producto producto);
    Task<Result<Producto, DomainError>> UpdateAsync(int id, Producto producto);
    Task<Result<Producto, DomainError>> DeleteAsync(int id);
    Task<Result<Producto, DomainError>> ConfirmarRecepcionAsync(int id, int cantidadRecibida);
    Task<IEnumerable<Producto>> ProductosUrgentes();
}