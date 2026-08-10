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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) //
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite(_connectionString);
    }

    /// <summary>
    /// Al eliminar un Proveedor, los productos que lo tenían asignado quedan con
    /// ProveedorId = null en vez de eliminarse (o de bloquear el borrado por FK).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductoEntity>()
            .HasOne(p => p.Proveedor)
            .WithMany()
            .HasForeignKey(p => p.ProveedorId)
            .OnDelete(DeleteBehavior.SetNull);
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
    public async Task AsegurarColumnaEsSupermercadoGenericoAsync()
    {
        var conexion = Database.GetDbConnection();
        await conexion.OpenAsync();
        try
        {
            var columnaExiste = false;
            await using (var comandoComprobar = conexion.CreateCommand())
            {
                comandoComprobar.CommandText = "PRAGMA table_info(Proveedores)";
                await using var lector = await comandoComprobar.ExecuteReaderAsync();
                while (await lector.ReadAsync())
                {
                    if (string.Equals(lector.GetString(lector.GetOrdinal("name")), "EsSupermercadoGenerico", StringComparison.OrdinalIgnoreCase))
                    {
                        columnaExiste = true;
                        break;
                    }
                }
            }

            if (!columnaExiste)
            {
                await using var comandoAlterar = conexion.CreateCommand();
                comandoAlterar.CommandText = "ALTER TABLE Proveedores ADD COLUMN EsSupermercadoGenerico INTEGER NOT NULL DEFAULT 0";
                await comandoAlterar.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            await conexion.CloseAsync();
        }
    }
}