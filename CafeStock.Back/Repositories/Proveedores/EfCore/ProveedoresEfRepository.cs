using CafeStock.Back.Entity;
using CafeStock.Back.Infrastructure;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Proveedores;
using CafeStock.Back.Models;
using CafeStock.Back.Mappers;
using CafeStock.Back.Repositories.Proveedores.Base;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CafeStock.Back.Repositories.Proveedores.EfCore;

public class ProveedoresEfRepository : IProveedorRepository
{
    private readonly string _connectionString;

    public ProveedoresEfRepository(string connection)
    {
        _connectionString = connection;
    }

    private AppDbContext CreateContext() => new AppDbContext(_connectionString);

    // El esquema lo gestiona DatabaseInitializer (EF Core Migrations); es idempotente y se
    // ejecuta una sola vez por base de datos aunque lo llamen los seis repositorios.
    private Task InitializeAsync() => DatabaseInitializer.EnsureInitializedAsync(_connectionString);

    public async Task<IEnumerable<Proveedor>> GetAllAsync()
    {
        await InitializeAsync();
        using var context = CreateContext();
        return await context.Proveedores
            .Select(e => e.ToProveedor())
            .ToListAsync();
    }

    public async Task<Result<Proveedor, DomainError>> GetByIdAsync(int id)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Proveedores.FindAsync(id);
        if (entity is null)
            return Result.Failure<Proveedor, DomainError>(ProveedorErrors.NotFound(id));
        return Result.Success<Proveedor, DomainError>(entity.ToProveedor());
    }

    public async Task<Result<Proveedor, DomainError>> CreateAsync(Proveedor proveedor)
    {
        await InitializeAsync();
        try
        {
            using var context = CreateContext();
            var entity = proveedor.ToEntity();
            context.Proveedores.Add(entity);
            await context.SaveChangesAsync();
            return Result.Success<Proveedor, DomainError>(entity.ToProveedor());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al crear el proveedor {Nombre}", proveedor.Nombre);
            return Result.Failure<Proveedor, DomainError>(ProveedorErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<Result<Proveedor, DomainError>> UpdateAsync(int id, Proveedor proveedor)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Proveedores.FindAsync(id);
        if (entity is null)
            return Result.Failure<Proveedor, DomainError>(ProveedorErrors.NotFound(id));
        try
        {
            entity.Nombre = proveedor.Nombre;
            entity.ImagenUrl = proveedor.ImagenUrl;
            entity.Telefono = proveedor.Telefono;
            entity.PersonaContacto = proveedor.PersonaContacto;
            entity.Email = proveedor.Email;
            entity.DiaReparto = proveedor.DiaReparto;
            entity.Notas = proveedor.Notas;
            entity.Direccion = proveedor.Direccion;
            entity.NifCif = proveedor.NifCif;
            entity.EsSupermercadoGenerico = proveedor.EsSupermercadoGenerico;
            entity.AgruparPorTipoUnidad = proveedor.AgruparPorTipoUnidad;
            entity.PrecioUnitarioAltaPrecision = proveedor.PrecioUnitarioAltaPrecision;
            // Un formulario que no conozca el color lo manda vacío: en ese caso se conserva el actual.
            if (!string.IsNullOrWhiteSpace(proveedor.Color))
                entity.Color = proveedor.Color;
            await context.SaveChangesAsync();
            return Result.Success<Proveedor, DomainError>(entity.ToProveedor());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al editar el proveedor {Id}", id);
            return Result.Failure<Proveedor, DomainError>(ProveedorErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<Result<Proveedor, DomainError>> DeleteAsync(int id)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Proveedores.FindAsync(id);
        if (entity is null)
            return Result.Failure<Proveedor, DomainError>(ProveedorErrors.NotFound(id));
        try
        {
            context.Proveedores.Remove(entity);
            await context.SaveChangesAsync();
            return Result.Success<Proveedor, DomainError>(entity.ToProveedor());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al eliminar el proveedor {Id}", id);
            return Result.Failure<Proveedor, DomainError>(ProveedorErrors.DatabaseError(ex.Message));
        }
    }
}
