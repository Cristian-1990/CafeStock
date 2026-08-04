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
}