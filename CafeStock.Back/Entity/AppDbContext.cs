using CafeStock.Back.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace CafeStock.Back.Entity;
/// <summary>
/// Contexto único de la app. Hereda de IdentityDbContext para guardar usuarios y roles en la
/// misma base de datos (y el mismo historial de migraciones) que el resto de tablas.
/// </summary>
public partial class AppDbContext : IdentityDbContext<IdentityUser>
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
    public DbSet<PresupuestoEntity> Presupuestos { get; set; } = null!;
    public DbSet<SnapshotInventarioEntity> SnapshotsInventario { get; set; } = null!;
    public DbSet<StockRealizadoEntity> StocksRealizados { get; set; } = null!;
    public DbSet<HojaCafeEntity> HojasCafe { get; set; } = null!;
    public DbSet<PaqueteCafeEntity> PaquetesCafe { get; set; } = null!;

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
        // Imprescindible en un IdentityDbContext: configura las tablas de usuarios y roles
        // (AspNetUsers, AspNetRoles, AspNetUserRoles…).
        base.OnModelCreating(modelBuilder);

        // Concurrencia optimista sin cambiar el esquema: EF añade la condición
        // "AND StockActual = <valor leído>" al UPDATE. Si otra tablet cambió el stock entre
        // la lectura y el guardado, el UPDATE afecta a 0 filas y EF lanza
        // DbUpdateConcurrencyException (ver ReintentoConcurrencia) en vez de pisar su cambio.
        modelBuilder.Entity<ProductoEntity>()
            .Property(p => p.StockActual)
            .IsConcurrencyToken();

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

        // A diferencia de Producto/Compra, ProveedorId en Presupuesto es obligatorio (un
        // presupuesto no tiene sentido sin proveedor): si se elimina el proveedor, sus
        // presupuestos se eliminan con él (Cascade), igual que LineaCompra/RegistroCafe con
        // sus FK obligatorias, en vez de SetNull (que no sería válido sobre un int no anulable).
        modelBuilder.Entity<PresupuestoEntity>()
            .HasOne(p => p.Proveedor)
            .WithMany()
            .HasForeignKey(p => p.ProveedorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Un presupuesto por proveedor y mes/año — ver también la comprobación equivalente en
        // PresupuestosEfRepository.CreateAsync (más amigable) y AsegurarTablaPresupuestosAsync
        // (para bases de datos que no pasan por EnsureCreated). Este índice es la garantía real.
        modelBuilder.Entity<PresupuestoEntity>()
            .HasIndex(p => new { p.ProveedorId, p.Mes, p.Anio })
            .IsUnique();

        // Como mucho un snapshot por día — ver también AsegurarTablaSnapshotsInventarioAsync.
        modelBuilder.Entity<SnapshotInventarioEntity>()
            .HasIndex(s => s.Fecha)
            .IsUnique();

        // Como mucho una marca de "stock realizado" por proveedor (0 = sin proveedor). Sin FK a propósito:
        // ver el comentario de StockRealizado.
        modelBuilder.Entity<StockRealizadoEntity>()
            .HasIndex(s => s.ProveedorId)
            .IsUnique();

        // Hojas de café: cada paquete nace en una hoja (Restrict: una hoja con paquetes no se borra) y es de un
        // producto (Cascade, como el resto del historial del producto). El registro de uso apunta, de forma
        // opcional, al paquete y a la hoja: SetNull para no perder el historial si algún día se borran.
        modelBuilder.Entity<PaqueteCafeEntity>()
            .HasOne(p => p.Producto).WithMany().HasForeignKey(p => p.ProductoId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PaqueteCafeEntity>()
            .HasOne(p => p.HojaOrigen).WithMany().HasForeignKey(p => p.HojaOrigenId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RegistroCafeEntity>()
            .HasOne(r => r.Paquete).WithMany().HasForeignKey(r => r.PaqueteId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<RegistroCafeEntity>()
            .HasOne(r => r.Hoja).WithMany().HasForeignKey(r => r.HojaId).OnDelete(DeleteBehavior.SetNull);
    }
}