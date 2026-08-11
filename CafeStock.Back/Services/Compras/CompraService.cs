using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Compras.Base;
using CafeStock.Back.Validators.Common;
using Serilog;

namespace CafeStock.Back.Services.Compras;

public class CompraService : ICompraService
{
    private readonly ICompraRepository _repository;
    private readonly IValidador<Compra> _validador;

    public CompraService(ICompraRepository repository, IValidador<Compra> validador)
    {
        _repository = repository;
        _validador = validador;
    }

    public async Task<IEnumerable<Compra>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Result<Compra, DomainError>> CreateAsync(Compra compra)
    {
        var validacion = _validador.Validar(compra);
        if (validacion.IsFailure)
            return validacion;

        var resultado = await _repository.CreateAsync(compra);
        if (resultado.IsSuccess)
            Log.Information("Compra registrada: Id={Id}, ProveedorId={ProveedorId}, Lineas={Lineas}",
                resultado.Value.Id, resultado.Value.ProveedorId, resultado.Value.Lineas.Count);
        return resultado;
    }
}
