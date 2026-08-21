using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Repositories.SnapshotsInventario.Base;

public interface ISnapshotInventarioRepository
{
    /// <summary>
    /// Devuelve el snapshot de una fecha concreta, o un Failure NotFound si no existe todavía
    /// (caso normal la primera vez que se carga Informes en un día nuevo).
    /// </summary>
    Task<Result<SnapshotInventario, DomainError>> GetByFechaAsync(DateOnly fecha);

    Task<Result<SnapshotInventario, DomainError>> CreateAsync(SnapshotInventario snapshot);
}
