using CafeStock.Back.Models;
using CafeStock.Back.Validators.Compras;
using FluentAssertions;

namespace CafeStock.Tests.Validators;

[TestFixture]
public class ValidadorCompraTest
{
    private ValidadorCompra _validador;

    [SetUp]
    public void Setup()
    {
        _validador = new ValidadorCompra();
    }

    [Test]
    public void Validar_CompraConLineas_DevuelveSuccess()
    {
        // Arrange
        var compra = new Compra
        {
            Fecha = DateTime.Now,
            Lineas = [new LineaCompra { ProductoId = 1, Cantidad = 2, PrecioUnitario = 4.5m }]
        };

        // Act
        var resultado = _validador.Validar(compra);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Validar_SinLineasPeroConImporteExcepcional_DevuelveSuccess()
    {
        // Arrange: compra excepcional pura (un molde, un cubo de basura...), sin ningún
        // Producto asociado — solo el importe que el usuario ya suma por su cuenta.
        var compra = new Compra
        {
            Fecha = DateTime.Now,
            Lineas = [],
            ImporteComprasExcepcionales = 12.5m
        };

        // Act
        var resultado = _validador.Validar(compra);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Validar_SinLineasYSinImporteExcepcional_DevuelveFailure()
    {
        // Arrange: no representa ningún gasto real
        var compra = new Compra { Fecha = DateTime.Now, Lineas = [] };

        // Act
        var resultado = _validador.Validar(compra);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_ImporteExcepcionalNegativo_DevuelveFailure()
    {
        // Arrange
        var compra = new Compra
        {
            Fecha = DateTime.Now,
            Lineas = [new LineaCompra { ProductoId = 1, Cantidad = 2, PrecioUnitario = 4.5m }],
            ImporteComprasExcepcionales = -1m
        };

        // Act
        var resultado = _validador.Validar(compra);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_LineaConCantidadCero_DevuelveFailure()
    {
        // Arrange
        var compra = new Compra
        {
            Fecha = DateTime.Now,
            Lineas = [new LineaCompra { ProductoId = 1, Cantidad = 0, PrecioUnitario = 4.5m }]
        };

        // Act
        var resultado = _validador.Validar(compra);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_LineaConPrecioUnitarioCero_DevuelveFailure()
    {
        // Arrange
        var compra = new Compra
        {
            Fecha = DateTime.Now,
            Lineas = [new LineaCompra { ProductoId = 1, Cantidad = 2, PrecioUnitario = 0 }]
        };

        // Act
        var resultado = _validador.Validar(compra);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }
}
