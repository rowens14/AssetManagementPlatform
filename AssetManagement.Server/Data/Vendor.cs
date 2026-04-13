/*
 * FILE: Vendor.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: EF Core entity representing a hardware or software vendor.
 *          Vendors are referenced by Asset records so that manufacturer
 *          and software publisher information is normalised rather than
 *          stored as free-text strings on every asset.
 *          Examples: Dell, Apple, Microsoft, Adobe, Cisco, Canon.
 */

namespace AssetManagement.Server.Data;

public class Vendor
{
    public int    Id   { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Hardware";  // Hardware | Software | Both

    public ICollection<Asset> Assets { get; set; } = [];
}
