using CafeStock.Back.Models;
using CafeStock.Back.Services.Productos;
using FluentAssertions;

namespace CafeStock.Tests.Services;

[TestFixture]
public class AgrupadorProveedorTest
{
    private static Proveedor Supermercado(int id, string nombre) =>
        new() { Id = id, Nombre = nombre, EsSupermercadoGenerico = true };

    private static Proveedor Distribuidor(int id, string nombre) =>
        new() { Id = id, Nombre = nombre, EsSupermercadoGenerico = false };

    [Test]
    public void AgruparPorProveedor_SupermercadoGenerico_OrdenaPorPasilloAscendente()
    {
        // Arrange
        var alcampo = Supermercado(1, "Alcampo");
        var productos = new List<Producto>
        {
            new() { Id = 1, Nombre = "Tonica", ProveedorId = 1, Pasillo = 3 },
            new() { Id = 2, Nombre = "Atun", ProveedorId = 1, Pasillo = 1 },
            new() { Id = 3, Nombre = "Aceitunas", ProveedorId = 1, Pasillo = 2 }
        };

        // Act
        var grupos = AgrupadorProveedor.AgruparPorProveedor(productos, [alcampo]).ToList();

        // Assert
        grupos.Should().HaveCount(1);
        grupos[0].Productos.Select(p => p.Nombre).Should().Equal("Atun", "Aceitunas", "Tonica");
    }

    [Test]
    public void AgruparPorProveedor_SupermercadoGenerico_HeladoConPasillo100SalePorEncimaDeLoDemas()
    {
        // Arrange: el helado se marca con Pasillo=100 (convención manual, ver
        // Producto.Pasillo) para que salga siempre el último, sin importar cuántos pasillos
        // reales (nunca más de 50) haya en la tienda.
        var alcampo = Supermercado(1, "Alcampo");
        var productos = new List<Producto>
        {
            new() { Id = 1, Nombre = "Helado", ProveedorId = 1, Pasillo = 100 },
            new() { Id = 2, Nombre = "Tonica", ProveedorId = 1, Pasillo = 3 },
            new() { Id = 3, Nombre = "Atun", ProveedorId = 1, Pasillo = 1 }
        };

        // Act
        var grupos = AgrupadorProveedor.AgruparPorProveedor(productos, [alcampo]).ToList();

        // Assert
        grupos[0].Productos.Select(p => p.Nombre).Should().Equal("Atun", "Tonica", "Helado");
    }

    [Test]
    public void AgruparPorProveedor_SupermercadoGenerico_PasilloSinAsignar_VaDespuesDeLosAsignadosYAntesDelHelado()
    {
        // Arrange: un producto todavía sin Pasillo asignado (null, dato pendiente de rellenar)
        // no debe colarse antes de uno que sí tiene pasillo real, ni después del helado.
        var alcampo = Supermercado(1, "Alcampo");
        var productos = new List<Producto>
        {
            new() { Id = 1, Nombre = "Helado", ProveedorId = 1, Pasillo = 100 },
            new() { Id = 2, Nombre = "SinPasilloTodavia", ProveedorId = 1, Pasillo = null },
            new() { Id = 3, Nombre = "Atun", ProveedorId = 1, Pasillo = 1 }
        };

        // Act
        var grupos = AgrupadorProveedor.AgruparPorProveedor(productos, [alcampo]).ToList();

        // Assert
        grupos[0].Productos.Select(p => p.Nombre).Should().Equal("Atun", "SinPasilloTodavia", "Helado");
    }

    [Test]
    public void AgruparPorProveedor_ProveedorNoEsSupermercadoGenerico_NoReordenaPorPasillo()
    {
        // Arrange: un proveedor normal (reparto por distribuidor) mantiene el orden de
        // siempre, ignorando Pasillo aunque tenga valor.
        var distribuidor = Distribuidor(1, "Mahou");
        var productos = new List<Producto>
        {
            new() { Id = 1, Nombre = "Cerveza 33cl", ProveedorId = 1, Pasillo = 5 },
            new() { Id = 2, Nombre = "Cerveza 1L", ProveedorId = 1, Pasillo = 1 }
        };

        // Act
        var grupos = AgrupadorProveedor.AgruparPorProveedor(productos, [distribuidor]).ToList();

        // Assert: se mantiene el orden de entrada, no el de Pasillo
        grupos[0].Productos.Select(p => p.Nombre).Should().Equal("Cerveza 33cl", "Cerveza 1L");
    }
}
