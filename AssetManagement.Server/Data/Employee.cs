/*
 * FILE: Employee.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: EF Core entity representing a staff member at the firm.
 *          Employees are distinct from AppUsers (login accounts) — not every
 *          employee needs a system login, and not every login account is tied
 *          to an employee record. The link is AppUser.EmployeeId (nullable).
 *          Employees appear in hardware assignment records as the person
 *          who holds an asset.
 */

namespace AssetManagement.Server.Data;

public class Employee
{
    public int    Id          { get; set; }
    public string FirstName   { get; set; } = "";
    public string LastName    { get; set; } = "";
    public string Email       { get; set; } = "";
    public string JobTitle    { get; set; } = "";
    public int    SiteId      { get; set; }
    public int    DepartmentId { get; set; }
    public bool   IsActive    { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";

    public Site       Site       { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public AppUser?   AppUser    { get; set; }  // nullable — not all employees have logins

    public ICollection<HardwareAssignment>  HardwareAssignments  { get; set; } = [];
    public ICollection<SoftwareAssignment>  SoftwareAssignments  { get; set; } = [];
}
