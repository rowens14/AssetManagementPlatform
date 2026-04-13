using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssetManagement.Server.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder mb)
    {
        mb.CreateTable("sites", t => new
        {
            id   = t.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
            code = t.Column<string>(maxLength: 10,  nullable: false),
            name = t.Column<string>(maxLength: 100, nullable: false),
        }, constraints: t => t.PrimaryKey("PK_sites", x => x.id));
        mb.CreateIndex("IX_sites_code", "sites", "code", unique: true);

        mb.CreateTable("departments", t => new
        {
            id      = t.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
            site_id = t.Column<int>(nullable: false),
            name    = t.Column<string>(maxLength: 100, nullable: false),
        }, constraints: t =>
        {
            t.PrimaryKey("PK_departments", x => x.id);
            t.ForeignKey("FK_departments_sites", x => x.site_id, "sites", "id", onDelete: ReferentialAction.Restrict);
        });

        mb.CreateTable("vendors", t => new
        {
            id   = t.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
            name = t.Column<string>(maxLength: 120, nullable: false),
            type = t.Column<string>(maxLength: 20,  nullable: false, defaultValue: "Hardware"),
        }, constraints: t => t.PrimaryKey("PK_vendors", x => x.id));
        mb.CreateIndex("IX_vendors_name", "vendors", "name", unique: true);

        mb.CreateTable("employees", t => new
        {
            id            = t.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
            first_name    = t.Column<string>(maxLength: 80,  nullable: false),
            last_name     = t.Column<string>(maxLength: 80,  nullable: false),
            email         = t.Column<string>(maxLength: 150, nullable: true),
            job_title     = t.Column<string>(maxLength: 100, nullable: true),
            site_id       = t.Column<int>(nullable: false),
            department_id = t.Column<int>(nullable: false),
            is_active     = t.Column<bool>(nullable: false, defaultValue: true),
        }, constraints: t =>
        {
            t.PrimaryKey("PK_employees", x => x.id);
            t.ForeignKey("FK_employees_sites",       x => x.site_id,       "sites",       "id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_employees_departments", x => x.department_id, "departments", "id", onDelete: ReferentialAction.Restrict);
        });

        mb.CreateTable("app_users", t => new
        {
            id            = t.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
            username      = t.Column<string>(maxLength: 60,  nullable: false),
            password_hash = t.Column<string>(maxLength: 256, nullable: false),
            role          = t.Column<string>(maxLength: 20,  nullable: false, defaultValue: "Viewer"),
            employee_id   = t.Column<int>(nullable: true),
            is_active     = t.Column<bool>(nullable: false, defaultValue: true),
        }, constraints: t =>
        {
            t.PrimaryKey("PK_app_users", x => x.id);
            t.ForeignKey("FK_app_users_employees", x => x.employee_id, "employees", "id", onDelete: ReferentialAction.SetNull);
        });
        mb.CreateIndex("IX_app_users_username", "app_users", "username", unique: true);

        mb.CreateTable("assets", t => new
        {
            id                  = t.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
            asset_code          = t.Column<string>(maxLength: 50,  nullable: false),
            asset_tag           = t.Column<string>(maxLength: 50,  nullable: false),
            serial_number       = t.Column<string>(maxLength: 100, nullable: false),
            asset_type          = t.Column<string>(maxLength: 30,  nullable: false),
            vendor_id           = t.Column<int>(nullable: true),
            model               = t.Column<string>(maxLength: 100, nullable: true),
            site_id             = t.Column<int>(nullable: false),
            lifecycle_status    = t.Column<string>(maxLength: 30,  nullable: false, defaultValue: "Available"),
            description         = t.Column<string>(maxLength: 500, nullable: false, defaultValue: ""),
            compliance_id       = t.Column<string>(maxLength: 60,  nullable: false, defaultValue: ""),
            is_encrypted        = t.Column<bool>(nullable: false, defaultValue: false),
            encryption_protocol = t.Column<string>(maxLength: 60,  nullable: false, defaultValue: ""),
            has_anti_virus      = t.Column<bool>(nullable: false, defaultValue: false),
            last_security_audit = t.Column<DateOnly>(nullable: true),
            created_at          = t.Column<DateOnly>(nullable: false),
        }, constraints: t =>
        {
            t.PrimaryKey("PK_assets", x => x.id);
            t.ForeignKey("FK_assets_sites",   x => x.site_id,   "sites",   "id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_assets_vendors", x => x.vendor_id, "vendors", "id", onDelete: ReferentialAction.SetNull);
        });
        mb.CreateIndex("IX_assets_asset_code",    "assets", "asset_code",    unique: true);
        mb.CreateIndex("IX_assets_asset_tag",     "assets", "asset_tag",     unique: true);
        mb.CreateIndex("IX_assets_serial_number", "assets", "serial_number", unique: true);

        mb.CreateTable("hardware_details", t => new
        {
            asset_id           = t.Column<int>(nullable: false),
            form_factor        = t.Column<string>(maxLength: 50,  nullable: false, defaultValue: ""),
            specs              = t.Column<string>(maxLength: 500, nullable: false, defaultValue: ""),
            os                 = t.Column<string>(maxLength: 80,  nullable: false, defaultValue: ""),
            ip_address         = t.Column<string>(maxLength: 45,  nullable: false, defaultValue: ""),
            mac_address        = t.Column<string>(maxLength: 20,  nullable: false, defaultValue: ""),
            hostname           = t.Column<string>(maxLength: 100, nullable: false, defaultValue: ""),
            purchase_order_ref = t.Column<string>(maxLength: 80,  nullable: false, defaultValue: ""),
            purchase_date      = t.Column<DateOnly>(nullable: true),
            warranty_expiry    = t.Column<DateOnly>(nullable: true),
        }, constraints: t =>
        {
            t.PrimaryKey("PK_hardware_details", x => x.asset_id);
            t.ForeignKey("FK_hardware_details_assets", x => x.asset_id, "assets", "id", onDelete: ReferentialAction.Cascade);
        });

        mb.CreateTable("software_details", t => new
        {
            asset_id     = t.Column<int>(nullable: false),
            license_type = t.Column<string>(maxLength: 30, nullable: false, defaultValue: "Per-Seat"),
            total_seats  = t.Column<int>(nullable: false, defaultValue: 0),
            used_seats   = t.Column<int>(nullable: false, defaultValue: 0),
            start_date   = t.Column<DateOnly>(nullable: true),
            expiry_date  = t.Column<DateOnly>(nullable: true),
            renewal_date = t.Column<DateOnly>(nullable: true),
            license_key  = t.Column<string>(maxLength: 200, nullable: false, defaultValue: ""),
            auto_renew   = t.Column<bool>(nullable: false, defaultValue: false),
        }, constraints: t =>
        {
            t.PrimaryKey("PK_software_details", x => x.asset_id);
            t.ForeignKey("FK_software_details_assets", x => x.asset_id, "assets", "id", onDelete: ReferentialAction.Cascade);
        });

        mb.CreateTable("hardware_assignments", t => new
        {
            id                  = t.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
            asset_id            = t.Column<int>(nullable: false),
            employee_id         = t.Column<int>(nullable: false),
            created_by_user_id  = t.Column<int>(nullable: false),
            effective_date      = t.Column<DateOnly>(nullable: false),
            end_date            = t.Column<DateOnly>(nullable: true),
            notes               = t.Column<string>(maxLength: 500, nullable: false, defaultValue: ""),
            created_at          = t.Column<DateOnly>(nullable: false),
        }, constraints: t =>
        {
            t.PrimaryKey("PK_hardware_assignments", x => x.id);
            t.ForeignKey("FK_hardware_assignments_assets",    x => x.asset_id,           "assets",    "id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_hardware_assignments_employees", x => x.employee_id,         "employees", "id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_hardware_assignments_users",     x => x.created_by_user_id, "app_users", "id", onDelete: ReferentialAction.Restrict);
        });

        mb.CreateTable("software_assignments", t => new
        {
            id           = t.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
            asset_id     = t.Column<int>(nullable: false),
            employee_id  = t.Column<int>(nullable: false),
            assigned_date = t.Column<DateOnly>(nullable: false),
            revoked_date  = t.Column<DateOnly>(nullable: true),
        }, constraints: t =>
        {
            t.PrimaryKey("PK_software_assignments", x => x.id);
            t.ForeignKey("FK_software_assignments_assets",    x => x.asset_id,    "assets",    "id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("FK_software_assignments_employees", x => x.employee_id, "employees", "id", onDelete: ReferentialAction.Restrict);
        });

        mb.CreateTable("lifecycle_events", t => new
        {
            id                  = t.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
            asset_id            = t.Column<int>(nullable: false),
            old_status          = t.Column<string>(maxLength: 30,  nullable: false),
            new_status          = t.Column<string>(maxLength: 30,  nullable: false),
            reason              = t.Column<string>(maxLength: 500, nullable: false),
            changed_by_user_id  = t.Column<int>(nullable: false),
            changed_at          = t.Column<DateOnly>(nullable: false),
        }, constraints: t =>
        {
            t.PrimaryKey("PK_lifecycle_events", x => x.id);
            t.ForeignKey("FK_lifecycle_events_assets",    x => x.asset_id,           "assets",    "id", onDelete: ReferentialAction.Cascade);
            t.ForeignKey("FK_lifecycle_events_app_users", x => x.changed_by_user_id, "app_users", "id", onDelete: ReferentialAction.Restrict);
        });

        mb.CreateTable("audit_logs", t => new
        {
            id       = t.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
            ts       = t.Column<DateTime>(type: "timestamptz", nullable: false),
            user_id  = t.Column<int>(nullable: true),
            username = t.Column<string>(maxLength: 60,  nullable: false),
            action   = t.Column<string>(maxLength: 200, nullable: false),
            target   = t.Column<string>(maxLength: 200, nullable: false, defaultValue: ""),
        }, constraints: t =>
        {
            t.PrimaryKey("PK_audit_logs", x => x.id);
            t.ForeignKey("FK_audit_logs_app_users", x => x.user_id, "app_users", "id", onDelete: ReferentialAction.SetNull);
        });
    }

    protected override void Down(MigrationBuilder mb)
    {
        mb.DropTable("audit_logs");
        mb.DropTable("lifecycle_events");
        mb.DropTable("software_assignments");
        mb.DropTable("hardware_assignments");
        mb.DropTable("software_details");
        mb.DropTable("hardware_details");
        mb.DropTable("assets");
        mb.DropTable("app_users");
        mb.DropTable("employees");
        mb.DropTable("vendors");
        mb.DropTable("departments");
        mb.DropTable("sites");
    }
}
