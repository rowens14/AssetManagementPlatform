/*
 * FILE: Site.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: EF Core entity representing a physical office location.
 *          The firm has two sites: Naples (NAP) and Fort Myers (FTM).
 *          Sites are the top-level organisational unit — departments,
 *          employees, assets, and licenses are all scoped to a site.
 */

namespace AssetManagement.Server.Data;

public class Site
{
    public int    Id   { get; set; }
    public string Code { get; set; } = "";   // NAP | FTM
    public string Name { get; set; } = "";

    public ICollection<Department>      Departments { get; set; } = [];
    public ICollection<Employee>        Employees   { get; set; } = [];
    public ICollection<Asset>           Assets      { get; set; } = [];
}
