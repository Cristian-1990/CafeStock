using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Compras.Base;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.Proveedores;

namespace CafeStock.Back.Services.Facturas;

public class FacturaService : IFacturaService
{
    private readonly ICompraRepository _compraRepository;
    private readonly IProductoService _productoService;
    private readonly IProveedorService _proveedorService;

    public FacturaService(ICompraRepository compraRepository, IProductoService productoService, IProveedorService proveedorService)
    {
        _compraRepository = compraRepository;
        _productoService = productoService;
        _proveedorService = proveedorService;
    }

    public async Task<IEnumerable<Compra>> GetAllAsync()
    {
        var compras = await _compraRepository.GetAllAsync();
        var productosPorId = (await _productoService.GetAllAsync()).ToDictionary(p => p.Id);
        var proveedoresPorId = (await _proveedorService.GetAllAsync()).ToDictionary(p => p.Id);

        return compras.Select(compra => ResolverNombres(compra, productosPorId, proveedoresPorId)).ToList();
    }

    public async Task<IEnumerable<Compra>> GetByProveedorAsync(int proveedorId)
    {
        var facturas = await GetAllAsync();
        return facturas.Where(f => f.ProveedorId == proveedorId);
    }

    private static Compra ResolverNombres(
        Compra compra,
        Dictionary<int, Producto> productosPorId,
        Dictionary<int, Proveedor> proveedoresPorId)
    {
        return compra with
        {
            ProveedorNombre = compra.ProveedorId.HasValue && proveedoresPorId.TryGetValue(compra.ProveedorId.Value, out var proveedor)
                ? proveedor.Nombre
                : null,
            Lineas = compra.Lineas.Select(linea => linea with
            {
                ProductoNombre = productosPorId.TryGetValue(linea.ProductoId, out var producto) ? producto.Nombre : null
            }).ToList()
        };
    }
}
