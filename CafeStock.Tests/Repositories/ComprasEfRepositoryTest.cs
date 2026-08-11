using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Compras.EfCore;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Repositories.Proveedores.EfCore;
using FluentAssertions;

namespace CafeStock.Tests.Repositories;

[TestFixture]
public class ComprasEfRepositoryTest
{
    private string _dbPath;
    private string _connectionString;
    private ComprasEfRepository _repository;
    private ProductosEfRepository _productoRepository;
    private ProveedoresEfRepository _proveedorRepository;

    [SetUp]
    public void Setup()
    {
        // Base de datos temporal única por test
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_dbPath}";
        _repository = new ComprasEfRepository(_connectionString);
        _productoRepository = new ProductosEfRepository(_connectionString);
        _proveedorRepository = new ProveedoresEfRepository(_connectionString);
    }

    [TearDown]
    public void TearDown()
    {
        // Fuerza a SQLite a soltar el archivo antes de borrarlo
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Test]
    public async Task CreateAsync_GuardaCompraConSusLineas()
    {
        // Arrange
        var proveedor = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Alcampo" });
        var producto = await _productoRepository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5, ProveedorId = proveedor.Value.Id });

        var compra = new Compra
        {
            ProveedorId = proveedor.Value.Id,
            Fecha = DateTime.Now,
            Lineas = [new LineaCompra { ProductoId = producto.Value.Id, Cantidad = 3, PrecioUnitario = 4.5m }]
        };

        // Act
        var resultado = await _repository.CreateAsync(compra);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().BeGreaterThan(0);
        resultado.Value.Lineas.Should().ContainSingle();
        resultado.Value.Lineas.Single().Cantidad.Should().Be(3);
        resultado.Value.Lineas.Single().PrecioUnitario.Should().Be(4.5m);
    }

    [Test]
    public async Task CreateAsync_SinProveedor_GuardaConProveedorIdNulo()
    {
        // Arrange
        var producto = await _productoRepository.CreateAsync(
            new Producto { Nombre = "Vasos de cartón", StockActual = 20, StockMaximo = 100 });

        var compra = new Compra
        {
            ProveedorId = null,
            Fecha = DateTime.Now,
            Lineas = [new LineaCompra { ProductoId = producto.Value.Id, Cantidad = 80, PrecioUnitario = 0.05m }]
        };

        // Act
        var resultado = await _repository.CreateAsync(compra);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.ProveedorId.Should().BeNull();
    }

    [Test]
    public async Task GetAllAsync_DevuelveLasComprasConSusLineas()
    {
        // Arrange
        var producto = await _productoRepository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });
        await _repository.CreateAsync(new Compra
        {
            Fecha = DateTime.Now,
            Lineas = [new LineaCompra { ProductoId = producto.Value.Id, Cantidad = 3, PrecioUnitario = 4.5m }]
        });

        // Act
        var compras = await _repository.GetAllAsync();

        // Assert
        compras.Should().ContainSingle();
        compras.Single().Lineas.Should().ContainSingle();
    }
}
