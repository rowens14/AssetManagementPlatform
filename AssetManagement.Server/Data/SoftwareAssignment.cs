/*
 * FILE: SoftwareAssignment.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: EF Core entity recording per-seat software license assignments.
 *          Links a SoftwareDetail (the license) to an Employee (the seat holder).
 *          Multiple active assignments can exist for the same software asset —
 *          one per allocated seat. UsedSeats on SoftwareDetail is derived from
 *          the count of active SoftwareAssignment records.
 */

namespace AssetManagement.Server.Data;

public class SoftwareAssignment
{
    public int      Id             { get; set; }
    public int      AssetId        { get; set; }   // FK to Asset (which has SoftwareDetail)
    public int      EmployeeId     { get; set; }
    public DateOnly AssignedDate   { get; set; }
    public DateOnly? RevokedDate   { get; set; }
    public bool     IsActive       => RevokedDate == null;

    public Asset    Asset    { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
