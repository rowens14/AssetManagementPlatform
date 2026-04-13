/*
 * FILE: Asset.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: EF Core entity representing a physical or digital asset owned by the firm.
 *          This is the unified asset table — both hardware and software assets live here.
 *          Hardware-specific fields are in the linked HardwareDetail record.
 *          Software/license-specific fields are in the linked SoftwareDetail record.
 *          Exactly one of HardwareDetail or SoftwareDetail will be non-null for any
 *          given asset, determined by the AssetType field.
 *          Security fields (IsEncrypted, EncryptionProtocol, etc.) are on this entity
 *          but only exposed in API responses for Admin and Manager roles.
 */

namespace AssetManagement.Server.Data;

public class Asset
{
    public int     Id              { get; set; }
    public string  AssetCode       { get; set; } = "";
    public string  AssetTag        { get; set; } = "";
    public string  SerialNumber    { get; set; } = "";
    public string  AssetType       { get; set; } = "Laptop";
    public int?    VendorId        { get; set; }
    public string  Model           { get; set; } = "";
    public int     SiteId          { get; set; }
    public string  LifecycleStatus { get; set; } = "Available";
    public string  Description     { get; set; } = "";

    // Security fields — only returned to Admin / Manager in API responses
    public string  ComplianceId        { get; set; } = "";
    public bool    IsEncrypted         { get; set; }
    public string  EncryptionProtocol  { get; set; } = "";
    public bool    HasAntiVirus        { get; set; }
    public DateOnly? LastSecurityAudit { get; set; }

    public DateOnly CreatedAt { get; set; }

    // Navigation
    public Site            Site           { get; set; } = null!;
    public Vendor?         Vendor         { get; set; }
    public HardwareDetail? HardwareDetail { get; set; }
    public SoftwareDetail? SoftwareDetail { get; set; }

    public ICollection<HardwareAssignment> HardwareAssignments { get; set; } = [];
    public ICollection<LifecycleEvent>     LifecycleEvents     { get; set; } = [];
}
