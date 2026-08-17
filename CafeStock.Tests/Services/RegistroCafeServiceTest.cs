using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.Base;
using CafeStock.Back.Repositories.RegistrosCafe.Base;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.RegistrosCafe;
using CafeStock.Back.Validators.Productos;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;

namespace CafeStock.Tests.Services;

[TestFixture]
public class RegistroCafeServiceTest
{
    private Mock<IRegistroCafeRepository> _repositoryMock;
    private Mock<IProductoRepository> _productoRepositoryMock;
    private RegistroCafeService _service;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<IRegistroCafeRepository>();
        _productoRepositoryMock = new Mock<IProductoRepository>();
        var productoService = new ProductoService(_productoRepositoryMock.Object, new ValidadorProducto());
        _service = new RegistroCafeService(_repositoryMock.Object, productoService);
    }

    [Test]
    public async Task RegistrarVentaAsync_NombreVacio_NoLlamaAlRepositorio()
    {
        // Act
        var resultado = await _service.RegistrarVentaAsync(1, "");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(r => r.RegistrarVentaAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RegistrarVentaAsync_NombreSoloEspacios_NoLlamaAlRepositorio()
    {
        // Act
        var resultado = await _service.RegistrarVentaAsync(1, "   ");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(r => r.RegistrarVentaAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RegistrarVentaAsync_NombreValido_LlamaAlRepositorioConNombreRecortado()
    {
        // Arrange
        var registro = new RegistroCafe { Id = 1, ProductoId = 5, NombreTrabajador = "Ana", Fecha = DateTime.Now };
        _repositoryMock
            .Setup(r => r.RegistrarVentaAsync(5, "Ana"))
            .ReturnsAsync(Result.Success<RegistroCafe, DomainError>(registro));

        // Act
        var resultado = await _service.RegistrarVentaAsync(5, "  Ana  ");

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.RegistrarVentaAsync(5, "Ana"), Times.Once);
    }

    [Test]
    public async Task GetUltimosAsync_ResuelveElNombreDelProducto()
    {
        // Arrange
        var registros = new List<RegistroCafe> { new() { Id = 1, ProductoId = 5, NombreTrabajador = "Ana", Fecha = DateTime.Now } };
        _repositoryMock.Setup(r => r.GetUltimosAsync(20)).ReturnsAsync(registros);
        _productoRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(
            [new Producto { Id = 5, Nombre = "Brasil — Cuarto 250g", StockActual = 3, StockMaximo = 4 }]);

        // Act
        var resultado = (await _service.GetUltimosAsync()).ToList();

        // Assert
        resultado.Should().HaveCount(1);
        resultado[0].ProductoNombre.Should().Be("Brasil — Cuarto 250g");
    }
}
