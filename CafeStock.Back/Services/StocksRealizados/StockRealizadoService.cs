using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.StocksRealizados.Base;
using CSharpFunctionalExtensions;
using Serilog;

namespace CafeStock.Back.Services.StocksRealizados;

public class StockRealizadoService : IStockRealizadoService
{
    private readonly IStockRealizadoRepository _repository;

    public StockRealizadoService(IStockRealizadoRepository repository)
    {
        _repository = repository;
    }

    private static int Clave(int? proveedorId) => proveedorId ?? StockRealizado.SinProveedor;

    public async Task<HashSet<int?>> GetProveedoresPendientesAsync()
    {
        var marcas = await _repository.GetAllAsync();
        return marcas
            .Select(m => m.ProveedorId == StockRealizado.SinProveedor ? (int?)null : m.ProveedorId)
            .ToHashSet();
    }

    public async Task<Result<bool, DomainError>> MarcarAsync(int? proveedorId)
    {
        var resultado = await _repository.MarcarAsync(Clave(proveedorId), DateTime.Now);
        if (resultado.IsFailure)
            return Result.Failure<bool, DomainError>(resultado.Error);

        Log.Information("Stock realizado, recepción pendiente: ProveedorId={ProveedorId}", Clave(proveedorId));
        return Result.Success<bool, DomainError>(true);
    }

    public async Task<Result<bool, DomainError>> QuitarAsync(int? proveedorId)
    {
        var resultado = await _repository.QuitarAsync(Clave(proveedorId));
        if (resultado.IsSuccess)
            Log.Information("Recepción hecha, marca de stock quitada: ProveedorId={ProveedorId}", Clave(proveedorId));
        return resultado;
    }
}
