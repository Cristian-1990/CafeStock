using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Productos;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.Base;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Validators.Productos;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;

namespace CafeStock.Tests.Services;

[TestFixture]
public class ProductoServiceTest
{
    private Mock<IProductoRepository> _repositoryMock;
    private ProductoService _service;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<IProductoRepository>();
        _service = new ProductoService(_repositoryMock.Object, new ValidadorProducto());
    }

    [Test]
    public async Task CreateAsync_ProductoValido_LlamaAlRepositorio()
    {
        // Arrange
        var producto = new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 };
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(Result.Success<Producto, DomainError>(producto));

        // Act
        var resultado = await _service.CreateAsync(producto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Producto>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_ProductoInvalido_NoLlamaAlRepositorio()
    {
        // Arrange
        var producto = new Producto { Nombre = "", StockActual = 2, StockMaximo = 5 };

        // Act
        var resultado = await _service.CreateAsync(producto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Producto>()), Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_ProductoExiste_DevuelveProducto()
    {
        // Arrange
        var producto = new Producto { Id = 1, Nombre = "Café", StockActual = 2, StockMaximo = 5 };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Producto, DomainError>(producto));

        // Act
        var resultado = await _service.GetByIdAsync(1);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Nombre.Should().Be("Café");
    }

    [Test]
    public async Task GetByIdAsync_ProductoNoExiste_DevuelveFailure()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(99))
            .ReturnsAsync(Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(99)));

        // Act
        var resultado = await _service.GetByIdAsync(99);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ConfirmarRecepcionAsync_CantidadCero_NoLlamaAlRepositorio()
    {
        // Act
        var resultado = await _service.ConfirmarRecepcionAsync(1, 0);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(r => r.ConfirmarRecepcionAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task ConfirmarRecepcionAsync_CantidadNegativa_NoLlamaAlRepositorio()
    {
        // Act
        var resultado = await _service.ConfirmarRecepcionAsync(1, -1);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(r => r.ConfirmarRecepcionAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task ConfirmarRecepcionAsync_CantidadValida_LlamaAlRepositorio()
    {
        // Arrange
        var producto = new Producto { Id = 1, Nombre = "Café", StockActual = 4, StockMaximo = 5 };
        _repositoryMock
            .Setup(r => r.ConfirmarRecepcionAsync(1, 2))
            .ReturnsAsync(Result.Success<Producto, DomainError>(producto));

        // Act
        var resultado = await _service.ConfirmarRecepcionAsync(1, 2);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.ConfirmarRecepcionAsync(1, 2), Times.Once);
    }

    [Test]
    public async Task ActualizarPrecioUnitarioAsync_PrecioValido_LlamaAlRepositorio()
    {
        // Arrange
        var producto = new Producto { Id = 1, Nombre = "Café", StockActual = 4, StockMaximo = 5, PrecioUnitario = 4.5m };
        _repositoryMock
            .Setup(r => r.ActualizarPrecioUnitarioAsync(1, 4.5m))
            .ReturnsAsync(Result.Success<Producto, DomainError>(producto));

        // Act
        var resultado = await _service.ActualizarPrecioUnitarioAsync(1, 4.5m);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.ActualizarPrecioUnitarioAsync(1, 4.5m), Times.Once);
    }

    [Test]
    public async Task ActualizarPrecioUnitarioAsync_PrecioCero_NoLlamaAlRepositorio()
    {
        // Act
        var resultado = await _service.ActualizarPrecioUnitarioAsync(1, 0m);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(r => r.ActualizarPrecioUnitarioAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Test]
    public async Task ActualizarPrecioUnitarioAsync_PrecioNegativo_NoLlamaAlRepositorio()
    {
        // Act
        var resultado = await _service.ActualizarPrecioUnitarioAsync(1, -1m);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(r => r.ActualizarPrecioUnitarioAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Test]
    public async Task GetAllAsync_DevuelveTodosLosProductos()
    {
        // Arrange
        var productos = new List<Producto>
        {
            new() { Nombre = "Café", StockActual = 2, StockMaximo = 5 },
            new() { Nombre = "Azúcar", StockActual = 1, StockMaximo = 3 }
        };
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(productos);

        // Act
        var resultado = await _service.GetAllAsync();

        // Assert
        resultado.Should().HaveCount(2);
    }
}