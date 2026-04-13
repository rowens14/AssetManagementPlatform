/*
 * FILE: DashboardDtos.cs
 * PROJECT: AssetManagement.Shared / Models / DTOs / Dashboard
 * PURPOSE: Data Transfer Objects for dashboard summary and analytics data.
 *          These DTOs aggregate data across multiple tables for display on
 *          the dashboard and reports pages.
 *          ExpiringLicenseDto uses the 30-day warning threshold per requirements.
 */

namespace AssetManagement.Shared.Models.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalAssets      { get; set; }
    public int DeployedAssets   { get; set; }
    public int AvailableAssets  { get; set; }
    public int MaintenanceAssets { get; set; }
    public int RetiredAssets    { get; set; }
    public int TotalLicenses    { get; set; }
    public int ExpiringLicenses { get; set; }  // within 30 days
    public int TotalEmployees   { get; set; }
}

public class SeatUsageDto
{
    public int    AssetId      { get; set; }
    public string ProductName  { get; set; } = "";
    public string VendorName   { get; set; } = "";
    public string SiteName     { get; set; } = "";
    public int    TotalSeats   { get; set; }
    public int    UsedSeats    { get; set; }
    public int    FreeSeats    => TotalSeats - UsedSeats;
    public double UsagePct     => TotalSeats > 0 ? Math.Round((double)UsedSeats / TotalSeats * 100, 1) : 0;
}

public class ExpiringLicenseDto
{
    public int      AssetId     { get; set; }
    public string   ProductName { get; set; } = "";
    public string   SiteName    { get; set; } = "";
    public DateOnly ExpiryDate  { get; set; }
    public int      DaysRemaining { get; set; }
}

// Kept as LicenseDto alias so SoftwareLicensesPage.razor compiles without changes
public class LicenseDto
{
    public int    Id       { get; set; }
    public string Product  { get; set; } = "";
    public string Vendor   { get; set; } = "";
    public int    SiteId   { get; set; }
    public string SiteName { get; set; } = "";
    public string Type     { get; set; } = "Per-Seat";
    public int    Total    { get; set; }
    public DateOnly? Start  { get; set; }
    public DateOnly? Expiry { get; set; }
    public string Status   { get; set; } = "Active";
    public bool   IsExpiringSoon => Expiry.HasValue && Status == "Active" &&
        Expiry.Value.ToDateTime(TimeOnly.MinValue) < DateTime.UtcNow.AddDays(30);
}

public class SaveLicenseRequest
{
    public int    Id      { get; set; }
    public string Product { get; set; } = "";
    public string Vendor  { get; set; } = "";
    public int    SiteId  { get; set; }
    public string Type    { get; set; } = "Per-Seat";
    public int    Total   { get; set; }
    public DateOnly? Start  { get; set; }
    public DateOnly? Expiry { get; set; }
    public string Status  { get; set; } = "Active";
}
