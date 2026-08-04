using CafeStock.Back.Infrastructure;
using CafeStock.Back.Repositories.Productos.Base;
using CafeStock.Back.Repositories.Proveedores.Base;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.Proveedores;
using CafeStock.Blazor.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddScoped<CafeStock.Blazor.Services.PdfService>();

// Configurar la base de datos
var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cafestock.db");
var connectionString = $"Data Source={dbPath}";

// Registrar dependencias del Back
var provider = DependenciesProvider.BuildServiceProvider(connectionString);
builder.Services.AddSingleton(provider.GetService<IProductoRepository>()!);
builder.Services.AddScoped<IProductoService, CafeStock.Back.Services.Productos.ProductoService>();
builder.Services.AddScoped<CafeStock.Back.Validators.Common.IValidador<CafeStock.Back.Models.Producto>, CafeStock.Back.Validators.Productos.ValidadorProducto>();
builder.Services.AddSingleton(provider.GetService<IProveedorRepository>()!);
builder.Services.AddScoped<IProveedorService, CafeStock.Back.Services.Proveedores.ProveedorService>();
builder.Services.AddScoped<CafeStock.Back.Validators.Common.IValidador<CafeStock.Back.Models.Proveedor>, CafeStock.Back.Validators.Proveedores.ValidadorProveedor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();