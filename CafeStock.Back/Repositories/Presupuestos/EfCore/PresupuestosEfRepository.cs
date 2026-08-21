using CafeStock.Back.Entity;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Presupuestos;
using CafeStock.Back.Mappers;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Presupuestos.Base;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CafeStock.Back.Repositories.Presupuestos.EfCore;

public class PresupuestosEfRepository : IPresupuestoRepository
{
    private readonly string _connectionString;
    private bool _initialized;

    public PresupuestosEfRepository(string connection)
    {
        _connectionString = connection;
    }

    private AppDbContext CreateContext() => new AppDbContext(_connectionString);

    private async Task InitializeAsync()
    {
        if (_initialized) return;
        using var context = CreateContext();
        await context.EnsureCreatedAsync();
        await context.AsegurarTablaPresupuestosAsync();
        _initialized = true;
    }

    public async Task<IEnumerable<Presupuesto>> GetAllAsync()
    {
        await InitializeAsync();
        using var context = CreateContext();
        return await context.Presupuestos
            .Select(e => e.ToPresupuesto())
            .ToListAsync();
    }

    public async Task<IEnumerable<Presupuesto>> GetByProveedorAsync(int proveedorId)
    {
        await InitializeAsync();
        using var context = CreateContext();
        return await context.Presupuestos
            .Where(e => e.ProveedorId == proveedorId)
            .Select(e => e.ToPresupuesto())
            .ToListAsync();
    }

    public async Task<Result<Presupuesto, DomainError>> GetByProveedorMesAnioAsync(int proveedorId, int mes, int anio)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Presupuestos
            .FirstOrDefaultAsync(e => e.ProveedorId == proveedorId && e.Mes == mes && e.Anio == anio);
        if (entity is null)
            return Result.Failure<Presupuesto, DomainError>(PresupuestoErrors.NoAsignadoParaMes(proveedorId, mes, anio));
        return Result.Success<Presupuesto, DomainError>(entity.ToPresupuesto());
    }

    public async Task<Result<Presupuesto, DomainError>> CreateAsync(Presupuesto presupuesto)
    {
        await InitializeAsync();
        using var context = CreateContext();

        var proveedor = await context.Proveedores.FindAsync(presupuesto.ProveedorId);
        if (proveedor is null)
            return Result.Failure<Presupuesto, DomainError>(PresupuestoErrors.ProveedorNoEncontrado(presupuesto.ProveedorId));

        var yaExiste = await context.Presupuestos.AnyAsync(e =>
            e.ProveedorId == presupuesto.ProveedorId && e.Mes == presupuesto.Mes && e.Anio == presupuesto.Anio);
        if (yaExiste)
            return Result.Failure<Presupuesto, DomainError>(
                PresupuestoErrors.Duplicado(presupuesto.ProveedorId, presupuesto.Mes, presupuesto.Anio));

        try
        {
            var entity = presupuesto.ToEntity();
            context.Presupuestos.Add(entity);
            await context.SaveChangesAsync();
            return Result.Success<Presupuesto, DomainError>(entity.ToPresupuesto());
        }
        catch (Exception ex)
        {
            // Backstop del índice único (UNIQUE en ProveedorId+Mes+Anio) por si dos peticiones
            // llegan casi a la vez y ambas pasan la comprobación AnyAsync de arriba.
            Log.Error(ex, "Error al crear el presupuesto (ProveedorId={ProveedorId}, Mes={Mes}, Anio={Anio})",
                presupuesto.ProveedorId, presupuesto.Mes, presupuesto.Anio);
            return Result.Failure<Presupuesto, DomainError>(PresupuestoErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<Result<Presupuesto, DomainError>> ActualizarImporteAsync(int id, decimal nuevoImporte)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Presupuestos.FindAsync(id);
        if (entity is null)
            return Result.Failure<Presupuesto, DomainError>(PresupuestoErrors.NotFound(id));
        try
        {
            entity.ImporteAsignado = nuevoImporte;
            await context.SaveChangesAsync();
            return Result.Success<Presupuesto, DomainError>(entity.ToPresupuesto());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al actualizar el importe del presupuesto {Id}", id);
            return Result.Failure<Presupuesto, DomainError>(PresupuestoErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<Result<Presupuesto, DomainError>> DeleteAsync(int id)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Presupuestos.FindAsync(id);
        if (entity is null)
            return Result.Failure<Presupuesto, DomainError>(PresupuestoErrors.NotFound(id));
        try
        {
            context.Presupuestos.Remove(entity);
            await context.SaveChangesAsync();
            return Result.Success<Presupuesto, DomainError>(entity.ToPresupuesto());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al eliminar el presupuesto {Id}", id);
            return Result.Failure<Presupuesto, DomainError>(PresupuestoErrors.DatabaseError(ex.Message));
        }
    }
}
