/*
 * FILE: AssetInventoryController.cs
 * PROJECT: AssetManagement.Server / Controllers
 * PURPOSE: REST API for hardware asset inventory. All endpoints require a valid
 *          JWT token (inherited [Authorize] from ApiControllerBase). The current
 *          user's ID and role are read from JWT claims — no X-User-Id header.
 *          Security fields are only populated for Admin and Manager roles.
 */

using AssetManagement.Server.Data;
using AssetManagement.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Server.Controllers;

[Route("api/[controller]")]
public class AssetsController(AssetDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<List<AssetDto>> GetAll()
    {
        var canSee = CurrentUserRole is "Admin" or "Manager";
        return await db.Assets
            .Where(a => a.AssetType != "Software")
            .Include(a => a.Site)
            .Include(a => a.Vendor)
            .Include(a => a.HardwareDetail)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => MapAsset(a, canSee))
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<AssetDto>> Create([FromBody] SaveAssetRequest req)
    {
        if (await db.Assets.AnyAsync(a =>
                a.AssetCode    == req.AssetCode    ||
                a.AssetTag     == req.AssetTag     ||
                a.SerialNumber == req.SerialNumber))
            return Conflict("Asset code, tag, or serial already in use.");

        var asset = new Asset
        {
            AssetCode          = req.AssetCode,
            AssetTag           = req.AssetTag,
            SerialNumber       = req.SerialNumber,
            AssetType          = req.AssetType,
            VendorId           = req.VendorId,
            Model              = req.Model,
            SiteId             = req.SiteId,
            LifecycleStatus    = req.LifecycleStatus,
            Description        = req.Description,
            ComplianceId       = req.ComplianceId,
            IsEncrypted        = req.IsEncrypted,
            EncryptionProtocol = req.EncryptionProtocol,
            HasAntiVirus       = req.HasAntiVirus,
            LastSecurityAudit  = req.LastAudit ?? req.LastSecurityAudit,
            CreatedAt          = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.Assets.Add(asset);
        await db.SaveChangesAsync();

        var detail = new HardwareDetail
        {
            AssetId          = asset.Id,
            FormFactor       = req.FormFactor,
            Specs            = req.Specs,
            Os               = req.Os,
            IpAddress        = req.IpAddress,
            MacAddress       = req.MacAddress,
            Hostname         = req.Hostname,
            PurchaseOrderRef = req.PurchaseOrderRef,
            PurchaseDate     = req.PurchaseDate,
            WarrantyExpiry   = req.WarrantyExpiry,
        };
        db.HardwareDetails.Add(detail);

        db.AuditLogs.Add(new AuditLog
        {
            Ts       = DateTime.UtcNow,
            UserId   = CurrentUserId,
            Username = User.Identity?.Name ?? "",
            Action   = "Created asset",
            Target   = req.AssetCode,
        });

        await db.SaveChangesAsync();

        await db.Entry(asset).Reference(a => a.Site).LoadAsync();
        await db.Entry(asset).Reference(a => a.Vendor).LoadAsync();
        asset.HardwareDetail = detail;

        var canSee = CurrentUserRole is "Admin" or "Manager";
        return CreatedAtAction(nameof(GetAll), MapAsset(asset, canSee));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AssetDto>> Update(int id, [FromBody] SaveAssetRequest req)
    {
        var asset = await db.Assets
            .Include(a => a.Site)
            .Include(a => a.Vendor)
            .Include(a => a.HardwareDetail)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (asset == null) return NotFound();

        if (await db.Assets.AnyAsync(a => a.Id != id && (
                a.AssetCode    == req.AssetCode    ||
                a.AssetTag     == req.AssetTag     ||
                a.SerialNumber == req.SerialNumber)))
            return Conflict("Asset code, tag, or serial already in use.");

        asset.AssetCode          = req.AssetCode;
        asset.AssetTag           = req.AssetTag;
        asset.SerialNumber       = req.SerialNumber;
        asset.AssetType          = req.AssetType;
        asset.VendorId           = req.VendorId;
        asset.Model              = req.Model;
        asset.SiteId             = req.SiteId;
        asset.LifecycleStatus    = req.LifecycleStatus;
        asset.Description        = req.Description;
        asset.ComplianceId       = req.ComplianceId;
        asset.IsEncrypted        = req.IsEncrypted;
        asset.EncryptionProtocol = req.EncryptionProtocol;
        asset.HasAntiVirus       = req.HasAntiVirus;
        asset.LastSecurityAudit  = req.LastAudit ?? req.LastSecurityAudit;

        if (asset.HardwareDetail != null)
        {
            asset.HardwareDetail.FormFactor       = req.FormFactor;
            asset.HardwareDetail.Specs            = req.Specs;
            asset.HardwareDetail.Os               = req.Os;
            asset.HardwareDetail.IpAddress        = req.IpAddress;
            asset.HardwareDetail.MacAddress       = req.MacAddress;
            asset.HardwareDetail.Hostname         = req.Hostname;
            asset.HardwareDetail.PurchaseOrderRef = req.PurchaseOrderRef;
            asset.HardwareDetail.PurchaseDate     = req.PurchaseDate;
            asset.HardwareDetail.WarrantyExpiry   = req.WarrantyExpiry;
        }

        db.AuditLogs.Add(new AuditLog
        {
            Ts       = DateTime.UtcNow,
            UserId   = CurrentUserId,
            Username = User.Identity?.Name ?? "",
            Action   = "Updated asset",
            Target   = req.AssetCode,
        });

        await db.SaveChangesAsync();
        await db.Entry(asset).Reference(a => a.Site).LoadAsync();
        await db.Entry(asset).Reference(a => a.Vendor).LoadAsync();

        var canSee = CurrentUserRole is "Admin" or "Manager";
        return Ok(MapAsset(asset, canSee));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (CurrentUserRole != "Admin") return Forbid();
        var asset = await db.Assets.FindAsync(id);
        if (asset == null) return NotFound();
        db.Assets.Remove(asset);
        db.AuditLogs.Add(new AuditLog
        {
            Ts       = DateTime.UtcNow,
            UserId   = CurrentUserId,
            Username = User.Identity?.Name ?? "",
            Action   = "Deleted asset",
            Target   = asset.AssetCode,
        });
        await db.SaveChangesAsync();
        return NoContent();
    }

    internal static AssetDto MapAsset(Asset a, bool includeSecurityFields = false) => new()
    {
        Id              = a.Id,
        AssetCode       = a.AssetCode,
        AssetTag        = a.AssetTag,
        SerialNumber    = a.SerialNumber,
        AssetType       = a.AssetType,
        VendorId        = a.VendorId,
        VendorName      = a.Vendor?.Name ?? "",
        Model           = a.Model,
        SiteId          = a.SiteId,
        SiteName        = a.Site?.Name ?? "",
        LifecycleStatus = a.LifecycleStatus,
        Description     = a.Description,
        Hostname        = a.HardwareDetail?.Hostname ?? "",
        Manufacturer    = a.Vendor?.Name ?? "",
        CreatedAt       = a.CreatedAt,
        LastAudit       = includeSecurityFields ? a.LastSecurityAudit : null,
        ComplianceId       = includeSecurityFields ? a.ComplianceId       : "",
        IsEncrypted        = includeSecurityFields && a.IsEncrypted,
        EncryptionProtocol = includeSecurityFields ? a.EncryptionProtocol : "",
        HasAntiVirus       = includeSecurityFields && a.HasAntiVirus,
        LastSecurityAudit  = includeSecurityFields ? a.LastSecurityAudit  : null,
        HardwareDetail = a.HardwareDetail == null ? null : new HardwareDetailDto
        {
            FormFactor       = a.HardwareDetail.FormFactor,
            Specs            = a.HardwareDetail.Specs,
            Os               = a.HardwareDetail.Os,
            IpAddress        = a.HardwareDetail.IpAddress,
            MacAddress       = a.HardwareDetail.MacAddress,
            Hostname         = a.HardwareDetail.Hostname,
            PurchaseOrderRef = a.HardwareDetail.PurchaseOrderRef,
            PurchaseDate     = a.HardwareDetail.PurchaseDate,
            WarrantyExpiry   = a.HardwareDetail.WarrantyExpiry,
        }
    };
}
