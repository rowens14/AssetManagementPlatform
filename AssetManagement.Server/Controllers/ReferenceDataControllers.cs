/*
 * FILE: ReferenceDataControllers.cs
 * PROJECT: AssetManagement.Server / Controllers
 * PURPOSE: Six controllers for reference and supporting data. All require JWT auth
 *          via ApiControllerBase. User identity is read from JWT claims.
 */

using AssetManagement.Server.Data;
using AssetManagement.Shared.Models;
using AssetManagement.Shared.Models.DTOs.Audit;
using AssetManagement.Shared.Models.DTOs.Dashboard;
using AssetManagement.Shared.Models.DTOs.Employees;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Server.Controllers;

// ── LIFECYCLE EVENTS ──────────────────────────────────────────────────────────
[Route("api/[controller]")]
public class LifecycleEventsController(AssetDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<List<LifecycleEventDto>> GetAll() =>
        await db.LifecycleEvents
            .Include(e => e.Asset)
            .Include(e => e.ChangedBy).ThenInclude(u => u.Employee)
            .OrderByDescending(e => e.ChangedAt)
            .Select(e => MapEvent(e))
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<LifecycleEventDto>> Create([FromBody] CreateLifecycleEventRequest req)
    {
        var asset = await db.Assets.FindAsync(req.AssetId);
        if (asset == null) return NotFound("Asset not found.");
        if (string.IsNullOrWhiteSpace(req.Reason)) return BadRequest("Reason is required.");

        var old = asset.LifecycleStatus;
        asset.LifecycleStatus = req.NewStatus;

        var ev = new LifecycleEvent
        {
            AssetId         = req.AssetId,
            OldStatus       = old,
            NewStatus       = req.NewStatus,
            Reason          = req.Reason,
            ChangedByUserId = CurrentUserId,
            ChangedAt       = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.LifecycleEvents.Add(ev);

        db.AuditLogs.Add(new AuditLog
        {
            Ts       = DateTime.UtcNow,
            UserId   = CurrentUserId,
            Username = User.Identity?.Name ?? "",
            Action   = $"Changed lifecycle for {asset.AssetCode}",
            Target   = $"{old} → {req.NewStatus}",
        });

        await db.SaveChangesAsync();
        await db.Entry(ev).Reference(e => e.Asset).LoadAsync();
        await db.Entry(ev).Reference(e => e.ChangedBy).LoadAsync();
        if (ev.ChangedBy != null)
            await db.Entry(ev.ChangedBy).Reference(u => u.Employee).LoadAsync();

        return CreatedAtAction(nameof(GetAll), MapEvent(ev));
    }

    internal static LifecycleEventDto MapEvent(LifecycleEvent e) => new()
    {
        Id                   = e.Id,
        AssetId              = e.AssetId,
        AssetCode            = e.Asset?.AssetCode ?? "",
        OldStatus            = e.OldStatus,
        NewStatus            = e.NewStatus,
        Reason               = e.Reason,
        ChangedByDisplayName = e.ChangedBy?.Employee?.FullName ?? e.ChangedBy?.Username ?? "",
        ChangedAt            = e.ChangedAt,
    };
}

// ── LICENSES ─────────────────────────────────────────────────────────────────
[Route("api/[controller]")]
public class LicensesController(AssetDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<List<LicenseDto>> GetAll() =>
        await db.Assets
            .Where(a => a.AssetType == "Software")
            .Include(a => a.Site)
            .Include(a => a.Vendor)
            .Include(a => a.SoftwareDetail)
            .OrderByDescending(a => a.Id)
            .Select(a => MapLicense(a))
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<LicenseDto>> Create([FromBody] SaveLicenseRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Product) || req.Total < 0)
            return BadRequest("Product name and valid quantity required.");

        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Name == req.Vendor);
        if (vendor == null && !string.IsNullOrWhiteSpace(req.Vendor))
        {
            vendor = new Vendor { Name = req.Vendor, Type = "Software" };
            db.Vendors.Add(vendor);
            await db.SaveChangesAsync();
        }

        var asset = new Asset
        {
            AssetCode       = $"SW-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            AssetTag        = $"SW-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            SerialNumber    = $"SW-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            AssetType       = "Software",
            Model           = req.Product,
            SiteId          = req.SiteId,
            VendorId        = vendor?.Id,
            LifecycleStatus = "Available",
            CreatedAt       = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.Assets.Add(asset);
        await db.SaveChangesAsync();

        db.SoftwareDetails.Add(new SoftwareDetail
        {
            AssetId     = asset.Id,
            LicenseType = req.Type,
            TotalSeats  = req.Total,
            StartDate   = req.Start,
            ExpiryDate  = req.Expiry,
        });
        await db.SaveChangesAsync();

        await db.Entry(asset).Reference(a => a.Site).LoadAsync();
        await db.Entry(asset).Reference(a => a.Vendor).LoadAsync();
        await db.Entry(asset).Reference(a => a.SoftwareDetail).LoadAsync();
        return CreatedAtAction(nameof(GetAll), MapLicense(asset));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<LicenseDto>> Update(int id, [FromBody] SaveLicenseRequest req)
    {
        var asset = await db.Assets
            .Include(a => a.Site).Include(a => a.Vendor).Include(a => a.SoftwareDetail)
            .FirstOrDefaultAsync(a => a.Id == id && a.AssetType == "Software");
        if (asset == null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.Product) || req.Total < 0)
            return BadRequest("Product name and valid quantity required.");

        asset.Model  = req.Product;
        asset.SiteId = req.SiteId;
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Name == req.Vendor);
        asset.VendorId = vendor?.Id;

        if (asset.SoftwareDetail != null)
        {
            asset.SoftwareDetail.LicenseType = req.Type;
            asset.SoftwareDetail.TotalSeats  = req.Total;
            asset.SoftwareDetail.StartDate   = req.Start;
            asset.SoftwareDetail.ExpiryDate  = req.Expiry;
        }
        await db.SaveChangesAsync();
        await db.Entry(asset).Reference(a => a.Site).LoadAsync();
        await db.Entry(asset).Reference(a => a.Vendor).LoadAsync();
        return Ok(MapLicense(asset));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == id && a.AssetType == "Software");
        if (asset == null) return NotFound();
        db.Assets.Remove(asset);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static LicenseDto MapLicense(Asset a) => new()
    {
        Id       = a.Id,
        Product  = a.Model,
        Vendor   = a.Vendor?.Name ?? "",
        SiteId   = a.SiteId,
        SiteName = a.Site?.Name ?? "",
        Type     = a.SoftwareDetail?.LicenseType ?? "Per-Seat",
        Total    = a.SoftwareDetail?.TotalSeats ?? 0,
        Start    = a.SoftwareDetail?.StartDate,
        Expiry   = a.SoftwareDetail?.ExpiryDate,
        Status   = a.LifecycleStatus == "Available" ? "Active" : a.LifecycleStatus,
    };
}

// ── USERS ─────────────────────────────────────────────────────────────────────
[Route("api/[controller]")]
public class UsersController(AssetDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<List<AppUserDto>> GetAll() =>
        await db.AppUsers
            .Include(u => u.Employee).ThenInclude(e => e!.Site)
            .Include(u => u.Employee).ThenInclude(e => e!.Department)
            .Select(u => AuthController.MapUser(u))
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<AppUserDto>> Create([FromBody] CreateUserRequest req)
    {
        if (await db.AppUsers.AnyAsync(u => u.Username == req.Username))
            return Conflict("Username already taken.");

        var user = new AppUser
        {
            Username     = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role         = req.Role,
            EmployeeId   = req.EmployeeId,
            IsActive     = true,
        };
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        await db.Entry(user).Reference(u => u.Employee).LoadAsync();
        if (user.Employee != null)
        {
            await db.Entry(user.Employee).Reference(e => e.Site).LoadAsync();
            await db.Entry(user.Employee).Reference(e => e.Department).LoadAsync();
        }
        return CreatedAtAction(nameof(GetAll), AuthController.MapUser(user));
    }

    [HttpPut("{id}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        if (id == CurrentUserId) return BadRequest("Cannot deactivate yourself.");
        var user = await db.AppUsers.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync();
        return NoContent();
    }
}

// ── SITES ─────────────────────────────────────────────────────────────────────
[Route("api/[controller]")]
public class SitesController(AssetDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<List<SiteDto>> GetAll() =>
        await db.Sites.Select(s => new SiteDto { Id = s.Id, Code = s.Code, Name = s.Name }).ToListAsync();
}

// ── DEPARTMENTS ───────────────────────────────────────────────────────────────
[Route("api/[controller]")]
public class DepartmentsController(AssetDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<List<DepartmentDto>> GetAll() =>
        await db.Departments
            .Include(d => d.Site)
            .Select(d => new DepartmentDto { Id = d.Id, SiteId = d.SiteId, SiteName = d.Site.Name, Name = d.Name })
            .ToListAsync();
}

// ── AUDIT ─────────────────────────────────────────────────────────────────────
[Route("api/[controller]")]
public class AuditController(AssetDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<List<AuditEntryDto>> GetAll() =>
        await db.AuditLogs
            .OrderByDescending(e => e.Ts)
            .Select(e => new AuditEntryDto { Id = e.Id, Ts = e.Ts, Username = e.Username, Action = e.Action, Target = e.Target })
            .ToListAsync();

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AuditEntryDto entry)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Ts       = DateTime.UtcNow,
            UserId   = CurrentUserId,
            Username = User.Identity?.Name ?? entry.Username,
            Action   = entry.Action,
            Target   = entry.Target,
        });
        await db.SaveChangesAsync();
        return NoContent();
    }
}
