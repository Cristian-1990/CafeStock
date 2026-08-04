using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.Base;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Repositories.Proveedores.Base;
using CafeStock.Back.Repositories.Proveedores.EfCore;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.Proveedores;
using CafeStock.Back.Validators.Common;
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
    }

    private static void RegisterRepositories(IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IProductoRepository>(sp =>
            new ProductosEfRepository(connectionString));
        services.AddSingleton<IProveedorRepository>(sp =>
            new ProveedoresEfRepository(connectionString));
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IProveedorService, ProveedorService>();
    }
}