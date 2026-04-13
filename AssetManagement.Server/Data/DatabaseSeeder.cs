/*
 * FILE: DatabaseSeeder.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: Seeds the PostgreSQL database with initial reference data and demonstration
 *          records on first startup. Called from Program.cs after EnsureCreatedAsync().
 *          Guards with an AnyAsync check so it only runs on an empty database.
 *          Seeds (in dependency order):
 *          - 2 Sites: Naples (NAP), Fort Myers (FTM)
 *          - 6 Departments: Litigation, HR, Finance, Partners, Paralegals, IT
 *          - Vendors: Dell, Apple, HP, Cisco, Canon, Lenovo, Microsoft, Adobe, Clio, Zoom
 *          - 8 Employees across both sites
 *          - 5 AppUser login accounts with bcrypt-hashed passwords
 *          - 8 Hardware Assets with HardwareDetail records
 *          - 4 Software Assets with SoftwareDetail records (licenses)
 *          - 4 HardwareAssignments
 *          - 4 LifecycleEvents
 *          - 7 AuditLog entries
 */

using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Server.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AssetDbContext db)
    {
        if (await db.Sites.AnyAsync()) return;

        // ── Sites ─────────────────────────────────────────────────────────────
        var naples    = new Site { Code = "NAP", Name = "Naples" };
        var fortMyers = new Site { Code = "FTM", Name = "Fort Myers" };
        db.Sites.AddRange(naples, fortMyers);
        await db.SaveChangesAsync();

        // ── Departments ───────────────────────────────────────────────────────
        var litigation   = new Department { SiteId = naples.Id,    Name = "Litigation" };
        var hr           = new Department { SiteId = naples.Id,    Name = "HR" };
        var finance      = new Department { SiteId = naples.Id,    Name = "Finance" };
        var partners     = new Department { SiteId = fortMyers.Id, Name = "Partners" };
        var paralegals   = new Department { SiteId = fortMyers.Id, Name = "Paralegals" };
        var it           = new Department { SiteId = fortMyers.Id, Name = "IT" };
        db.Departments.AddRange(litigation, hr, finance, partners, paralegals, it);
        await db.SaveChangesAsync();

        // ── Vendors ───────────────────────────────────────────────────────────
        var dell      = new Vendor { Name = "Dell",      Type = "Hardware" };
        var apple     = new Vendor { Name = "Apple",     Type = "Hardware" };
        var hp        = new Vendor { Name = "HP",        Type = "Hardware" };
        var cisco     = new Vendor { Name = "Cisco",     Type = "Hardware" };
        var canon     = new Vendor { Name = "Canon",     Type = "Hardware" };
        var lenovo    = new Vendor { Name = "Lenovo",    Type = "Hardware" };
        var microsoft = new Vendor { Name = "Microsoft", Type = "Software" };
        var adobe     = new Vendor { Name = "Adobe",     Type = "Software" };
        var clio      = new Vendor { Name = "Clio",      Type = "Software" };
        var zoom      = new Vendor { Name = "Zoom",      Type = "Software" };
        db.Vendors.AddRange(dell, apple, hp, cisco, canon, lenovo, microsoft, adobe, clio, zoom);
        await db.SaveChangesAsync();

        // ── Employees ─────────────────────────────────────────────────────────
        var empBrandon = new Employee { FirstName="Brandon", LastName="Campbell", Email="b.campbell@zeldasamus.com", JobTitle="IT Administrator",    SiteId=naples.Id,    DepartmentId=it.Id,          IsActive=true };
        var empMax     = new Employee { FirstName="Max",      LastName="Owens",    Email="m.owens@zeldasamus.com",    JobTitle="Senior Litigator",   SiteId=naples.Id,    DepartmentId=litigation.Id,  IsActive=true };
        var empNathan  = new Employee { FirstName="Nathan",   LastName="Laws",     Email="n.laws@zeldasamus.com",     JobTitle="Paralegal",          SiteId=fortMyers.Id, DepartmentId=paralegals.Id,  IsActive=true };
        var empDave    = new Employee { FirstName="Dave",     LastName="Bernard",  Email="d.bernard@zeldasamus.com",  JobTitle="Partner",            SiteId=fortMyers.Id, DepartmentId=partners.Id,    IsActive=true };
        var empJane    = new Employee { FirstName="Jane",     LastName="Smith",    Email="j.smith@zeldasamus.com",    JobTitle="HR Manager",         SiteId=naples.Id,    DepartmentId=hr.Id,          IsActive=true };
        var empAlice   = new Employee { FirstName="Alice",    LastName="Wong",     Email="a.wong@zeldasamus.com",     JobTitle="Finance Analyst",    SiteId=naples.Id,    DepartmentId=finance.Id,     IsActive=true };
        var empRobert  = new Employee { FirstName="Robert",   LastName="Torres",   Email="r.torres@zeldasamus.com",   JobTitle="Associate Attorney", SiteId=fortMyers.Id, DepartmentId=paralegals.Id,  IsActive=true };
        var empSarah   = new Employee { FirstName="Sarah",    LastName="Kim",      Email="s.kim@zeldasamus.com",      JobTitle="Office Manager",     SiteId=fortMyers.Id, DepartmentId=it.Id,          IsActive=true };
        db.Employees.AddRange(empBrandon, empMax, empNathan, empDave, empJane, empAlice, empRobert, empSarah);
        await db.SaveChangesAsync();

        // ── AppUsers (bcrypt-hashed passwords) ────────────────────────────────
        var userAdmin = new AppUser
        {
            Username = "admin", Role = "Admin", IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            EmployeeId = empBrandon.Id
        };
        var userManager = new AppUser
        {
            Username = "manager", Role = "Manager", IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass123"),
            EmployeeId = empMax.Id
        };
        var userViewer = new AppUser
        {
            Username = "viewer", Role = "Viewer", IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("view123"),
            EmployeeId = empNathan.Id
        };
        var userDbernard = new AppUser
        {
            Username = "dbernard", Role = "Manager", IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass456"),
            EmployeeId = empDave.Id
        };
        var userJsmith = new AppUser
        {
            Username = "jsmith", Role = "Viewer", IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass789"),
            EmployeeId = empJane.Id
        };
        db.AppUsers.AddRange(userAdmin, userManager, userViewer, userDbernard, userJsmith);
        await db.SaveChangesAsync();

        // ── Hardware Assets + HardwareDetail ──────────────────────────────────
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var asset1 = new Asset { AssetCode="HW-2026-001", AssetTag="TAG-1001", SerialNumber="SN-DELL-X1234",  AssetType="Laptop",  VendorId=dell.Id,   Model="Latitude 5540",       SiteId=naples.Id,    LifecycleStatus="Deployed",    Description="Primary workstation",      ComplianceId="CMP-001", IsEncrypted=true, EncryptionProtocol="BitLocker", HasAntiVirus=true, LastSecurityAudit=new DateOnly(2025,11,1),  CreatedAt=new DateOnly(2025,1,15) };
        var asset2 = new Asset { AssetCode="HW-2026-002", AssetTag="TAG-1002", SerialNumber="SN-APPLE-M9876", AssetType="Laptop",  VendorId=apple.Id,  Model="MacBook Pro 14-inch", SiteId=naples.Id,    LifecycleStatus="Available",   Description="",                         ComplianceId="",        IsEncrypted=true, EncryptionProtocol="FileVault",  HasAntiVirus=true, LastSecurityAudit=new DateOnly(2025,10,15), CreatedAt=new DateOnly(2025,2,10) };
        var asset3 = new Asset { AssetCode="HW-2026-003", AssetTag="TAG-1003", SerialNumber="SN-HP-M4561",    AssetType="Desktop", VendorId=hp.Id,     Model="EliteDesk 800 G9",    SiteId=fortMyers.Id, LifecycleStatus="Deployed",    Description="Reception desk",           ComplianceId="CMP-003", IsEncrypted=true, EncryptionProtocol="BitLocker",  HasAntiVirus=true, LastSecurityAudit=new DateOnly(2025,9,20),  CreatedAt=new DateOnly(2025,3,1)  };
        var asset4 = new Asset { AssetCode="HW-2026-004", AssetTag="TAG-1004", SerialNumber="SN-CISCO-R8541", AssetType="Switch",  VendorId=cisco.Id,  Model="Catalyst 2960X",      SiteId=naples.Id,    LifecycleStatus="Deployed",    Description="Core network switch",      ComplianceId="CMP-004", IsEncrypted=false, EncryptionProtocol="",          HasAntiVirus=false,LastSecurityAudit=new DateOnly(2025,8,1),   CreatedAt=new DateOnly(2024,12,1) };
        var asset5 = new Asset { AssetCode="HW-2026-005", AssetTag="TAG-1005", SerialNumber="SN-CANON-P2233", AssetType="Printer", VendorId=canon.Id,  Model="imageCLASS MF644Cdw", SiteId=fortMyers.Id, LifecycleStatus="Maintenance", Description="Paper jam hardware fault", ComplianceId="",        IsEncrypted=false, EncryptionProtocol="",          HasAntiVirus=false,LastSecurityAudit=null,                     CreatedAt=new DateOnly(2024,11,10)};
        var asset6 = new Asset { AssetCode="HW-2026-006", AssetTag="TAG-1006", SerialNumber="SN-DELL-M7821",  AssetType="Monitor", VendorId=dell.Id,   Model="UltraSharp U2723DE",  SiteId=naples.Id,    LifecycleStatus="Available",   Description="",                         ComplianceId="",        IsEncrypted=false, EncryptionProtocol="",          HasAntiVirus=false,LastSecurityAudit=null,                     CreatedAt=new DateOnly(2025,4,1)  };
        var asset7 = new Asset { AssetCode="HW-2026-007", AssetTag="TAG-1007", SerialNumber="SN-LEN-T1928",   AssetType="Laptop",  VendorId=lenovo.Id, Model="ThinkPad X1 Carbon",  SiteId=fortMyers.Id, LifecycleStatus="Retired",     Description="End-of-life, 2019 model",  ComplianceId="",        IsEncrypted=true,  EncryptionProtocol="BitLocker",  HasAntiVirus=false,LastSecurityAudit=new DateOnly(2024,6,1),   CreatedAt=new DateOnly(2019,5,1)  };
        var asset8 = new Asset { AssetCode="LAW-001",     AssetTag="LAW-001",  SerialNumber="SN123456",       AssetType="Laptop",  VendorId=dell.Id,   Model="Latitude 7420",       SiteId=fortMyers.Id, LifecycleStatus="Deployed",    Description="Migrated from backup",     ComplianceId="",        IsEncrypted=false, EncryptionProtocol="",          HasAntiVirus=false,LastSecurityAudit=null,                     CreatedAt=new DateOnly(2024,1,15) };
        db.Assets.AddRange(asset1, asset2, asset3, asset4, asset5, asset6, asset7, asset8);
        await db.SaveChangesAsync();

        db.HardwareDetails.AddRange(
            new HardwareDetail { AssetId=asset1.Id, FormFactor="Laptop",  Os="Windows 11 Pro", Hostname="NAP-LT-001",  Specs="Intel i7 / 16GB / 512GB SSD", PurchaseDate=new DateOnly(2025,1,10),  WarrantyExpiry=new DateOnly(2028,1,10)  },
            new HardwareDetail { AssetId=asset2.Id, FormFactor="Laptop",  Os="macOS Sonoma",   Hostname="",            Specs="Apple M3 Pro / 18GB / 512GB",  PurchaseDate=new DateOnly(2025,2,5),   WarrantyExpiry=new DateOnly(2027,2,5)   },
            new HardwareDetail { AssetId=asset3.Id, FormFactor="Desktop", Os="Windows 11 Pro", Hostname="FTM-DT-001",  Specs="Intel i5 / 8GB / 256GB SSD",  PurchaseDate=new DateOnly(2025,2,20),  WarrantyExpiry=new DateOnly(2028,2,20)  },
            new HardwareDetail { AssetId=asset4.Id, FormFactor="Switch",  Os="Cisco IOS",      Hostname="NAP-SW-CORE", Specs="24-port 1G + 4x SFP+",        PurchaseDate=new DateOnly(2024,11,15), WarrantyExpiry=new DateOnly(2027,11,15) },
            new HardwareDetail { AssetId=asset5.Id, FormFactor="Printer", Os="",               Hostname="",            Specs="Colour laser, duplex",         PurchaseDate=new DateOnly(2024,10,1),  WarrantyExpiry=new DateOnly(2027,10,1)  },
            new HardwareDetail { AssetId=asset6.Id, FormFactor="Monitor", Os="",               Hostname="",            Specs="27-inch 4K USB-C",             PurchaseDate=new DateOnly(2025,3,15),  WarrantyExpiry=new DateOnly(2028,3,15)  },
            new HardwareDetail { AssetId=asset7.Id, FormFactor="Laptop",  Os="Windows 10",     Hostname="FTM-LT-OLD1", Specs="Intel i5 / 8GB / 256GB",       PurchaseDate=new DateOnly(2019,4,1),   WarrantyExpiry=new DateOnly(2022,4,1)   },
            new HardwareDetail { AssetId=asset8.Id, FormFactor="Laptop",  Os="Windows 11",     Hostname="",            Specs="Intel i5 / 8GB / 256GB",       PurchaseDate=new DateOnly(2024,1,5),   WarrantyExpiry=new DateOnly(2027,1,5)   }
        );

        // ── Software Assets + SoftwareDetail ──────────────────────────────────
        var sw1 = new Asset { AssetCode="SW-M365-NAP",  AssetTag="SW-M365-NAP",  SerialNumber="SW-001", AssetType="Software", VendorId=microsoft.Id, Model="Microsoft 365 Business", SiteId=naples.Id,    LifecycleStatus="Available", Description="Office productivity suite — Naples",    CreatedAt=today };
        var sw2 = new Asset { AssetCode="SW-M365-FTM",  AssetTag="SW-M365-FTM",  SerialNumber="SW-002", AssetType="Software", VendorId=microsoft.Id, Model="Microsoft 365 Business", SiteId=fortMyers.Id, LifecycleStatus="Available", Description="Office productivity suite — Fort Myers", CreatedAt=today };
        var sw3 = new Asset { AssetCode="SW-CLIO-ALL",  AssetTag="SW-CLIO-ALL",  SerialNumber="SW-003", AssetType="Software", VendorId=clio.Id,      Model="Clio Manage",            SiteId=naples.Id,    LifecycleStatus="Available", Description="Practice management platform",          CreatedAt=today };
        var sw4 = new Asset { AssetCode="SW-ZOOM-FTM",  AssetTag="SW-ZOOM-FTM",  SerialNumber="SW-004", AssetType="Software", VendorId=zoom.Id,      Model="Zoom Meetings",          SiteId=fortMyers.Id, LifecycleStatus="Available", Description="Video conferencing — Fort Myers",       CreatedAt=today };
        db.Assets.AddRange(sw1, sw2, sw3, sw4);
        await db.SaveChangesAsync();

        db.SoftwareDetails.AddRange(
            new SoftwareDetail { AssetId=sw1.Id, LicenseType="Subscription", TotalSeats=15, UsedSeats=12, StartDate=new DateOnly(2025,1,1), ExpiryDate=new DateOnly(2026,1,1),  AutoRenew=true  },
            new SoftwareDetail { AssetId=sw2.Id, LicenseType="Subscription", TotalSeats=10, UsedSeats=8,  StartDate=new DateOnly(2025,1,1), ExpiryDate=new DateOnly(2026,1,1),  AutoRenew=true  },
            new SoftwareDetail { AssetId=sw3.Id, LicenseType="Enterprise",   TotalSeats=35, UsedSeats=22, StartDate=new DateOnly(2025,3,1), ExpiryDate=new DateOnly(2026,3,1),  AutoRenew=true  },
            new SoftwareDetail { AssetId=sw4.Id, LicenseType="Volume",       TotalSeats=25, UsedSeats=18, StartDate=new DateOnly(2025,2,15),ExpiryDate=new DateOnly(2026,2,15), AutoRenew=false }
        );

        // ── HardwareAssignments ───────────────────────────────────────────────
        db.HardwareAssignments.AddRange(
            new HardwareAssignment { AssetId=asset1.Id, EmployeeId=empMax.Id,   CreatedByUserId=userAdmin.Id, EffectiveDate=new DateOnly(2025,1,20), EndDate=null,                    Notes="Primary laptop",         CreatedAt=new DateOnly(2025,1,20) },
            new HardwareAssignment { AssetId=asset3.Id, EmployeeId=empNathan.Id,CreatedByUserId=userAdmin.Id, EffectiveDate=new DateOnly(2025,3,5),  EndDate=null,                    Notes="Reception desk",         CreatedAt=new DateOnly(2025,3,5)  },
            new HardwareAssignment { AssetId=asset4.Id, EmployeeId=empDave.Id,  CreatedByUserId=userAdmin.Id, EffectiveDate=new DateOnly(2025,1,1),  EndDate=null,                    Notes="Network infrastructure", CreatedAt=new DateOnly(2025,1,1)  },
            new HardwareAssignment { AssetId=asset6.Id, EmployeeId=empJane.Id,  CreatedByUserId=userAdmin.Id, EffectiveDate=new DateOnly(2025,4,10), EndDate=new DateOnly(2025,9,30), Notes="HR workstation monitor",  CreatedAt=new DateOnly(2025,4,10) }
        );

        // ── LifecycleEvents ───────────────────────────────────────────────────
        db.LifecycleEvents.AddRange(
            new LifecycleEvent { AssetId=asset1.Id, OldStatus="Available",  NewStatus="Deployed",    Reason="Assigned to Max Owens — Litigation team",          ChangedByUserId=userAdmin.Id,    ChangedAt=new DateOnly(2025,1,20)  },
            new LifecycleEvent { AssetId=asset5.Id, OldStatus="Deployed",   NewStatus="Maintenance", Reason="Paper jam hardware fault — sent for repair",         ChangedByUserId=userDbernard.Id, ChangedAt=new DateOnly(2026,1,10)  },
            new LifecycleEvent { AssetId=asset7.Id, OldStatus="Deployed",   NewStatus="Retired",     Reason="End of support lifecycle — replaced with new unit",  ChangedByUserId=userAdmin.Id,    ChangedAt=new DateOnly(2025,12,1)  },
            new LifecycleEvent { AssetId=asset3.Id, OldStatus="Available",  NewStatus="Deployed",    Reason="Deployed to Fort Myers reception",                   ChangedByUserId=userDbernard.Id, ChangedAt=new DateOnly(2025,3,5)   }
        );

        // ── AuditLog ──────────────────────────────────────────────────────────
        db.AuditLogs.AddRange(
            new AuditLog { Ts=new DateTime(2025,1,15,10,0,0,DateTimeKind.Utc),  UserId=userAdmin.Id,    Username="admin",    Action="Created asset",             Target="HW-2026-001"                },
            new AuditLog { Ts=new DateTime(2025,1,20,9,30,0,DateTimeKind.Utc),  UserId=userAdmin.Id,    Username="admin",    Action="Assigned asset HW-2026-001", Target="Max Owens"                  },
            new AuditLog { Ts=new DateTime(2025,3,5,11,15,0,DateTimeKind.Utc),  UserId=userDbernard.Id, Username="dbernard", Action="Deployed asset",             Target="HW-2026-003"                },
            new AuditLog { Ts=new DateTime(2026,1,10,14,22,0,DateTimeKind.Utc), UserId=userDbernard.Id, Username="dbernard", Action="Updated lifecycle",          Target="HW-2026-005 to Maintenance" },
            new AuditLog { Ts=new DateTime(2025,12,1,16,45,0,DateTimeKind.Utc), UserId=userAdmin.Id,    Username="admin",    Action="Retired asset",              Target="HW-2026-007"                },
            new AuditLog { Ts=new DateTime(2025,4,10,8,10,0,DateTimeKind.Utc),  UserId=userAdmin.Id,    Username="admin",    Action="Assigned monitor",           Target="HW-2026-006 to Jane Smith"  },
            new AuditLog { Ts=new DateTime(2025,9,30,17,0,0,DateTimeKind.Utc),  UserId=userAdmin.Id,    Username="admin",    Action="Returned assignment",        Target="HW-2026-006"                }
        );

        await db.SaveChangesAsync();
    }
}
