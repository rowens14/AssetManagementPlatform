/*
 * FILE: HardwareAssignment.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: EF Core entity recording the chain of custody for a hardware asset.
 *          Every time a hardware asset is assigned to an employee, a new record
 *          is created with an EffectiveDate. When the asset is returned, EndDate
 *          is set. Records are never deleted — the full custody history is preserved.
 *          A partial unique index (WHERE end_date IS NULL) enforces that only one
 *          active assignment per asset can exist at any time.
 */

namespace AssetManagement.Server.Data;

public class HardwareAssignment
{
    public int      Id            { get; set; }
    public int      AssetId       { get; set; }
    public int      EmployeeId    { get; set; }
    public int      CreatedByUserId { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? EndDate      { get; set; }
    public string   Notes         { get; set; } = "";
    public DateOnly CreatedAt     { get; set; }

    public bool IsActive => EndDate == null;

    public Asset    Asset      { get; set; } = null!;
    public Employee Employee   { get; set; } = null!;
    public AppUser  CreatedBy  { get; set; } = null!;
}
