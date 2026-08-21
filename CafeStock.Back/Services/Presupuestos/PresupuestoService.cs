using CSharpFunctionalExtensions;
using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Presupuestos;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Presupuestos.Base;
using CafeStock.Back.Validators.Common;
using Serilog;

namespace CafeStock.Back.Services.Presupuestos;

public class PresupuestoService : IPresupuestoService
{
    private readonly IPresupuestoRepository _repository;
    private readonly IValidador<Presupuesto> _validador;

    public PresupuestoService(IPresupuestoRepository repository, IValidador<Presupuesto> validador)
    {
        _repository = repository;
        _validador = validador;
    }

    public async Task<IEnumerable<Presupuesto>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<Presupuesto>> GetByProveedorAsync(int proveedorId)
    {
        return await _repository.GetByProveedorAsync(proveedorId);
    }

    public async Task<Result<Presupuesto, DomainError>> GetByProveedorMesAnioAsync(int proveedorId, int mes, int anio)
    {
        return await _repository.GetByProveedorMesAnioAsync(proveedorId, mes, anio);
    }

    public async Task<Result<Presupuesto, DomainError>> CreateAsync(Presupuesto presupuesto)
    {
        var validacion = _validador.Validar(presupuesto);
        if (validacion.IsFailure)
            return validacion;

        var resultado = await _repository.CreateAsync(presupuesto);
        if (resultado.IsSuccess)
            Log.Information("Presupuesto creado: Id={Id}, ProveedorId={ProveedorId}, Mes={Mes}, Anio={Anio}, ImporteAsignado={ImporteAsignado}",
                resultado.Value.Id, resultado.Value.ProveedorId, resultado.Value.Mes, resultado.Value.Anio, resultado.Value.ImporteAsignado);
        return resultado;
    }

    public async Task<Result<Presupuesto, DomainError>> ActualizarImporteAsync(int id, decimal nuevoImporte)
    {
        if (nuevoImporte <= 0)
            return Result.Failure<Presupuesto, DomainError>(
                PresupuestoErrors.Validation(["El importe asignado debe ser mayor que 0"]));

        var resultado = await _repository.ActualizarImporteAsync(id, nuevoImporte);
        if (resultado.IsSuccess)
            Log.Information("Importe de presupuesto actualizado: Id={Id}, NuevoImporte={NuevoImporte}", id, nuevoImporte);
        return resultado;
    }

    public async Task<Result<Presupuesto, DomainError>> DeleteAsync(int id)
    {
        var resultado = await _repository.DeleteAsync(id);
        if (resultado.IsSuccess)
            Log.Information("Presupuesto eliminado: Id={Id}", id);
        return resultado;
    }
}
