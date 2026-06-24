using CafeStock.Back.Infrastructure;
using CafeStock.Back.Repositories.Productos.Base;
using CafeStock.Back.Services.Productos;
using CafeStock.Blazor.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configurar la base de datos
var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cafestock.db");
var connectionString = $"Data Source={dbPath}";

// Registrar dependencias del Back
var provider = DependenciesProvider.BuildServiceProvider(connectionString);
builder.Services.AddSingleton(provider.GetService<IProductoRepository>()!);
builder.Services.AddScoped<IProductoService, CafeStock.Back.Services.Productos.ProductoService>();
builder.Services.AddScoped<CafeStock.Back.Validators.Common.IValidador<CafeStock.Back.Models.Producto>, CafeStock.Back.Validators.Productos.ValidadorProducto>();

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