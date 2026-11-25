using MaintenanceSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceSystem.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<Visit> Visits => Set<Visit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Property>(b =>
        {
            b.ToTable("Properties");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.Property(x => x.Address).HasMaxLength(250).IsRequired();
            b.Property(x => x.City).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Unit>(b =>
        {
            b.ToTable("Units");
            b.HasKey(x => x.Id);
            b.Property(x => x.Number).HasMaxLength(30).IsRequired();
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();

            b.HasIndex(x => new { x.PropertyId, x.Number }).IsUnique();
            b.HasOne(x => x.Property)
             .WithMany(p => p.Units)
             .HasForeignKey(x => x.PropertyId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tenant>(b =>
        {
            b.ToTable("Tenants");
            b.HasKey(x => x.Id);
            b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            b.Property(x => x.Email).HasMaxLength(150).IsRequired();
            b.Property(x => x.Phone).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Lease>(b =>
        {
            b.ToTable("Leases");
            b.HasKey(x => x.Id);
            b.Property(x => x.MonthlyRent).HasPrecision(18, 2);

            // Filtrado para impedir dos contratos abiertos simultáneamente en la misma unidad
            b.HasIndex(x => x.UnitId)
             .HasDatabaseName("UX_Leases_UnitId_OpenEnd")
             .IsUnique()
             .HasFilter("[EndDate] IS NULL");

            b.HasIndex(x => new { x.UnitId, x.StartDate, x.EndDate })
             .HasDatabaseName("IX_Leases_Unit_Dates");

            b.HasOne(x => x.Unit)
             .WithMany(u => u.Leases)
             .HasForeignKey(x => x.UnitId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Tenant)
             .WithMany(t => t.Leases)
             .HasForeignKey(x => x.TenantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Technician>(b =>
        {
            b.ToTable("Technicians");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.Property(x => x.Specialty).HasConversion<string>().HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.IsActive).HasDatabaseName("IX_Technicians_IsActive");
        });

        modelBuilder.Entity<MaintenanceRequest>(b =>
        {
            b.ToTable("MaintenanceRequests");
            b.HasKey(x => x.Id);
            b.Property(x => x.Category).HasMaxLength(100).IsRequired();
            b.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000).IsRequired();

            // Índices pensados para filtros y reportes
            b.HasIndex(x => new { x.Status, x.CreatedAt })
             .HasDatabaseName("IX_MaintenanceRequests_Status_CreatedAt");
            b.HasIndex(x => new { x.UnitId, x.Status, x.CreatedAt })
             .HasDatabaseName("IX_MaintenanceRequests_Unit_Status_CreatedAt");
            b.HasIndex(x => new { x.TechnicianId, x.Status })
             .HasDatabaseName("IX_MaintenanceRequests_Technician_Status");

            b.HasOne(x => x.Unit)
             .WithMany(u => u.MaintenanceRequests)
             .HasForeignKey(x => x.UnitId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Tenant)
             .WithMany(t => t.MaintenanceRequests)
             .HasForeignKey(x => x.TenantId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Technician)
             .WithMany(t => t.AssignedRequests)
             .HasForeignKey(x => x.TechnicianId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Visit>(b =>
        {
            b.ToTable("Visits", t => t.HasCheckConstraint("CK_Visits_Cost_NonNegative", "[Cost] >= 0"));
            b.HasKey(x => x.Id);
            b.Property(x => x.Notes).HasMaxLength(1000).IsRequired();
            b.Property(x => x.Cost).HasPrecision(18, 2);

            b.HasIndex(x => new { x.TechnicianId, x.VisitDate })
             .HasDatabaseName("IX_Visits_Technician_VisitDate");
            b.HasIndex(x => x.MaintenanceRequestId)
             .HasDatabaseName("IX_Visits_MaintenanceRequestId");

            b.HasOne(x => x.MaintenanceRequest)
             .WithMany(r => r.Visits)
             .HasForeignKey(x => x.MaintenanceRequestId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Technician)
             .WithMany(t => t.Visits)
             .HasForeignKey(x => x.TechnicianId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}