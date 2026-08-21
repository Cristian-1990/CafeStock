using CafeStock.Back.Entity;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.SnapshotsInventario;
using CafeStock.Back.Mappers;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.SnapshotsInventario.Base;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CafeStock.Back.Repositories.SnapshotsInventario.EfCore;

public class SnapshotsInventarioEfRepository : ISnapshotInventarioRepository
{
    private readonly string _connectionString;
    private bool _initialized;

    public SnapshotsInventarioEfRepository(string connection)
    {
        _connectionString = connection;
    }

    private AppDbContext CreateContext() => new AppDbContext(_connectionString);

    private async Task InitializeAsync()
    {
        if (_initialized) return;
        using var context = CreateContext();
        await context.EnsureCreatedAsync();
        await context.AsegurarTablaSnapshotsInventarioAsync();
        _initialized = true;
    }

    public async Task<IEnumerable<SnapshotInventario>> GetAllAsync()
    {
        await InitializeAsync();
        using var context = CreateContext();
        return await context.SnapshotsInventario
            .Select(e => e.ToSnapshotInventario())
            .ToListAsync();
    }

    public async Task<Result<SnapshotInventario, DomainError>> GetByFechaAsync(DateOnly fecha)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.SnapshotsInventario.FirstOrDefaultAsync(e => e.Fecha == fecha);
        if (entity is null)
            return Result.Failure<SnapshotInventario, DomainError>(SnapshotInventarioErrors.NotFound(fecha));
        return Result.Success<SnapshotInventario, DomainError>(entity.ToSnapshotInventario());
    }

    public async Task<Result<SnapshotInventario, DomainError>> CreateAsync(SnapshotInventario snapshot)
    {
        await InitializeAsync();
        try
        {
            using var context = CreateContext();
            var entity = snapshot.ToEntity();
            context.SnapshotsInventario.Add(entity);
            await context.SaveChangesAsync();
            return Result.Success<SnapshotInventario, DomainError>(entity.ToSnapshotInventario());
        }
        catch (Exception ex)
        {
            // Backstop del índice único de Fecha, por si dos llamadas a
            // AsegurarSnapshotDeHoyAsync se solapan casi a la vez.
            Log.Error(ex, "Error al crear el snapshot de inventario (Fecha={Fecha})", snapshot.Fecha);
            return Result.Failure<SnapshotInventario, DomainError>(SnapshotInventarioErrors.DatabaseError(ex.Message));
        }
    }
}
