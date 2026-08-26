using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Productos;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Compras.Base;
using CafeStock.Back.Services.Compras;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Validators.Compras;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;

namespace CafeStock.Tests.Services;

[TestFixture]
public class CompraServiceTest
{
    private Mock<ICompraRepository> _repositoryMock;
    private Mock<IProductoService> _productoServiceMock;
    private CompraService _service;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<ICompraRepository>();
        _productoServiceMock = new Mock<IProductoService>();
        _service = new CompraService(_repositoryMock.Object, new ValidadorCompra(), _productoServiceMock.Object);
    }

    private static Compra CompraConLinea(int productoId, decimal precioUnitario) => new()
    {
        Id = 1,
        ProveedorId = 1,
        Fecha = DateTime.Now,
        Lineas = [new LineaCompra { ProductoId = productoId, Cantidad = 2, PrecioUnitario = precioUnitario }]
    };

    [Test]
    public async Task CreateAsync_PrecioLineaDistintoDelPrecioDeReferencia_ActualizaElPrecioDelProducto()
    {
        // Arrange: el producto tiene 4.00€ de referencia, pero la línea recibida trae 4.50€
        var compra = CompraConLinea(productoId: 1, precioUnitario: 4.5m);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Compra>()))
            .ReturnsAsync(Result.Success<Compra, DomainError>(compra));
        _productoServiceMock
            .Setup(s => s.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Producto, DomainError>(new Producto { Id = 1, Nombre = "Café", PrecioUnitario = 4.0m }));
        _productoServiceMock
            .Setup(s => s.ActualizarPrecioUnitarioAsync(1, 4.5m))
            .ReturnsAsync(Result.Success<Producto, DomainError>(new Producto { Id = 1, Nombre = "Café", PrecioUnitario = 4.5m }));

        // Act
        var resultado = await _service.CreateAsync(compra);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _productoServiceMock.Verify(s => s.ActualizarPrecioUnitarioAsync(1, 4.5m), Times.Once);
    }

    [Test]
    public async Task CreateAsync_PrecioLineaIgualAlPrecioDeReferencia_NoActualizaElPrecioDelProducto()
    {
        // Arrange: la línea trae exactamente el mismo precio que ya tiene el producto
        var compra = CompraConLinea(productoId: 1, precioUnitario: 4.0m);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Compra>()))
            .ReturnsAsync(Result.Success<Compra, DomainError>(compra));
        _productoServiceMock
            .Setup(s => s.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Producto, DomainError>(new Producto { Id = 1, Nombre = "Café", PrecioUnitario = 4.0m }));

        // Act
        var resultado = await _service.CreateAsync(compra);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _productoServiceMock.Verify(s => s.ActualizarPrecioUnitarioAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_NoEncuentraElProducto_NoActualizaYNoTumbaLaCompraYaGuardada()
    {
        // Arrange: la compra se guardó bien, pero el producto ya no existe al sincronizar
        var compra = CompraConLinea(productoId: 99, precioUnitario: 4.5m);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Compra>()))
            .ReturnsAsync(Result.Success<Compra, DomainError>(compra));
        _productoServiceMock
            .Setup(s => s.GetByIdAsync(99))
            .ReturnsAsync(Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(99)));

        // Act
        var resultado = await _service.CreateAsync(compra);

        // Assert: la compra sigue siendo un éxito pese a que la sincronización no pudo hacerse
        resultado.IsSuccess.Should().BeTrue();
        _productoServiceMock.Verify(s => s.ActualizarPrecioUnitarioAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_CompraInvalida_NoLlamaAlRepositorioNiSincronizaPrecio()
    {
        // Arrange: sin líneas, la ValidadorCompra real la rechaza antes de llegar al repositorio
        var compra = new Compra { ProveedorId = 1, Fecha = DateTime.Now, Lineas = [] };

        // Act
        var resultado = await _service.CreateAsync(compra);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Compra>()), Times.Never);
        _productoServiceMock.Verify(s => s.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }
}
