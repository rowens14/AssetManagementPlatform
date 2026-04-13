/*
 * FILE: HardwareDetail.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: EF Core entity holding hardware-specific fields for an Asset record.
 *          One-to-one with Asset (AssetId is the PK and FK).
 *          Contains form factor, technical specifications, operating system,
 *          network identifiers, and hostname. Populated only for hardware assets
 *          (Laptop, Desktop, Server, Switch, Printer, Monitor, Tablet, Phone).
 */

namespace AssetManagement.Server.Data;

public class HardwareDetail
{
    public int    AssetId    { get; set; }    // PK and FK to Asset
    public string FormFactor { get; set; } = "";   // Desktop | Laptop | Tower | Rack | etc.
    public string Specs      { get; set; } = "";   // Free-text: RAM, CPU, Storage
    public string Os         { get; set; } = "";   // Windows 11, macOS 14, etc.
    public string IpAddress  { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string Hostname   { get; set; } = "";
    public string PurchaseOrderRef { get; set; } = "";
    public DateOnly? PurchaseDate  { get; set; }
    public DateOnly? WarrantyExpiry { get; set; }

    public Asset Asset { get; set; } = null!;
}
