/*
 * FILE: SoftwareDetail.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: EF Core entity holding software license-specific fields for an Asset record.
 *          One-to-one with Asset (AssetId is the PK and FK).
 *          Contains seat counts, license type, expiry, renewal info, and license key.
 *          Populated only for software assets.
 *          The IsExpiringSoon computed property (30-day warning) is used by the
 *          DashboardController to surface expiring licenses.
 */

namespace AssetManagement.Server.Data;

public class SoftwareDetail
{
    public int    AssetId        { get; set; }    // PK and FK to Asset
    public string LicenseType    { get; set; } = "Per-Seat";  // Per-Seat|Volume|OEM|Subscription|Enterprise
    public int    TotalSeats     { get; set; }
    public int    UsedSeats      { get; set; }
    public DateOnly? StartDate   { get; set; }
    public DateOnly? ExpiryDate  { get; set; }
    public DateOnly? RenewalDate { get; set; }
    public string LicenseKey     { get; set; } = "";   // stored encrypted in production
    public bool   AutoRenew      { get; set; }

    public bool IsExpiringSoon =>
        ExpiryDate.HasValue &&
        ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) < DateTime.UtcNow.AddDays(30) &&
        ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) > DateTime.UtcNow;

    public Asset Asset { get; set; } = null!;
    public ICollection<SoftwareAssignment> SoftwareAssignments { get; set; } = [];
}
