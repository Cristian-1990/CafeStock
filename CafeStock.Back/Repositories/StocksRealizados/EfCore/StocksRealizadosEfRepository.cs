using CafeStock.Back.Entity;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.StocksRealizados;
using CafeStock.Back.Infrastructure;
using CafeStock.Back.Mappers;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.StocksRealizados.Base;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CafeStock.Back.Repositories.StocksRealizados.EfCore;

public class StocksRealizadosEfRepository : IStockRealizadoRepository
{
    private readonly string _connectionString;

    public StocksRealizadosEfRepository(string connection)
    {
        _connectionString = connection;
    }

    private AppDbContext CreateContext() => new AppDbContext(_connectionString);

    // El esquema lo gestiona DatabaseInitializer (EF Core Migrations); es idempotente y se
    // ejecuta una sola vez por base de datos.
    private Task InitializeAsync() => DatabaseInitializer.EnsureInitializedAsync(_connectionString);

    public async Task<IEnumerable<StockRealizado>> GetAllAsync()
    {
        await InitializeAsync();
        using var context = CreateContext();
        return await context.StocksRealizados
            .Select(e => e.ToStockRealizado())
            .ToListAsync();
    }

    public async Task<Result<StockRealizado, DomainError>> MarcarAsync(int proveedorId, DateTime fecha)
    {
        await InitializeAsync();
        try
        {
            using var context = CreateContext();
            var entity = await context.StocksRealizados.FirstOrDefaultAsync(e => e.ProveedorId == proveedorId);
            if (entity is null)
            {
                entity = new StockRealizadoEntity { ProveedorId = proveedorId, Fecha = fecha };
                context.StocksRealizados.Add(entity);
            }
            else
            {
                entity.Fecha = fecha;
            }

            await context.SaveChangesAsync();
            return Result.Success<StockRealizado, DomainError>(entity.ToStockRealizado());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al marcar el stock como realizado (ProveedorId={ProveedorId})", proveedorId);
            return Result.Failure<StockRealizado, DomainError>(StockRealizadoErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<Result<bool, DomainError>> QuitarAsync(int proveedorId)
    {
        await InitializeAsync();
        try
        {
            using var context = CreateContext();
            await context.StocksRealizados.Where(e => e.ProveedorId == proveedorId).ExecuteDeleteAsync();
            return Result.Success<bool, DomainError>(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al quitar la marca de stock realizado (ProveedorId={ProveedorId})", proveedorId);
            return Result.Failure<bool, DomainError>(StockRealizadoErrors.DatabaseError(ex.Message));
        }
    }
}
