using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Repositories.Compras.Base;

public interface ICompraRepository
{
    Task<IEnumerable<Compra>> GetAllAsync();
    Task<Result<Compra, DomainError>> CreateAsync(Compra compra);

    /// <summary>
    /// Actualiza únicamente los datos de facturación de una Compra ya registrada.
    /// </summary>
    Task<Result<Compra, DomainError>> ActualizarDatosFacturaAsync(
        int id, string metodoPago, string numeroFacturaProveedor, string facturaAdjuntaUrl);

    /// <summary>
    /// Actualiza únicamente las notas de una Compra ya registrada.
    /// </summary>
    Task<Result<Compra, DomainError>> ActualizarNotasAsync(int id, string notas);
}
