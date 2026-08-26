using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Compras.Base;
using CafeStock.Back.Repositories.Compras.EfCore;
using CafeStock.Back.Repositories.Presupuestos.Base;
using CafeStock.Back.Repositories.Presupuestos.EfCore;
using CafeStock.Back.Repositories.Productos.Base;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Repositories.Proveedores.Base;
using CafeStock.Back.Repositories.Proveedores.EfCore;
using CafeStock.Back.Repositories.RegistrosCafe.Base;
using CafeStock.Back.Repositories.RegistrosCafe.EfCore;
using CafeStock.Back.Repositories.SnapshotsInventario.Base;
using CafeStock.Back.Repositories.SnapshotsInventario.EfCore;
using CafeStock.Back.Services.Compras;
using CafeStock.Back.Services.Facturas;
using CafeStock.Back.Services.Presupuestos;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.Proveedores;
using CafeStock.Back.Services.RegistrosCafe;
using CafeStock.Back.Services.SnapshotsInventario;
using CafeStock.Back.Validators.Common;
using CafeStock.Back.Validators.Compras;
using CafeStock.Back.Validators.Presupuestos;
using CafeStock.Back.Validators.Productos;
using CafeStock.Back.Validators.Proveedores;
using Microsoft.Extensions.DependencyInjection;

namespace CafeStock.Back.Infrastructure;

public static class DependenciesProvider
{
    public static IServiceProvider BuildServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();

        RegisterValidators(services);
        RegisterRepositories(services, connectionString);
        RegisterServices(services);

        return services.BuildServiceProvider();
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        services.AddTransient<IValidador<Producto>, ValidadorProducto>();
        services.AddTransient<IValidador<Proveedor>, ValidadorProveedor>();
        services.AddTransient<IValidador<Compra>, ValidadorCompra>();
        services.AddTransient<IValidador<Presupuesto>, ValidadorPresupuesto>();
    }

    private static void RegisterRepositories(IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IProductoRepository>(sp =>
            new ProductosEfRepository(connectionString));
        services.AddSingleton<IProveedorRepository>(sp =>
            new ProveedoresEfRepository(connectionString));
        services.AddSingleton<ICompraRepository>(sp =>
            new ComprasEfRepository(connectionString));
        services.AddSingleton<IRegistroCafeRepository>(sp =>
            new RegistrosCafeEfRepository(connectionString));
        services.AddSingleton<IPresupuestoRepository>(sp =>
            new PresupuestosEfRepository(connectionString));
        services.AddSingleton<ISnapshotInventarioRepository>(sp =>
            new SnapshotsInventarioEfRepository(connectionString));
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IProveedorService, ProveedorService>();
        // CompraService depende de IProductoService (sincroniza Producto.PrecioUnitario tras
        // registrar una compra) — debe registrarse después.
        services.AddScoped<ICompraService, CompraService>();
        services.AddScoped<IFacturaService, FacturaService>();
        services.AddScoped<IRegistroCafeService, RegistroCafeService>();
        services.AddScoped<IPresupuestoService, PresupuestoService>();
        services.AddScoped<ISnapshotInventarioService, SnapshotInventarioService>();
    }
}