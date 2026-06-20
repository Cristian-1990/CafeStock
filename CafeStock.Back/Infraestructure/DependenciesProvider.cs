using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.Base;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Validators.Common;
using CafeStock.Back.Validators.Productos;
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
    }

    private static void RegisterRepositories(IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IProductoRepository>(sp =>
            new ProductosEfRepository(connectionString));
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IProductoService, ProductoService>();
    }
}