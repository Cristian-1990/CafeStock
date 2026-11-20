using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CafeStock.Back.Infrastructure;

/// <summary>
/// Concurrencia optimista: ejecuta una operación de "leer → modificar → guardar" y, si otro
/// usuario cambió el mismo registro entre la lectura y el guardado
/// (<see cref="DbUpdateConcurrencyException"/>), la repite desde cero con datos frescos.
///
/// La operación debe crear su propio <c>AppDbContext</c> en cada ejecución: el contexto que
/// falló conserva en memoria los valores antiguos, así que reutilizarlo reintentaría
/// siempre con la misma lectura obsoleta.
/// </summary>
public static class ReintentoConcurrencia
{
    /// <summary>
    /// Intentos máximos antes de rendirse. Con la carga de una cafetería (un puñado de
    /// tablets) 1-2 reintentos bastan; 10 da margen sin riesgo de bucle infinito.
    /// </summary>
    public const int MaxIntentos = 10;

    public static async Task<T> EjecutarAsync<T>(Func<Task<T>> operacion)
    {
        for (var intento = 1; ; intento++)
        {
            try
            {
                return await operacion();
            }
            catch (DbUpdateConcurrencyException ex) when (intento < MaxIntentos)
            {
                Log.Debug(ex, "Conflicto de concurrencia (intento {Intento}/{Max}); se reintenta con datos frescos",
                    intento, MaxIntentos);
            }
        }
    }
}
