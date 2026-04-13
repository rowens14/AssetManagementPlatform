/*
 * FILE: EmployeeDtos.cs
 * PROJECT: AssetManagement.Shared / Models / DTOs / Employees
 * PURPOSE: Data Transfer Objects for employee and user account data.
 *          EmployeeDto represents a staff member (may not have a login).
 *          AppUserDto represents a login account (may not have an employee record).
 *          Login request and response types are also defined here.
 */

using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Shared.Models.DTOs.Employees
{
    public class EmployeeDto
    {
        public int    Id           { get; set; }
        public string FirstName    { get; set; } = "";
        public string LastName     { get; set; } = "";
        public string FullName     { get; set; } = "";
        public string Email        { get; set; } = "";
        public string JobTitle     { get; set; } = "";
        public int    SiteId       { get; set; }
        public string SiteName     { get; set; } = "";
        public int    DepartmentId { get; set; }
        public string DepartmentName { get; set; } = "";
        public bool   IsActive     { get; set; } = true;
    }

    public class AppUserDto
    {
        public int    Id          { get; set; }
        public string Username    { get; set; } = "";
        public string DisplayName { get; set; } = "";  // from linked Employee, or Username if no employee
        public string Role        { get; set; } = "Viewer";
        public int?   EmployeeId  { get; set; }
        public int    SiteId      { get; set; }
        public string SiteName    { get; set; } = "";
        public int    DeptId      { get; set; }
        public string DeptName    { get; set; } = "";
        public bool   IsActive    { get; set; } = true;

        // Derived — not stored, not sent from server
        public string Initials => string.Concat(
            DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Take(2).Select(w => w[0])).ToUpper();
    }

    public class LoginRequest
    {
        [Required] public string Username { get; set; } = "";
        [Required] public string Password { get; set; } = "";
    }

    public class LoginResponse
    {
        public bool        Success { get; set; }
        public string?     Error   { get; set; }
        public string?     Token   { get; set; }   // JWT Bearer token
        public AppUserDto? User    { get; set; }
    }

    public class CreateUserRequest
    {
        [Required] public string Username   { get; set; } = "";
        [Required] public string Password   { get; set; } = "";
        [Required] public string DisplayName { get; set; } = "";
        public string Role       { get; set; } = "Viewer";
        public int?   EmployeeId { get; set; }
    }
}
