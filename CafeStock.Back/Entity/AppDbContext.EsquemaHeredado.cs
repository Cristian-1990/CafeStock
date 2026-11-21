using CafeStock.Back.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeStock.Back.Entity;

// =====================================================================================
// CÓDIGO HEREDADO — actualización de esquema anterior a EF Core Migrations.
//
// Hasta ahora el esquema se mantenía con EnsureCreated + ALTER TABLE a mano (19 métodos
// AsegurarXxxAsync). Desde la migración InitialCreate el esquema lo gestionan las migraciones
// (carpeta Migrations/). Este fichero solo se usa UNA VEZ por base de datos antigua:
// DatabaseInitializer lo ejecuta para dejar una .db creada con el sistema viejo igual que el
// modelo actual, y a continuación la marca como "InitialCreate ya aplicada".
//
// Se puede borrar este fichero cuando todas las bases de datos reales (producción en la Pi,
// pruebas, portátil, sobremesa) hayan arrancado al menos una vez con esta versión.
// =====================================================================================
public partial class AppDbContext
{
    /// <summary>
    /// Deja una base de datos creada con el sistema antiguo (sin tabla __EFMigrationsHistory)
    /// con el mismo esquema que el modelo actual. Orden obligatorio, aprendido de bugs reales
    /// (HTTP 500 al arrancar): PRIMERO todas las tablas y columnas con SQL crudo y SOLO DESPUÉS
    /// las correcciones de datos, que usan consultas LINQ y por tanto seleccionan TODAS las
    /// columnas del modelo — si alguna aún no existe, la consulta revienta con "no such column".
    /// </summary>
    public async Task ActualizarEsquemaHeredadoAsync()
    {
        // 1) Tablas que pudieron no existir en bases de datos muy antiguas.
        await AsegurarTablasComprasAsync();
        await AsegurarTablaRegistrosCafeAsync();
        await AsegurarTablaPresupuestosAsync();
        await AsegurarTablaSnapshotsInventarioAsync();

        // 2) Columnas añadidas con el tiempo, por tabla.
        await AsegurarColumnaEsSupermercadoGenericoAsync();
        await AsegurarColumnaAgruparPorTipoUnidadAsync();
        await AsegurarColumnaPrecioUnitarioAltaPrecisionAsync();
        await AsegurarColumnaDireccionAsync();
        await AsegurarColumnaNifCifAsync();

        await AsegurarColumnaPrecioUnitarioAsync();
        await AsegurarColumnaSeguimientoIndividualAsync();
        await AsegurarColumnaAplicaConciliacionTpvAsync();
        await AsegurarColumnaUnidadesPorPackAsync();
        await AsegurarColumnaPasilloAsync();
        await AsegurarColumnaOrdenEnPasilloAsync();
        await AsegurarColumnaEsFrontalPasilloAsync();
        await AsegurarColumnaSeccionAsync();

        await AsegurarColumnaMetodoPagoAsync();
        await AsegurarColumnaNumeroFacturaProveedorAsync();
        await AsegurarColumnaFacturaAdjuntaUrlAsync();
        await AsegurarColumnaNotasCompraAsync();
        await AsegurarColumnaComprasExcepcionalesAsync();
        await AsegurarColumnaImporteComprasExcepcionalesAsync();

        await AsegurarColumnaStockResultanteTrasRecepcionAsync();
        await AsegurarColumnaStockMaximoEnMomentoAsync();

        // 3) Correcciones de datos de un solo uso (necesitan el esquema ya completo).
        await AsegurarSeguimientoIndividualCafePucheroAsync();
        await AsegurarAgrupacionPorUnidadPucheroAsync();
        await AsegurarPrecioAltaPrecisionPriegolaAsync();
    }

    /// <summary>
    /// Migración idempotente: en bases de datos creadas antes de que existiera este campo,
    /// EnsureCreatedAsync no la toca (solo crea el esquema si el .db no existía). Comprueba
    /// con PRAGMA table_info si la columna ya está y, si no, la añade sin perder datos.
    /// </summary>
    public Task AsegurarColumnaEsSupermercadoGenericoAsync() =>
        AsegurarColumnaAsync("Proveedores", "EsSupermercadoGenerico", "INTEGER NOT NULL DEFAULT 0");

    /// <summary>
    /// Igual que AsegurarColumnaEsSupermercadoGenericoAsync pero para AgruparPorTipoUnidad
    /// (división visual "Por peso"/"Por unidad" en /lista-detallada), añadido después de que
    /// ya hubiera proveedores en producción.
    /// </summary>
    public Task AsegurarColumnaAgruparPorTipoUnidadAsync() =>
        AsegurarColumnaAsync("Proveedores", "AgruparPorTipoUnidad", "INTEGER NOT NULL DEFAULT 0");

    /// <summary>
    /// Igual que AsegurarColumnaEsSupermercadoGenericoAsync pero para
    /// PrecioUnitarioAltaPrecision (4 decimales en vez de 2 al mostrar/editar el precio
    /// unitario), añadido después de que ya hubiera proveedores en producción.
    /// </summary>
    public Task AsegurarColumnaPrecioUnitarioAltaPrecisionAsync() =>
        AsegurarColumnaAsync("Proveedores", "PrecioUnitarioAltaPrecision", "INTEGER NOT NULL DEFAULT 0");

    /// <summary>
    /// Igual que AsegurarColumnaEsSupermercadoGenericoAsync pero para el precio unitario
    /// de Producto, añadido después de que ya hubiera productos en producción.
    /// </summary>
    public Task AsegurarColumnaPrecioUnitarioAsync() =>
        AsegurarColumnaAsync("Productos", "PrecioUnitario", "TEXT NOT NULL DEFAULT '0'");

    /// <summary>
    /// Igual que AsegurarColumnaEsSupermercadoGenericoAsync pero para la dirección
    /// del proveedor, añadida después de que ya hubiera proveedores en producción.
    /// </summary>
    public Task AsegurarColumnaDireccionAsync() =>
        AsegurarColumnaAsync("Proveedores", "Direccion", "TEXT NOT NULL DEFAULT ''");

    /// <summary>
    /// Igual que AsegurarColumnaDireccionAsync pero para el NIF/CIF del proveedor.
    /// </summary>
    public Task AsegurarColumnaNifCifAsync() =>
        AsegurarColumnaAsync("Proveedores", "NifCif", "TEXT NOT NULL DEFAULT ''");

    /// <summary>
    /// Datos de facturación de Compra (método de pago, número de factura del proveedor y
    /// factura adjunta), añadidos después de que la tabla Compras ya existiera en producción.
    /// </summary>
    public Task AsegurarColumnaMetodoPagoAsync() =>
        AsegurarColumnaAsync("Compras", "MetodoPago", "TEXT NOT NULL DEFAULT ''");

    public Task AsegurarColumnaNumeroFacturaProveedorAsync() =>
        AsegurarColumnaAsync("Compras", "NumeroFacturaProveedor", "TEXT NOT NULL DEFAULT ''");

    public Task AsegurarColumnaFacturaAdjuntaUrlAsync() =>
        AsegurarColumnaAsync("Compras", "FacturaAdjuntaUrl", "TEXT NOT NULL DEFAULT ''");

    /// <summary>
    /// Notas libres de Compra, añadidas después de que la tabla ya existiera en producción.
    /// </summary>
    public Task AsegurarColumnaNotasCompraAsync() =>
        AsegurarColumnaAsync("Compras", "Notas", "TEXT NOT NULL DEFAULT ''");

    /// <summary>
    /// Igual que las anteriores, para las dos columnas de compras excepcionales de Compra
    /// (artículos sueltos de supermercado que no valen la pena como Producto, ver
    /// Compra.DescripcionComprasExcepcionales), añadidas después de que la tabla Compras ya
    /// existiera en producción. '' y 0 significan "no hubo compra excepcional", que es el
    /// dato real para todo el historial anterior a este campo.
    /// </summary>
    public Task AsegurarColumnaComprasExcepcionalesAsync() =>
        AsegurarColumnaAsync("Compras", "DescripcionComprasExcepcionales", "TEXT NOT NULL DEFAULT ''");

    public Task AsegurarColumnaImporteComprasExcepcionalesAsync() =>
        AsegurarColumnaAsync("Compras", "ImporteComprasExcepcionales", "TEXT NOT NULL DEFAULT '0'");

    /// <summary>
    /// Igual que las anteriores, para el campo SeguimientoIndividual de Producto, añadido
    /// después de que ya hubiera productos en producción (todos arrancan en false).
    /// </summary>
    public Task AsegurarColumnaSeguimientoIndividualAsync() =>
        AsegurarColumnaAsync("Productos", "SeguimientoIndividual", "INTEGER NOT NULL DEFAULT 0");

    /// <summary>
    /// Igual que AsegurarColumnaSeguimientoIndividualAsync pero para AplicaConciliacionTpv de
    /// Producto, añadido después de que ya hubiera productos en producción. Aquí false SÍ es
    /// un dato real para todo el historial (a diferencia de LineaCompra.StockMaximoEnMomento,
    /// que es nullable porque ahí un 0 fingiría un dato que no existía).
    /// </summary>
    public Task AsegurarColumnaAplicaConciliacionTpvAsync() =>
        AsegurarColumnaAsync("Productos", "AplicaConciliacionTpv", "INTEGER NOT NULL DEFAULT 0");

    /// <summary>
    /// Igual que las anteriores, para UnidadesPorPack de Producto. Sin DEFAULT numérico (NULL)
    /// a propósito: null significa "sin conversión" (comportamiento actual), no "0 unidades
    /// por pack" — un DEFAULT 0 o 1 fingiría un dato de conversión que no existe para el
    /// historial de productos ya creados.
    /// </summary>
    public Task AsegurarColumnaUnidadesPorPackAsync() =>
        AsegurarColumnaAsync("Productos", "UnidadesPorPack", "INTEGER NULL");

    /// <summary>
    /// Igual que AsegurarColumnaUnidadesPorPackAsync pero para Pasillo (orden físico en la
    /// tienda, solo relevante para proveedores "supermercado genérico" — ver
    /// AgrupadorProveedor). Sin DEFAULT numérico a propósito: null significa "sin asignar
    /// todavía", no "pasillo 0".
    /// </summary>
    public Task AsegurarColumnaPasilloAsync() =>
        AsegurarColumnaAsync("Productos", "Pasillo", "INTEGER NULL");

    /// <summary>
    /// Igual que AsegurarColumnaPasilloAsync pero para OrdenEnPasillo (posición dentro del
    /// pasillo). DEFAULT 0 porque, a diferencia de Pasillo, 0 sí es un valor válido: significa
    /// "sin definir todavía", exactamente como para los productos ya existentes.
    /// </summary>
    public Task AsegurarColumnaOrdenEnPasilloAsync() =>
        AsegurarColumnaAsync("Productos", "OrdenEnPasillo", "INTEGER NOT NULL DEFAULT 0");

    /// <summary>
    /// Igual que AsegurarColumnaOrdenEnPasilloAsync pero para EsFrontalPasillo. false es un
    /// dato real para todo el historial (igual que AplicaConciliacionTpv): ningún producto
    /// anterior a este campo estaba marcado como frontal.
    /// </summary>
    public Task AsegurarColumnaEsFrontalPasilloAsync() =>
        AsegurarColumnaAsync("Productos", "EsFrontalPasillo", "INTEGER NOT NULL DEFAULT 0");

    /// <summary>
    /// Igual que AsegurarColumnaEsFrontalPasilloAsync pero para Seccion (SeccionEspecial —
    /// frutería/infusiones, ver Producto.Seccion). DEFAULT 0 porque Ninguna es el valor 0 del
    /// enum: ningún producto anterior a este campo pertenecía a una sección especial.
    /// </summary>
    public Task AsegurarColumnaSeccionAsync() =>
        AsegurarColumnaAsync("Productos", "Seccion", "INTEGER NOT NULL DEFAULT 0");

    /// <summary>
    /// Igual que las anteriores, para los dos campos de captura silenciosa del excedente de
    /// stock al recepcionar (LineaCompra.StockResultanteTrasRecepcion/StockMaximoEnMomento),
    /// añadidos después de que la tabla LineasCompra ya existiera en producción. Sin NOT NULL
    /// ni DEFAULT a propósito: las filas históricas ya guardadas no tienen este dato (no se
    /// recalcula retroactivamente) y deben quedar en NULL, no fingir un 0 que parecería un
    /// valor real — SQLite rellena de NULL las filas existentes al añadir una columna sin
    /// DEFAULT.
    /// </summary>
    public Task AsegurarColumnaStockResultanteTrasRecepcionAsync() =>
        AsegurarColumnaAsync("LineasCompra", "StockResultanteTrasRecepcion", "TEXT NULL");

    public Task AsegurarColumnaStockMaximoEnMomentoAsync() =>
        AsegurarColumnaAsync("LineasCompra", "StockMaximoEnMomento", "TEXT NULL");

    private async Task AsegurarColumnaAsync(string tabla, string columna, string definicionColumna)
    {
        var conexion = Database.GetDbConnection();
        await conexion.OpenAsync();
        try
        {
            var columnaExiste = false;
            await using (var comandoComprobar = conexion.CreateCommand())
            {
                comandoComprobar.CommandText = $"PRAGMA table_info({tabla})";
                await using var lector = await comandoComprobar.ExecuteReaderAsync();
                while (await lector.ReadAsync())
                {
                    if (string.Equals(lector.GetString(lector.GetOrdinal("name")), columna, StringComparison.OrdinalIgnoreCase))
                    {
                        columnaExiste = true;
                        break;
                    }
                }
            }

            if (!columnaExiste)
            {
                await using var comandoAlterar = conexion.CreateCommand();
                comandoAlterar.CommandText = $"ALTER TABLE {tabla} ADD COLUMN {columna} {definicionColumna}";
                await comandoAlterar.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            await conexion.CloseAsync();
        }
    }

    /// <summary>
    /// Migración idempotente: crea las tablas Compras y LineasCompra si no existen todavía
    /// (bases de datos anteriores a esta funcionalidad). CREATE TABLE IF NOT EXISTS ya es
    /// idempotente por sí solo, sin necesidad de comprobación previa como con las columnas.
    /// </summary>
    public async Task AsegurarTablasComprasAsync()
    {
        var conexion = Database.GetDbConnection();
        await conexion.OpenAsync();
        try
        {
            await using (var comandoCompras = conexion.CreateCommand())
            {
                comandoCompras.CommandText = """
                    CREATE TABLE IF NOT EXISTS Compras (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProveedorId INTEGER NULL,
                        Fecha TEXT NOT NULL,
                        FOREIGN KEY (ProveedorId) REFERENCES Proveedores(Id) ON DELETE SET NULL
                    )
                    """;
                await comandoCompras.ExecuteNonQueryAsync();
            }

            await using (var comandoLineas = conexion.CreateCommand())
            {
                comandoLineas.CommandText = """
                    CREATE TABLE IF NOT EXISTS LineasCompra (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CompraId INTEGER NOT NULL,
                        ProductoId INTEGER NOT NULL,
                        Cantidad INTEGER NOT NULL,
                        PrecioUnitario TEXT NOT NULL,
                        FOREIGN KEY (CompraId) REFERENCES Compras(Id) ON DELETE CASCADE,
                        FOREIGN KEY (ProductoId) REFERENCES Productos(Id) ON DELETE CASCADE
                    )
                    """;
                await comandoLineas.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            await conexion.CloseAsync();
        }
    }

    /// <summary>
    /// Migración idempotente: crea la tabla RegistrosCafe si no existe todavía (bases de
    /// datos anteriores a esta funcionalidad). CREATE TABLE IF NOT EXISTS ya es idempotente
    /// por sí solo.
    /// </summary>
    public async Task AsegurarTablaRegistrosCafeAsync()
    {
        var conexion = Database.GetDbConnection();
        await conexion.OpenAsync();
        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = """
                CREATE TABLE IF NOT EXISTS RegistrosCafe (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductoId INTEGER NOT NULL,
                    NombreTrabajador TEXT NOT NULL,
                    Fecha TEXT NOT NULL,
                    FOREIGN KEY (ProductoId) REFERENCES Productos(Id) ON DELETE CASCADE
                )
                """;
            await comando.ExecuteNonQueryAsync();
        }
        finally
        {
            await conexion.CloseAsync();
        }
    }

    /// <summary>
    /// Migración idempotente: crea la tabla Presupuestos si no existe todavía (bases de datos
    /// anteriores a esta funcionalidad). El UNIQUE en la propia definición de columnas cubre
    /// el mismo índice único que HasIndex()+IsUnique() en OnModelCreating, para bases de datos
    /// que nunca pasan por EnsureCreated (solo se crearon una vez y desde entonces solo se les
    /// van añadiendo columnas/tablas nuevas a mano, igual que el resto de AsegurarTablaXxxAsync).
    /// </summary>
    public async Task AsegurarTablaPresupuestosAsync()
    {
        var conexion = Database.GetDbConnection();
        await conexion.OpenAsync();
        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = """
                CREATE TABLE IF NOT EXISTS Presupuestos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProveedorId INTEGER NOT NULL,
                    Mes INTEGER NOT NULL,
                    Anio INTEGER NOT NULL,
                    ImporteAsignado TEXT NOT NULL,
                    FOREIGN KEY (ProveedorId) REFERENCES Proveedores(Id) ON DELETE CASCADE,
                    UNIQUE (ProveedorId, Mes, Anio)
                )
                """;
            await comando.ExecuteNonQueryAsync();
        }
        finally
        {
            await conexion.CloseAsync();
        }
    }

    /// <summary>
    /// Igual que AsegurarTablaPresupuestosAsync pero para SnapshotsInventario: como mucho un
    /// snapshot por día (UNIQUE en Fecha).
    /// </summary>
    public async Task AsegurarTablaSnapshotsInventarioAsync()
    {
        var conexion = Database.GetDbConnection();
        await conexion.OpenAsync();
        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = """
                CREATE TABLE IF NOT EXISTS SnapshotsInventario (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fecha TEXT NOT NULL,
                    ValorTotal TEXT NOT NULL,
                    UNIQUE (Fecha)
                )
                """;
            await comando.ExecuteNonQueryAsync();
        }
        finally
        {
            await conexion.CloseAsync();
        }
    }

    /// <summary>
    /// Migración de datos, idempotente y de un solo uso: activa SeguimientoIndividual en los
    /// dos cafés de Puchero (hasta ahora ambos guardados como "Brasil", diferenciados solo por
    /// Unidad) y les da un nombre descriptivo. Solo actúa mientras el nombre siga siendo
    /// "Brasil": una vez renombrados, no vuelve a encontrar candidatos y no hace nada en
    /// arranques posteriores. Requiere que la columna SeguimientoIndividual ya exista
    /// (llamar después de AsegurarColumnaSeguimientoIndividualAsync).
    /// </summary>
    public async Task AsegurarSeguimientoIndividualCafePucheroAsync()
    {
        var puchero = await Proveedores.FirstOrDefaultAsync(p => p.Nombre == "Puchero");
        if (puchero is null) return;

        var candidatos = await Productos
            .Where(p => p.ProveedorId == puchero.Id && p.Nombre == "Brasil")
            .ToListAsync();
        if (!candidatos.Any()) return;

        foreach (var producto in candidatos)
        {
            switch (producto.Unidad)
            {
                case UnidadMedida.Kg:
                    producto.Nombre = "Brasil — Grano 1kg";
                    producto.SeguimientoIndividual = true;
                    break;
                case UnidadMedida.Unidades:
                    producto.Nombre = "Brasil — Cuarto 250g";
                    producto.SeguimientoIndividual = true;
                    break;
            }
        }

        await SaveChangesAsync();
    }

    /// <summary>
    /// Migración de datos, idempotente y de un solo uso: activa AgruparPorTipoUnidad en el
    /// proveedor Puchero (el único con productos en más de un GrupoUnidadMedida que deben
    /// verse en secciones separadas — sus dos cafés, Grano 1kg y Cuarto 250g). Solo actúa
    /// mientras siga en false: una vez activado, no vuelve a hacer nada en arranques
    /// posteriores, así que un usuario puede desactivarlo a mano sin que esta migración se lo
    /// reactive. Requiere que la columna AgruparPorTipoUnidad ya exista (llamar después de
    /// AsegurarColumnaAgruparPorTipoUnidadAsync).
    /// </summary>
    public async Task AsegurarAgrupacionPorUnidadPucheroAsync()
    {
        var puchero = await Proveedores.FirstOrDefaultAsync(p => p.Nombre == "Puchero");
        if (puchero is null || puchero.AgruparPorTipoUnidad) return;

        puchero.AgruparPorTipoUnidad = true;
        await SaveChangesAsync();
    }

    /// <summary>
    /// Igual que AsegurarAgrupacionPorUnidadPucheroAsync pero activa
    /// PrecioUnitarioAltaPrecision en el proveedor Priégola (su precio por unidad sale de
    /// dividir un precio de caja entre muchas unidades, y con 2 decimales se pierde precisión
    /// real). Requiere que la columna PrecioUnitarioAltaPrecision ya exista (llamar después de
    /// AsegurarColumnaPrecioUnitarioAltaPrecisionAsync).
    ///
    /// Compara con Trim() porque el nombre real en producción es "Priégola " (con espacio
    /// final) — una comparación exacta nunca habría igualado y esta migración no habría hecho
    /// nada nunca, en silencio (bug real ya sufrido: la migración jamás activó el flag).
    /// </summary>
    public async Task AsegurarPrecioAltaPrecisionPriegolaAsync()
    {
        var priegola = await Proveedores.FirstOrDefaultAsync(p => p.Nombre.Trim() == "Priégola");
        if (priegola is null || priegola.PrecioUnitarioAltaPrecision) return;

        priegola.PrecioUnitarioAltaPrecision = true;
        await SaveChangesAsync();
    }
}
