using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Compras.Base;
using CafeStock.Back.Services.Facturas;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.Proveedores;
using FluentAssertions;
using Moq;

namespace CafeStock.Tests.Services;

[TestFixture]
public class FacturaServiceTest
{
    private Mock<ICompraRepository> _compraRepositoryMock;
    private Mock<IProductoService> _productoServiceMock;
    private Mock<IProveedorService> _proveedorServiceMock;
    private FacturaService _service;

    [SetUp]
    public void Setup()
    {
        _compraRepositoryMock = new Mock<ICompraRepository>();
        _productoServiceMock = new Mock<IProductoService>();
        _proveedorServiceMock = new Mock<IProveedorService>();
        _service = new FacturaService(_compraRepositoryMock.Object, _productoServiceMock.Object, _proveedorServiceMock.Object);

        _productoServiceMock
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync([new Producto { Id = 1, Nombre = "Café" }]);
        _proveedorServiceMock
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync([new Proveedor { Id = 10, Nombre = "Alcampo" }]);
    }

    [Test]
    public async Task GetAllAsync_ResuelveNombresDeProveedorYProducto()
    {
        // Arrange
        var compra = new Compra
        {
            Id = 1,
            ProveedorId = 10,
            Fecha = DateTime.Now,
            Lineas = [new LineaCompra { ProductoId = 1, Cantidad = 3, PrecioUnitario = 2.5m }]
        };
        _compraRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([compra]);

        // Act
        var facturas = (await _service.GetAllAsync()).ToList();

        // Assert
        facturas.Should().ContainSingle();
        facturas[0].ProveedorNombre.Should().Be("Alcampo");
        facturas[0].Lineas.Single().ProductoNombre.Should().Be("Café");
    }

    [Test]
    public async Task GetAllAsync_SinProveedor_ProveedorNombreEsNulo()
    {
        // Arrange
        var compra = new Compra
        {
            Id = 1,
            ProveedorId = null,
            Fecha = DateTime.Now,
            Lineas = [new LineaCompra { ProductoId = 1, Cantidad = 1, PrecioUnitario = 1m }]
        };
        _compraRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([compra]);

        // Act
        var facturas = (await _service.GetAllAsync()).ToList();

        // Assert
        facturas[0].ProveedorNombre.Should().BeNull();
    }

    [Test]
    public async Task GetByProveedorAsync_FiltraSoloLasDeEseProveedor()
    {
        // Arrange
        var compraAlcampo = new Compra { Id = 1, ProveedorId = 10, Fecha = DateTime.Now, Lineas = [] };
        var compraOtro = new Compra { Id = 2, ProveedorId = 20, Fecha = DateTime.Now, Lineas = [] };
        _compraRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([compraAlcampo, compraOtro]);

        // Act
        var facturas = (await _service.GetByProveedorAsync(10)).ToList();

        // Assert
        facturas.Should().ContainSingle();
        facturas[0].Id.Should().Be(1);
    }
}
