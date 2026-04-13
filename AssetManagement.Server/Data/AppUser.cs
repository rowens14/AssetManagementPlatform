/*
 * FILE: AppUser.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: EF Core entity for a system login account.
 *          Holds bcrypt-hashed credentials and the RBAC role (Admin/Manager/Viewer).
 *          EmployeeId is nullable — a user account may or may not correspond to
 *          an employee record (e.g. a shared IT admin account has no employee record).
 *          Roles control what the user can see and do in the Blazor client.
 */

namespace AssetManagement.Server.Data;

public class AppUser
{
    public int    Id           { get; set; }
    public string Username     { get; set; } = "";
    public string PasswordHash { get; set; } = "";   // bcrypt hash — never plain text
    public string Role         { get; set; } = "Viewer";  // Admin | Manager | Viewer
    public int?   EmployeeId   { get; set; }              // nullable
    public bool   IsActive     { get; set; } = true;

    public Employee?             Employee             { get; set; }
    public ICollection<AuditLog> AuditLogs            { get; set; } = [];
    public ICollection<HardwareAssignment> CreatedHardwareAssignments { get; set; } = [];
    public ICollection<LifecycleEvent>     ChangedLifecycleEvents     { get; set; } = [];
}
