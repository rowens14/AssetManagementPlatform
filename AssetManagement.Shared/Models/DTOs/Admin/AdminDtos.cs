/*
 * FILE: AdminDtos.cs
 * PROJECT: AssetManagement.Shared / Models / DTOs / Admin
 * PURPOSE: Data Transfer Objects for administrative reference data:
 *          sites, departments, and vendors. These are read-only lookup
 *          values loaded once after login and cached in ClientApplicationState.
 */

namespace AssetManagement.Shared.Models;

public class SiteDto
{
    public int    Id   { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class DepartmentDto
{
    public int    Id       { get; set; }
    public int    SiteId   { get; set; }
    public string SiteName { get; set; } = "";
    public string Name     { get; set; } = "";
}

public class VendorDto
{
    public int    Id   { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";   // Hardware | Software | Both
}
