using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Repositories.Proveedores.EfCore;
using FluentAssertions;

namespace CafeStock.Tests.Repositories;

[TestFixture]
public class ProductosEfRepositoryTest
{
    private string _dbPath;
    private string _connectionString;
    private ProductosEfRepository _repository;

    [SetUp]
    public void Setup()
    {
        // Base de datos temporal única por test
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_dbPath}";
        _repository = new ProductosEfRepository(_connectionString);
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
    public async Task CreateAsync_GuardaProducto()
    {
        // Arrange
        var producto = new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 };

        // Act
        var resultado = await _repository.CreateAsync(producto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetByIdAsync_ProductoExiste_LoDevuelve()
    {
        // Arrange
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });

        // Act
        var resultado = await _repository.GetByIdAsync(creado.Value.Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Nombre.Should().Be("Café");
    }

    [Test]
    public async Task GetByIdAsync_ProductoNoExiste_DevuelveFailure()
    {
        // Act
        var resultado = await _repository.GetByIdAsync(999);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task GetAllAsync_DevuelveTodos()
    {
        // Arrange
        await _repository.CreateAsync(new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });
        await _repository.CreateAsync(new Producto { Nombre = "Azúcar", StockActual = 1, StockMaximo = 3 });

        // Act
        var productos = await _repository.GetAllAsync();

        // Assert
        productos.Should().HaveCount(2);
    }

    [Test]
    public async Task UpdateAsync_ModificaProducto()
    {
        // Arrange
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });
        var modificado = creado.Value with { StockActual = 4 };

        // Act
        var resultado = await _repository.UpdateAsync(creado.Value.Id, modificado);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.StockActual.Should().Be(4);
    }

    [Test]
    public async Task DeleteAsync_EliminaProducto()
    {
        // Arrange
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });

        // Act
        var resultado = await _repository.DeleteAsync(creado.Value.Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        var comprobar = await _repository.GetByIdAsync(creado.Value.Id);
        comprobar.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ConfirmarRecepcionAsync_RecepcionCompleta_SumaTodoLoPedidoYNoQuedaDeficit()
    {
        // Arrange: faltan 3 (StockActual=2, StockMaximo=5)
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });

        // Act: llega todo lo que faltaba
        var resultado = await _repository.ConfirmarRecepcionAsync(creado.Value.Id, 3);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.StockActual.Should().Be(5);
        resultado.Value.CantidadAComprar.Should().Be(0);
    }

    [Test]
    public async Task ConfirmarRecepcionAsync_RecepcionParcial_SumaSoloLoRecibidoYQuedaDeficit()
    {
        // Arrange: faltan 3 (StockActual=2, StockMaximo=5)
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });

        // Act: pedían 3, llegan solo 2
        var resultado = await _repository.ConfirmarRecepcionAsync(creado.Value.Id, 2);

        // Assert: StockActual = 2 + 2 = 4, sigue faltando 1
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.StockActual.Should().Be(4);
        resultado.Value.CantidadAComprar.Should().Be(1);
    }

    [Test]
    public async Task ActualizarPrecioUnitarioAsync_ModificaSoloElPrecio()
    {
        // Arrange
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5, PrecioUnitario = 4.0m });

        // Act
        var resultado = await _repository.ActualizarPrecioUnitarioAsync(creado.Value.Id, 4.5m);

        // Assert: el precio cambia, el resto de campos queda intacto
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.PrecioUnitario.Should().Be(4.5m);
        resultado.Value.StockActual.Should().Be(2);
        resultado.Value.Nombre.Should().Be("Café");
    }

    [Test]
    public async Task ActualizarPrecioUnitarioAsync_ProductoNoExiste_DevuelveFailure()
    {
        // Act
        var resultado = await _repository.ActualizarPrecioUnitarioAsync(999, 4.5m);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ProductosUrgentes_DevuelveSoloBajoMinimo()
    {
        // Arrange
        await _repository.CreateAsync(new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });   // falta
        await _repository.CreateAsync(new Producto { Nombre = "Sal", StockActual = 5, StockMaximo = 5 });    // completo

        // Act
        var urgentes = await _repository.ProductosUrgentes();

        // Assert
        urgentes.Should().HaveCount(1);
        urgentes.First().Nombre.Should().Be("Café");
    }

    [Test]
    public async Task Initialize_ProveedorPucheroConDosBrasil_ActivaSeguimientoIndividualYRenombra()
    {
        // Arrange: los dos cafés de Puchero, tal y como están guardados hoy (mismo nombre,
        // distinta unidad). Se crean con instancias de repositorio "de usar y tirar", distintas
        // de _repository, para que su propio InitializeAsync no dispare la migración antes de
        // que estos datos existan (igual que en un arranque real: los datos ya están en el
        // .db, y la migración corre en la primera llamada tras el arranque de la app).
        var proveedoresRepo = new ProveedoresEfRepository(_connectionString);
        var puchero = await proveedoresRepo.CreateAsync(new Proveedor { Nombre = "Puchero" });
        var productosSeed = new ProductosEfRepository(_connectionString);
        await productosSeed.CreateAsync(new Producto
        {
            Nombre = "Brasil", Unidad = UnidadMedida.Kg, StockActual = 12, StockMaximo = 12, ProveedorId = puchero.Value.Id
        });
        await productosSeed.CreateAsync(new Producto
        {
            Nombre = "Brasil", Unidad = UnidadMedida.Unidades, StockActual = 4, StockMaximo = 4, ProveedorId = puchero.Value.Id
        });

        // Act: _repository (fresca, _initialized aún en false) dispara la migración en su
        // primera llamada — como al arrancar la app de verdad.
        var productos = (await _repository.GetAllAsync()).ToList();

        // Assert
        var grano = productos.Single(p => p.Unidad == UnidadMedida.Kg);
        grano.Nombre.Should().Be("Brasil — Grano 1kg");
        grano.SeguimientoIndividual.Should().BeTrue();

        var cuarto = productos.Single(p => p.Unidad == UnidadMedida.Unidades);
        cuarto.Nombre.Should().Be("Brasil — Cuarto 250g");
        cuarto.SeguimientoIndividual.Should().BeTrue();
    }

    [Test]
    public async Task Initialize_SinProveedorPuchero_NoActivaNiRenombraNada()
    {
        // Arrange: un producto que por casualidad también se llama "Brasil" pero no es de
        // Puchero (o Puchero no existe todavía) no debe verse afectado
        await _repository.CreateAsync(new Producto { Nombre = "Brasil", StockActual = 1, StockMaximo = 1 });

        // Act
        var productos = (await _repository.GetAllAsync()).ToList();

        // Assert
        productos.Single().Nombre.Should().Be("Brasil");
        productos.Single().SeguimientoIndividual.Should().BeFalse();
    }
}