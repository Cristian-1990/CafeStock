using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CafeStock.Back.Infrastructure;

/// <summary>
/// Concurrencia: ejecuta una operación de "leer → modificar → guardar" y, si otro usuario la
/// pisó mientras tanto, la repite desde cero con datos frescos. Dos situaciones se reintentan:
///
/// 1. <see cref="DbUpdateConcurrencyException"/>: concurrencia optimista de EF. Otro cambió el
///    mismo registro entre la lectura y el guardado (ver el token de concurrencia en AppDbContext).
/// 2. SQLITE_BUSY / SQLITE_LOCKED: SQLite solo admite un escritor a la vez. Si dos transacciones
///    leen y luego intentan escribir a la vez, una no puede "subir" de lectura a escritura y
///    SQLite la rechaza al instante con "database is locked" (esperar no lo arreglaría: la otra
///    ya habría cambiado los datos que leyó). La solución correcta es la misma: repetir.
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

    // Códigos primarios de SQLite (https://www.sqlite.org/rescode.html).
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;

    public static async Task<T> EjecutarAsync<T>(Func<Task<T>> operacion)
    {
        for (var intento = 1; ; intento++)
        {
            try
            {
                return await operacion();
            }
            catch (Exception ex) when (EsReintentable(ex) && intento < MaxIntentos)
            {
                Log.Debug(ex, "Conflicto de concurrencia (intento {Intento}/{Max}); se reintenta con datos frescos",
                    intento, MaxIntentos);
                // Pausa aleatoria creciente: evita que los escritores en conflicto choquen otra
                // vez en el mismo instante y se desfasen entre sí en la siguiente ronda.
                await Task.Delay(Random.Shared.Next(1, 5 * intento + 2));
            }
        }
    }

    public static bool EsReintentable(Exception ex) =>
        ex is DbUpdateConcurrencyException
        || ex is SqliteException { SqliteErrorCode: SqliteBusy or SqliteLocked }
        || ex is DbUpdateException { InnerException: SqliteException { SqliteErrorCode: SqliteBusy or SqliteLocked } };
}
