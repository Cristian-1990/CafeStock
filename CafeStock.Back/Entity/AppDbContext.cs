using CafeStock.Back.Models;
using Microsoft.EntityFrameworkCore;
namespace CafeStock.Back.Entity;
/// <summary>
/// Hereda de dbContext para tener todas sus funcionalidades
/// </summary>
public class AppDbContext : DbContext
{
    private readonly string _connectionString; //Guarda la ruta al erchivo SQLite

    public AppDbContext(string connectionString) //Definimos un constructor con inyeccion de la ruta
    {
        _connectionString = connectionString;
    }

    public DbSet<ProductoEntity> Productos { get; set; } = null!; //DbSet<ProductoEntity> Coleccion de ProductoEntity, Tabla Productos en SQLite
    public DbSet<ProveedorEntity> Proveedores { get; set; } = null!;
    public DbSet<CompraEntity> Compras { get; set; } = null!;
    public DbSet<LineaCompraEntity> LineasCompra { get; set; } = null!;
    public DbSet<RegistroCafeEntity> RegistrosCafe { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) //
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite(_connectionString);
    }

    /// <summary>
    /// Al eliminar un Proveedor, los productos que lo tenían asignado quedan con
    /// ProveedorId = null en vez de eliminarse (o de bloquear el borrado por FK). Igual para
    /// las Compras: el histórico se conserva sin proveedor asignado.
    /// Las líneas de una Compra se eliminan con ella (son de su propiedad). Si se elimina un
    /// producto que ya tiene líneas de compra, esas líneas históricas se eliminan también
    /// (cascade) en vez de bloquear el borrado del producto.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductoEntity>()
            .HasOne(p => p.Proveedor)
            .WithMany()
            .HasForeignKey(p => p.ProveedorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CompraEntity>()
            .HasOne(c => c.Proveedor)
            .WithMany()
            .HasForeignKey(c => c.ProveedorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<LineaCompraEntity>()
            .HasOne(l => l.Compra)
            .WithMany(c => c.Lineas)
            .HasForeignKey(l => l.CompraId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LineaCompraEntity>()
            .HasOne(l => l.Producto)
            .WithMany()
            .HasForeignKey(l => l.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Los registros de venta/consumo son historial ligado a su producto: si se elimina
        // el producto, sus registros se eliminan con él (igual que LineaCompra).
        modelBuilder.Entity<RegistroCafeEntity>()
            .HasOne(r => r.Producto)
            .WithMany()
            .HasForeignKey(r => r.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);
    }

/// <summary>
/// Crea un archivo .db y las tablas si no existen, si ya existen no hace nada
/// </summary>
    public async Task EnsureCreatedAsync()
    {
        await Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Migración idempotente: en bases de datos creadas antes de que existiera este campo,
    /// EnsureCreatedAsync no la toca (solo crea el esquema si el .db no existía). Comprueba
    /// con PRAGMA table_info si la columna ya está y, si no, la añade sin perder datos.
    /// </summary>
    public Task AsegurarColumnaEsSupermercadoGenericoAsync() =>
        AsegurarColumnaAsync("Proveedores", "EsSupermercadoGenerico", "INTEGER NOT NULL DEFAULT 0");

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
    /// Igual que las anteriores, para el campo SeguimientoIndividual de Producto, añadido
    /// después de que ya hubiera productos en producción (todos arrancan en false).
    /// </summary>
    public Task AsegurarColumnaSeguimientoIndividualAsync() =>
        AsegurarColumnaAsync("Productos", "SeguimientoIndividual", "INTEGER NOT NULL DEFAULT 0");

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
}