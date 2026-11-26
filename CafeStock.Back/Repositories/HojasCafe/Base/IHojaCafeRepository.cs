using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CSharpFunctionalExtensions;

namespace CafeStock.Back.Repositories.HojasCafe.Base;

public interface IHojaCafeRepository
{
    /// <summary>La hoja abierta (la crea si no hay ninguna) y, de paso, da de alta los paquetes de los cafés que ya tenían stock antes de las hojas.</summary>
    Task<HojaCafe> GetActualAsync();

    /// <summary>Todas las hojas, la más reciente primero, con cuántos paquetes tiene cada una y cuántos se usaron en ella.</summary>
    Task<IReadOnlyList<ResumenHojaCafe>> GetResumenesAsync();

    /// <summary>El tablero de una hoja (null = la actual). Failure HojaNoEncontrada si no existe.</summary>
    Task<Result<HojaCafeDetalle, DomainError>> GetDetalleAsync(int? hojaId);

    /// <summary>
    /// Recepciona café: cierra la hoja actual (si tiene algo) y abre la siguiente con los paquetes nuevos más los que
    /// sobraban; suma los paquetes al stock de cada café, todo en una transacción. Si la hoja actual está vacía se
    /// reutiliza en vez de cerrarla.
    /// </summary>
    Task<Result<HojaCafe, DomainError>> RecepcionarAsync(IReadOnlyList<NuevoPaqueteCafe> paquetes, string recepcionadoPor);
}
