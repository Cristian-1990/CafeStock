using CafeStock.Back.Entity;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Compras;
using CafeStock.Back.Mappers;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Compras.Base;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CafeStock.Back.Repositories.Compras.EfCore;

public class ComprasEfRepository : ICompraRepository
{
    private readonly string _connectionString;
    private bool _initialized;

    public ComprasEfRepository(string connection)
    {
        _connectionString = connection;
    }

    private AppDbContext CreateContext() => new AppDbContext(_connectionString);

    private async Task InitializeAsync()
    {
        if (_initialized) return;
        using var context = CreateContext();
        await context.EnsureCreatedAsync();
        await context.AsegurarTablasComprasAsync();
        _initialized = true;
    }

    public async Task<IEnumerable<Compra>> GetAllAsync()
    {
        await InitializeAsync();
        using var context = CreateContext();
        return await context.Compras
            .Include(e => e.Lineas)
            .Select(e => e.ToCompra())
            .ToListAsync();
    }

    public async Task<Result<Compra, DomainError>> CreateAsync(Compra compra)
    {
        await InitializeAsync();
        try
        {
            using var context = CreateContext();
            var entity = compra.ToEntity();
            context.Compras.Add(entity);
            await context.SaveChangesAsync();
            return Result.Success<Compra, DomainError>(entity.ToCompra());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al crear la compra (ProveedorId={ProveedorId})", compra.ProveedorId);
            return Result.Failure<Compra, DomainError>(CompraErrors.DatabaseError(ex.Message));
        }
    }
}
