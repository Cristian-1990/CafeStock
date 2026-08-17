using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Services.RegistrosCafe;

public interface IRegistroCafeService
{
    /// <summary>
    /// Últimos registros (más recientes primero) con ProductoNombre ya resuelto.
    /// </summary>
    Task<IEnumerable<RegistroCafe>> GetUltimosAsync(int cantidad = 20);

    /// <summary>
    /// Registra una venta. El nombre del trabajador es obligatorio (segunda barrera de
    /// defensa: el formulario de Vender.razor ya no deja registrar en blanco).
    /// </summary>
    Task<Result<RegistroCafe, DomainError>> RegistrarVentaAsync(int productoId, string nombreTrabajador);
}
