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
    public async Task UpdateAsync_ModificaPasillo()
    {
        // Arrange
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5, Pasillo = null });
        var modificado = creado.Value with { Pasillo = 3 };

        // Act
        var resultado = await _repository.UpdateAsync(creado.Value.Id, modificado);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Pasillo.Should().Be(3);
    }

    [Test]
    public async Task UpdateAsync_ModificaSeccion()
    {
        // Arrange
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Tomate", StockActual = 2, StockMaximo = 5, Seccion = SeccionEspecial.Ninguna });
        var modificado = creado.Value with { Seccion = SeccionEspecial.Fruteria };

        // Act
        var resultado = await _repository.UpdateAsync(creado.Value.Id, modificado);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Seccion.Should().Be(SeccionEspecial.Fruteria);
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
    public async Task AjustarStockActualAsync_DeltaPositivo_SumaAlStockExistente()
    {
        // Arrange
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 5, StockMaximo = 10 });

        // Act
        var resultado = await _repository.AjustarStockActualAsync(creado.Value.Id, 3);

        // Assert: el resto de campos queda intacto
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.StockActual.Should().Be(8);
        resultado.Value.Nombre.Should().Be("Café");
    }

    [Test]
    public async Task AjustarStockActualAsync_DeltaNegativo_RestaDelStockExistente()
    {
        // Arrange
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 5, StockMaximo = 10 });

        // Act
        var resultado = await _repository.AjustarStockActualAsync(creado.Value.Id, -2);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.StockActual.Should().Be(3);
    }

    [Test]
    public async Task AjustarStockActualAsync_DeltaDejaElStockNegativo_DevuelveFailureYNoModifica()
    {
        // Arrange
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 10 });

        // Act
        var resultado = await _repository.AjustarStockActualAsync(creado.Value.Id, -5);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        var recargado = await _repository.GetByIdAsync(creado.Value.Id);
        recargado.Value.StockActual.Should().Be(2);
    }

    [Test]
    public async Task AjustarStockActualAsync_ProductoNoExiste_DevuelveFailure()
    {
        // Act
        var resultado = await _repository.AjustarStockActualAsync(999, 1);

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
    public async Task ConfirmarRecepcionAsync_RecepcionesSimultaneas_NoPierdeNingunaSuma()
    {
        // Arrange: 15 recepciones de 2 unidades a la vez sobre el mismo producto.
        var creado = await _repository.CreateAsync(new Producto { Nombre = "Leche", StockActual = 0, StockMaximo = 100 });
        var id = creado.Value.Id;

        // Act
        var salida = new TaskCompletionSource();
        var tareas = Enumerable.Range(0, 15).Select(async _ =>
        {
            var repo = new ProductosEfRepository(_connectionString);
            await salida.Task;
            return await repo.ConfirmarRecepcionAsync(id, 2);
        }).ToList();
        salida.SetResult();
        var resultados = await Task.WhenAll(tareas);

        // Assert
        resultados.Should().OnlyContain(r => r.IsSuccess);
        (await _repository.GetByIdAsync(id)).Value.StockActual.Should().Be(30);
    }

    [Test]
    public async Task AjustarStockActualAsync_AjustesSimultaneos_RespetaElLimiteDeCero()
    {
        // Arrange: stock 5 y 10 descuentos de 1 a la vez; solo 5 caben, el resto debe fallar
        // por validación (stock < 0) y nunca dejar el stock en negativo.
        var creado = await _repository.CreateAsync(new Producto { Nombre = "Leche", StockActual = 5, StockMaximo = 10 });
        var id = creado.Value.Id;

        // Act
        var salida = new TaskCompletionSource();
        var tareas = Enumerable.Range(0, 10).Select(async _ =>
        {
            var repo = new ProductosEfRepository(_connectionString);
            await salida.Task;
            return await repo.AjustarStockActualAsync(id, -1);
        }).ToList();
        salida.SetResult();
        var resultados = await Task.WhenAll(tareas);

        // Assert
        resultados.Count(r => r.IsSuccess).Should().Be(5);
        (await _repository.GetByIdAsync(id)).Value.StockActual.Should().Be(0);
    }

    [Test]
    public async Task UpdateAsync_StockSinTocar_NoPisaLasVentasHechasConElFormularioAbierto()
    {
        // Arrange: el formulario se abre con stock 10 y, mientras tanto, otra tablet descuenta 3.
        var creado = await _repository.CreateAsync(new Producto { Nombre = "Leche", StockActual = 10, StockMaximo = 20, PrecioUnitario = 1m });
        var id = creado.Value.Id;
        await _repository.AjustarStockActualAsync(id, -3);

        // Act: se guarda un cambio de precio y de nombre, sin haber tocado el stock (10 == lo leído).
        var resultado = await _repository.UpdateAsync(id, creado.Value with
        {
            Nombre = "Leche entera", PrecioUnitario = 2m, StockActual = 10, StockActualAlLeer = 10
        });

        // Assert: el nombre y el precio cambian, pero el stock real (7) se conserva.
        resultado.IsSuccess.Should().BeTrue();
        var guardado = (await _repository.GetByIdAsync(id)).Value;
        guardado.Nombre.Should().Be("Leche entera");
        guardado.PrecioUnitario.Should().Be(2m);
        guardado.StockActual.Should().Be(7);
    }

    [Test]
    public async Task UpdateAsync_StockCambiadoAPropositoPorElUsuario_SiSeAplica()
    {
        // Arrange: corregir un recuento: el formulario se abrió con 10 y el usuario escribe 15.
        var creado = await _repository.CreateAsync(new Producto { Nombre = "Leche", StockActual = 10, StockMaximo = 20 });
        var id = creado.Value.Id;
        await _repository.AjustarStockActualAsync(id, -3); // mientras tanto, una venta

        // Act
        await _repository.UpdateAsync(id, creado.Value with { StockActual = 15, StockActualAlLeer = 10 });

        // Assert: lo que el usuario escribió a propósito manda, aunque hubiera cambios intermedios.
        (await _repository.GetByIdAsync(id)).Value.StockActual.Should().Be(15);
    }

    [Test]
    public async Task UpdateAsync_SinStockActualAlLeer_EscribeElStockComoSiempre()
    {
        // Arrange: quien edita sin informar el stock leído (p. ej. código anterior) conserva el comportamiento de siempre.
        var creado = await _repository.CreateAsync(new Producto { Nombre = "Leche", StockActual = 10, StockMaximo = 20 });
        var id = creado.Value.Id;
        await _repository.AjustarStockActualAsync(id, -3);

        // Act
        await _repository.UpdateAsync(id, creado.Value with { Nombre = "Leche entera", StockActual = 10 });

        // Assert
        (await _repository.GetByIdAsync(id)).Value.StockActual.Should().Be(10);
    }
}
