/*
 * FILE: Department.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: EF Core entity representing an organisational department within a site.
 *          Seeded departments: Litigation, HR, Finance, Partners, Paralegals, IT.
 *          Employees and AppUsers are linked to a department for organisational context.
 */

namespace AssetManagement.Server.Data;

public class Department
{
    public int    Id     { get; set; }
    public int    SiteId { get; set; }
    public string Name   { get; set; } = "";

    public Site                 Site      { get; set; } = null!;
    public ICollection<Employee> Employees { get; set; } = [];
}
