using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Services.HojasCafe;

public interface IHojaCafeService
{
    Task<HojaCafe> GetActualAsync();

    /// <summary>Todas las hojas, la más reciente primero.</summary>
    Task<IReadOnlyList<ResumenHojaCafe>> GetResumenesAsync();

    /// <summary>El tablero de una hoja; null = la hoja actual.</summary>
    Task<Result<HojaCafeDetalle, DomainError>> GetDetalleAsync(int? hojaId = null);

    /// <summary>Recepciona café: cierra la hoja actual y abre la siguiente con los paquetes nuevos y los sobrantes.</summary>
    Task<Result<HojaCafe, DomainError>> RecepcionarAsync(IReadOnlyList<NuevoPaqueteCafe> paquetes, string recepcionadoPor);
}
