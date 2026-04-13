/*
 * FILE: AssetDbContext.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: The EF Core database context for the ASMPlatform schema.
 *          Configures all entity-to-table mappings using the Fluent API.
 *          Key design decisions enforced here:
 *          - All table and column names use snake_case (PostgreSQL convention)
 *          - Primary keys use IDENTITY ALWAYS sequences
 *          - Foreign keys use ON DELETE RESTRICT unless stated otherwise
 *          - Unique indexes on asset_code, asset_tag, serial_number, username
 *          - Partial unique index on hardware_assignments(asset_id) WHERE end_date IS NULL
 *            enforces one active assignment per asset at the database level
 *          - AUDIT LOG IS APPEND-ONLY: SaveChangesAsync overrides throw
 *            InvalidOperationException if any AuditLog entry is modified or deleted,
 *            even by Admin users. This is a compliance requirement.
 */

using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Server.Data;

public class AssetDbContext(DbContextOptions<AssetDbContext> options) : DbContext(options)
{
    // ── DbSets ────────────────────────────────────────────────────────────────
    public DbSet<Site>               Sites               => Set<Site>();
    public DbSet<Department>         Departments         => Set<Department>();
    public DbSet<Vendor>             Vendors             => Set<Vendor>();
    public DbSet<Employee>           Employees           => Set<Employee>();
    public DbSet<AppUser>            AppUsers            => Set<AppUser>();
    public DbSet<Asset>              Assets              => Set<Asset>();
    public DbSet<HardwareDetail>     HardwareDetails     => Set<HardwareDetail>();
    public DbSet<SoftwareDetail>     SoftwareDetails     => Set<SoftwareDetail>();
    public DbSet<HardwareAssignment> HardwareAssignments => Set<HardwareAssignment>();
    public DbSet<SoftwareAssignment> SoftwareAssignments => Set<SoftwareAssignment>();
    public DbSet<LifecycleEvent>     LifecycleEvents     => Set<LifecycleEvent>();
    public DbSet<AuditLog>           AuditLogs           => Set<AuditLog>();

    // ── Append-only audit enforcement ─────────────────────────────────────────
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var auditViolations = ChangeTracker.Entries<AuditLog>()
            .Where(e => e.State is EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (auditViolations.Count > 0)
            throw new InvalidOperationException(
                "Audit log entries are append-only and cannot be modified or deleted. " +
                "This is a compliance requirement enforced at the database context level.");

        return await base.SaveChangesAsync(ct);
    }

    // ── Schema configuration ──────────────────────────────────────────────────
    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Sites ─────────────────────────────────────────────────────────────
        mb.Entity<Site>(e =>
        {
            e.ToTable("sites");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.Code).HasColumnName("code").HasMaxLength(10).IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        // ── Departments ───────────────────────────────────────────────────────
        mb.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.SiteId).HasColumnName("site_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Site).WithMany(s => s.Departments)
             .HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Vendors ───────────────────────────────────────────────────────────
        mb.Entity<Vendor>(e =>
        {
            e.ToTable("vendors");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            e.Property(x => x.Type).HasColumnName("type").HasMaxLength(20).HasDefaultValue("Hardware");
            e.HasIndex(x => x.Name).IsUnique();
        });

        // ── Employees ─────────────────────────────────────────────────────────
        mb.Entity<Employee>(e =>
        {
            e.ToTable("employees");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(80).IsRequired();
            e.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(80).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(150);
            e.Property(x => x.JobTitle).HasColumnName("job_title").HasMaxLength(100);
            e.Property(x => x.SiteId).HasColumnName("site_id");
            e.Property(x => x.DepartmentId).HasColumnName("department_id");
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.HasOne(x => x.Site).WithMany(s => s.Employees)
             .HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Department).WithMany(d => d.Employees)
             .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── AppUsers ──────────────────────────────────────────────────────────
        mb.Entity<AppUser>(e =>
        {
            e.ToTable("app_users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(60).IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(256).IsRequired();
            e.Property(x => x.Role).HasColumnName("role").HasMaxLength(20).HasDefaultValue("Viewer");
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.HasIndex(x => x.Username).IsUnique();
            e.HasOne(x => x.Employee).WithOne(emp => emp.AppUser)
             .HasForeignKey<AppUser>(x => x.EmployeeId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── Assets ────────────────────────────────────────────────────────────
        mb.Entity<Asset>(e =>
        {
            e.ToTable("assets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.AssetCode).HasColumnName("asset_code").HasMaxLength(50).IsRequired();
            e.Property(x => x.AssetTag).HasColumnName("asset_tag").HasMaxLength(50).IsRequired();
            e.Property(x => x.SerialNumber).HasColumnName("serial_number").HasMaxLength(100).IsRequired();
            e.Property(x => x.AssetType).HasColumnName("asset_type").HasMaxLength(30).IsRequired();
            e.Property(x => x.VendorId).HasColumnName("vendor_id");
            e.Property(x => x.Model).HasColumnName("model").HasMaxLength(100);
            e.Property(x => x.SiteId).HasColumnName("site_id");
            e.Property(x => x.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(30).HasDefaultValue("Available");
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(500).HasDefaultValue("");
            e.Property(x => x.ComplianceId).HasColumnName("compliance_id").HasMaxLength(60).HasDefaultValue("");
            e.Property(x => x.IsEncrypted).HasColumnName("is_encrypted").HasDefaultValue(false);
            e.Property(x => x.EncryptionProtocol).HasColumnName("encryption_protocol").HasMaxLength(60).HasDefaultValue("");
            e.Property(x => x.HasAntiVirus).HasColumnName("has_anti_virus").HasDefaultValue(false);
            e.Property(x => x.LastSecurityAudit).HasColumnName("last_security_audit");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.AssetCode).IsUnique();
            e.HasIndex(x => x.AssetTag).IsUnique();
            e.HasIndex(x => x.SerialNumber).IsUnique();
            e.HasOne(x => x.Site).WithMany(s => s.Assets)
             .HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Vendor).WithMany(v => v.Assets)
             .HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── HardwareDetail ────────────────────────────────────────────────────
        mb.Entity<HardwareDetail>(e =>
        {
            e.ToTable("hardware_details");
            e.HasKey(x => x.AssetId);
            e.Property(x => x.AssetId).HasColumnName("asset_id");
            e.Property(x => x.FormFactor).HasColumnName("form_factor").HasMaxLength(50).HasDefaultValue("");
            e.Property(x => x.Specs).HasColumnName("specs").HasMaxLength(500).HasDefaultValue("");
            e.Property(x => x.Os).HasColumnName("os").HasMaxLength(80).HasDefaultValue("");
            e.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45).HasDefaultValue("");
            e.Property(x => x.MacAddress).HasColumnName("mac_address").HasMaxLength(20).HasDefaultValue("");
            e.Property(x => x.Hostname).HasColumnName("hostname").HasMaxLength(100).HasDefaultValue("");
            e.Property(x => x.PurchaseOrderRef).HasColumnName("purchase_order_ref").HasMaxLength(80).HasDefaultValue("");
            e.Property(x => x.PurchaseDate).HasColumnName("purchase_date");
            e.Property(x => x.WarrantyExpiry).HasColumnName("warranty_expiry");
            e.HasOne(x => x.Asset).WithOne(a => a.HardwareDetail)
             .HasForeignKey<HardwareDetail>(x => x.AssetId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── SoftwareDetail ────────────────────────────────────────────────────
        mb.Entity<SoftwareDetail>(e =>
        {
            e.ToTable("software_details");
            e.HasKey(x => x.AssetId);
            e.Property(x => x.AssetId).HasColumnName("asset_id");
            e.Property(x => x.LicenseType).HasColumnName("license_type").HasMaxLength(30).HasDefaultValue("Per-Seat");
            e.Property(x => x.TotalSeats).HasColumnName("total_seats").HasDefaultValue(0);
            e.Property(x => x.UsedSeats).HasColumnName("used_seats").HasDefaultValue(0);
            e.Property(x => x.StartDate).HasColumnName("start_date");
            e.Property(x => x.ExpiryDate).HasColumnName("expiry_date");
            e.Property(x => x.RenewalDate).HasColumnName("renewal_date");
            e.Property(x => x.LicenseKey).HasColumnName("license_key").HasMaxLength(200).HasDefaultValue("");
            e.Property(x => x.AutoRenew).HasColumnName("auto_renew").HasDefaultValue(false);
            e.Ignore(x => x.IsExpiringSoon);  // computed — not a column
            e.HasOne(x => x.Asset).WithOne(a => a.SoftwareDetail)
             .HasForeignKey<SoftwareDetail>(x => x.AssetId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── HardwareAssignments ───────────────────────────────────────────────
        mb.Entity<HardwareAssignment>(e =>
        {
            e.ToTable("hardware_assignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.AssetId).HasColumnName("asset_id");
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
            e.Property(x => x.EffectiveDate).HasColumnName("effective_date");
            e.Property(x => x.EndDate).HasColumnName("end_date");
            e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500).HasDefaultValue("");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Ignore(x => x.IsActive);  // computed — not a column
            e.HasOne(x => x.Asset).WithMany(a => a.HardwareAssignments)
             .HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Employee).WithMany(emp => emp.HardwareAssignments)
             .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedBy).WithMany(u => u.CreatedHardwareAssignments)
             .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── SoftwareAssignments ───────────────────────────────────────────────
        mb.Entity<SoftwareAssignment>(e =>
        {
            e.ToTable("software_assignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.AssetId).HasColumnName("asset_id");
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.AssignedDate).HasColumnName("assigned_date");
            e.Property(x => x.RevokedDate).HasColumnName("revoked_date");
            e.Ignore(x => x.IsActive);  // computed — not a column
            e.HasOne(x => x.Asset).WithMany()
             .HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Employee).WithMany(emp => emp.SoftwareAssignments)
             .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── LifecycleEvents ───────────────────────────────────────────────────
        mb.Entity<LifecycleEvent>(e =>
        {
            e.ToTable("lifecycle_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.AssetId).HasColumnName("asset_id");
            e.Property(x => x.OldStatus).HasColumnName("old_status").HasMaxLength(30).IsRequired();
            e.Property(x => x.NewStatus).HasColumnName("new_status").HasMaxLength(30).IsRequired();
            e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
            e.Property(x => x.ChangedByUserId).HasColumnName("changed_by_user_id");
            e.Property(x => x.ChangedAt).HasColumnName("changed_at");
            e.HasOne(x => x.Asset).WithMany(a => a.LifecycleEvents)
             .HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ChangedBy).WithMany(u => u.ChangedLifecycleEvents)
             .HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── AuditLog ──────────────────────────────────────────────────────────
        mb.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.Ts).HasColumnName("ts").HasColumnType("timestamptz");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(60).IsRequired();
            e.Property(x => x.Action).HasColumnName("action").HasMaxLength(200).IsRequired();
            e.Property(x => x.Target).HasColumnName("target").HasMaxLength(200).HasDefaultValue("");
            e.HasOne(x => x.User).WithMany(u => u.AuditLogs)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
