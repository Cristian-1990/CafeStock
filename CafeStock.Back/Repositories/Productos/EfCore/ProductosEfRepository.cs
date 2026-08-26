using CafeStock.Back.Entity;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Productos;
using CafeStock.Back.Models;
using CafeStock.Back.Mappers;
using CafeStock.Back.Repositories.Productos.Base;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CafeStock.Back.Repositories.Productos.EfCore;

public class ProductosEfRepository : IProductoRepository
{
    private readonly string _connectionString;
    private bool _initialized;

    public ProductosEfRepository(string connection)
    {
        _connectionString = connection;
    }

    private AppDbContext CreateContext() => new AppDbContext(_connectionString);

    private async Task InitializeAsync()
    {
        if (_initialized) return;
        using var context = CreateContext();
        await context.EnsureCreatedAsync();
        await context.AsegurarColumnaPrecioUnitarioAsync();
        await context.AsegurarColumnaSeguimientoIndividualAsync();
        await context.AsegurarSeguimientoIndividualCafePucheroAsync();
        _initialized = true;
    }
    
    
    
    public async Task<IEnumerable<Producto>> GetAllAsync()
    {
        await InitializeAsync();
        using var context = CreateContext();
        return await context.Productos
            .Select(e => e.ToProducto())
            .ToListAsync();
    }

    public async Task<Result<Producto, DomainError>> GetByIdAsync(int id)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Productos.FindAsync(id);
        if (entity is null)
            return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));
        return Result.Success<Producto, DomainError>(entity.ToProducto());
    }

    public async Task<Result<Producto, DomainError>> CreateAsync(Producto producto)
    {
        await InitializeAsync();
        try
        {
            using var context = CreateContext();
            var entity = producto.ToEntity();
            context.Productos.Add(entity);
            await context.SaveChangesAsync();
            return Result.Success<Producto, DomainError>(entity.ToProducto());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al crear el producto {Nombre}", producto.Nombre);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }

    /// <summary>
    /// No copia SeguimientoIndividual a propósito: es un interruptor que solo activa la
    /// migración de datos (nunca el formulario de edición, que ni lo muestra), así que
    /// UpdateAsync no debe poder resetearlo sin querer al editar cualquier otro campo.
    /// </summary>
    public async Task<Result<Producto, DomainError>> UpdateAsync(int id, Producto producto)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Productos.FindAsync(id);
        if (entity is null)
            return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));
        try
        {
            entity.Nombre = producto.Nombre;
            entity.StockActual = producto.StockActual;
            entity.StockMaximo = producto.StockMaximo;
            entity.Unidad = producto.Unidad;
            entity.Descripcion = producto.Descripcion;
            entity.ImagenUrl = producto.ImagenUrl;
            entity.ProveedorId = producto.ProveedorId;
            entity.PrecioUnitario = producto.PrecioUnitario;
            await context.SaveChangesAsync();
            return Result.Success<Producto, DomainError>(entity.ToProducto());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al editar el producto {Id}", id);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<Result<Producto, DomainError>> DeleteAsync(int id)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Productos.FindAsync(id);
        if (entity is null)
            return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));
        try
        {
            context.Productos.Remove(entity);
            await context.SaveChangesAsync();
            return Result.Success<Producto, DomainError>(entity.ToProducto());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al eliminar el producto {Id}", id);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }



    public async Task<Result<Producto, DomainError>> ConfirmarRecepcionAsync(int id, int cantidadRecibida)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Productos.FindAsync(id);
        if (entity is null)
            return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));
        try
        {
            entity.StockActual += cantidadRecibida;
            await context.SaveChangesAsync();
            return Result.Success<Producto, DomainError>(entity.ToProducto());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al confirmar la recepción del producto {Id}", id);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<IEnumerable<Producto>> ProductosUrgentes()
    {
        await InitializeAsync();
        using var context = CreateContext();
        return await context.Productos
            .Where(e => e.StockActual < e.StockMaximo)
            .Select(e => e.ToProducto())
            .ToListAsync();
    }

    public async Task<Result<Producto, DomainError>> ActualizarStockActualAsync(int id, int nuevaCantidad)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Productos.FindAsync(id);
        if (entity is null)
            return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));
        try
        {
            entity.StockActual = nuevaCantidad;
            await context.SaveChangesAsync();
            return Result.Success<Producto, DomainError>(entity.ToProducto());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al actualizar el stock actual del producto {Id}", id);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }

    public async Task<Result<Producto, DomainError>> ActualizarPrecioUnitarioAsync(int id, decimal nuevoPrecio)
    {
        await InitializeAsync();
        using var context = CreateContext();
        var entity = await context.Productos.FindAsync(id);
        if (entity is null)
            return Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(id));
        try
        {
            entity.PrecioUnitario = nuevoPrecio;
            await context.SaveChangesAsync();
            return Result.Success<Producto, DomainError>(entity.ToProducto());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al actualizar el precio unitario del producto {Id}", id);
            return Result.Failure<Producto, DomainError>(ProductoErrors.DatabaseError(ex.Message));
        }
    }
}