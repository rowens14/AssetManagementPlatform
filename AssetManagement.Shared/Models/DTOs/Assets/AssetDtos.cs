/*
 * FILE: AssetDtos.cs
 * PROJECT: AssetManagement.Shared / Models / DTOs / Assets
 * PURPOSE: Data Transfer Objects for hardware and software assets.
 *          AssetDto is the unified read DTO — controllers flatten Asset +
 *          HardwareDetail (or SoftwareDetail) into this single shape so
 *          the client never needs to handle separate detail records.
 *          Security fields (IsEncrypted, EncryptionProtocol, etc.) are
 *          populated by the server only when the requesting user is Admin
 *          or Manager. Viewers receive empty strings for these fields.
 *          SaveAssetRequest and SaveSoftwareAssetRequest are the write DTOs.
 */

using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Shared.Models;

// ── Read DTO — hardware and software assets unified ──────────────────────────
public class AssetDto
{
    public int    Id              { get; set; }
    public string AssetCode       { get; set; } = "";
    public string AssetTag        { get; set; } = "";
    public string SerialNumber    { get; set; } = "";
    public string AssetType       { get; set; } = "Laptop";
    public int?   VendorId        { get; set; }
    public string VendorName      { get; set; } = "";
    public string Model           { get; set; } = "";
    public int    SiteId          { get; set; }
    public string SiteName        { get; set; } = "";
    public string LifecycleStatus { get; set; } = "Available";
    public string Description     { get; set; } = "";
    public string Hostname        { get; set; } = "";   // flattened from HardwareDetail
    public string Manufacturer    { get; set; } = "";   // flattened from Vendor.Name
    public DateOnly CreatedAt     { get; set; }

    // LastAudit is the client-facing alias for LastSecurityAudit
    public DateOnly? LastAudit    { get; set; }

    // Security fields — only populated for Admin / Manager
    public string  ComplianceId       { get; set; } = "";
    public bool    IsEncrypted        { get; set; }
    public string  EncryptionProtocol { get; set; } = "";
    public bool    HasAntiVirus       { get; set; }
    public DateOnly? LastSecurityAudit { get; set; }

    // Hardware-specific (null for software assets)
    public HardwareDetailDto? HardwareDetail { get; set; }

    // Software-specific (null for hardware assets)
    public SoftwareDetailDto? SoftwareDetail { get; set; }
}

public class HardwareDetailDto
{
    public string  FormFactor      { get; set; } = "";
    public string  Specs           { get; set; } = "";
    public string  Os              { get; set; } = "";
    public string  IpAddress       { get; set; } = "";
    public string  MacAddress      { get; set; } = "";
    public string  Hostname        { get; set; } = "";
    public string  PurchaseOrderRef { get; set; } = "";
    public DateOnly? PurchaseDate  { get; set; }
    public DateOnly? WarrantyExpiry { get; set; }
}

public class SoftwareDetailDto
{
    public string  LicenseType   { get; set; } = "Per-Seat";
    public int     TotalSeats    { get; set; }
    public int     UsedSeats     { get; set; }
    public DateOnly? StartDate   { get; set; }
    public DateOnly? ExpiryDate  { get; set; }
    public DateOnly? RenewalDate { get; set; }
    public bool    AutoRenew     { get; set; }
    public bool    IsExpiringSoon { get; set; }
}

// ── Write DTOs ────────────────────────────────────────────────────────────────
public class SaveAssetRequest
{
    public int    Id              { get; set; }
    [Required] public string AssetCode    { get; set; } = "";
    [Required] public string AssetTag     { get; set; } = "";
    [Required] public string SerialNumber { get; set; } = "";
    [Required] public string AssetType    { get; set; } = "Laptop";
    public int?   VendorId        { get; set; }
    [Required] public string Model { get; set; } = "";
    public int    SiteId          { get; set; }
    public string LifecycleStatus { get; set; } = "Available";
    public string Description     { get; set; } = "";
    public string Manufacturer    { get; set; } = "";
    public DateOnly? LastAudit    { get; set; }   // maps to LastSecurityAudit on the entity

    // Security fields
    public string  ComplianceId       { get; set; } = "";
    public bool    IsEncrypted        { get; set; }
    public string  EncryptionProtocol { get; set; } = "";
    public bool    HasAntiVirus       { get; set; }
    public DateOnly? LastSecurityAudit { get; set; }

    // Hardware detail
    public string  FormFactor       { get; set; } = "";
    public string  Specs            { get; set; } = "";
    public string  Os               { get; set; } = "";
    public string  IpAddress        { get; set; } = "";
    public string  MacAddress       { get; set; } = "";
    public string  Hostname         { get; set; } = "";
    public string  PurchaseOrderRef { get; set; } = "";
    public DateOnly? PurchaseDate   { get; set; }
    public DateOnly? WarrantyExpiry { get; set; }
}

public class SaveSoftwareAssetRequest
{
    public int    Id           { get; set; }
    [Required] public string AssetCode { get; set; } = "";
    [Required] public string AssetTag  { get; set; } = "";
    [Required] public string Model     { get; set; } = "";
    public int?   VendorId     { get; set; }
    public int    SiteId       { get; set; }
    public string Description  { get; set; } = "";
    public string LicenseType  { get; set; } = "Per-Seat";
    [Range(0, int.MaxValue)] public int TotalSeats { get; set; }
    public DateOnly? StartDate  { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public DateOnly? RenewalDate { get; set; }
    public bool   AutoRenew    { get; set; }
    public string Status       { get; set; } = "Available";
}
