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
}