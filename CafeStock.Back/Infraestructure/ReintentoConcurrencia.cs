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
    /// tablets) 1-2 reintentos bastan. En cada ronda de N escritores simultáneos al menos uno
    /// gana, así que el último necesita hasta N intentos: 25 cubre de sobra el peor caso
    /// realista y sigue sin haber riesgo de bucle infinito.
    /// </summary>
    public const int MaxIntentos = 25;

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
                // Pausa aleatoria creciente: evita que los escritores en conflicto choquen otra
                // vez en el mismo instante y se desfasen entre sí en la siguiente ronda.
                await Task.Delay(Random.Shared.Next(1, 5 * intento + 2));
            }
        }
    }
}
