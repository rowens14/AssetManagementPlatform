/*
 * FILE: AssignmentDtos.cs
 * PROJECT: AssetManagement.Shared / Models / DTOs / Assignments
 * PURPOSE: Data Transfer Objects for hardware and software assignments.
 *          HardwareAssignmentDto is the primary read DTO used throughout
 *          the client — it maps directly onto what AssignmentDto was in the
 *          in-memory build so the pages require no changes.
 *          AssignmentDto is kept as an alias for backward compatibility.
 */

using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Shared.Models;

// Primary read DTO for hardware chain-of-custody
public class HardwareAssignmentDto
{
    public int      Id                   { get; set; }
    public int      AssetId              { get; set; }
    public string   AssetCode            { get; set; } = "";
    public string   AssetModel           { get; set; } = "";
    public int      EmployeeId           { get; set; }
    public string   UserDisplayName      { get; set; } = "";  // employee full name
    public string   UserUsername         { get; set; } = "";  // linked AppUser username, if any
    public int      UserId               { get; set; }        // AppUser.Id for backward compat
    public DateOnly EffectiveDate        { get; set; }
    public DateOnly? EndDate             { get; set; }
    public string   CreatedByDisplayName { get; set; } = "";
    public DateOnly CreatedAt            { get; set; }
    public bool     IsActive             => EndDate == null;
}

// Alias kept so ClientApplicationState and pages compile without changes
public class AssignmentDto : HardwareAssignmentDto { }

public class CreateAssignmentRequest
{
    public int      AssetId       { get; set; }
    public int      UserId        { get; set; }  // maps to EmployeeId via AppUser.EmployeeId
    public DateOnly EffectiveDate { get; set; }
    public string   Notes         { get; set; } = "";
}

public class ReturnAssignmentRequest
{
    public DateOnly ReturnDate { get; set; }
}

// Software seat assignment
public class SoftwareAssignmentDto
{
    public int      Id           { get; set; }
    public int      AssetId      { get; set; }
    public string   ProductName  { get; set; } = "";
    public int      EmployeeId   { get; set; }
    public string   EmployeeName { get; set; } = "";
    public DateOnly AssignedDate { get; set; }
    public DateOnly? RevokedDate { get; set; }
    public bool     IsActive     => RevokedDate == null;
}
