using CafeStock.Back.Models;
using CafeStock.Back.Repositories.SnapshotsInventario.Base;
using CafeStock.Back.Services.Productos;
using Serilog;

namespace CafeStock.Back.Services.SnapshotsInventario;

public class SnapshotInventarioService : ISnapshotInventarioService
{
    private readonly ISnapshotInventarioRepository _repository;
    private readonly IProductoService _productoService;

    public SnapshotInventarioService(ISnapshotInventarioRepository repository, IProductoService productoService)
    {
        _repository = repository;
        _productoService = productoService;
    }

    public async Task<IEnumerable<SnapshotInventario>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task AsegurarSnapshotDeHoyAsync()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);

        var existente = await _repository.GetByFechaAsync(hoy);
        if (existente.IsSuccess)
            return;

        var productos = await _productoService.GetAllAsync();
        var valorTotal = productos.Sum(p => p.StockActual * p.PrecioUnitario);

        var snapshot = new SnapshotInventario { Fecha = hoy, ValorTotal = valorTotal };
        var resultado = await _repository.CreateAsync(snapshot);
        if (resultado.IsSuccess)
            Log.Information("Snapshot de inventario capturado: Fecha={Fecha}, ValorTotal={ValorTotal}", hoy, valorTotal);
        else
            Log.Warning("No se pudo capturar el snapshot de inventario de hoy: {Error}", resultado.Error.Message);
    }
}
