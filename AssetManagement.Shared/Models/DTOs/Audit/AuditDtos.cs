/*
 * FILE: AuditDtos.cs
 * PROJECT: AssetManagement.Shared / Models / DTOs / Audit
 * PURPOSE: Data Transfer Objects for the audit log and lifecycle events.
 *          AuditEntryDto maps directly to AuditLog entries and is used
 *          throughout the client for the audit log page and inline displays.
 *          LifecycleEventDto maps to LifecycleEvent records and is used in
 *          the asset detail lifecycle history tab.
 */

using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Shared.Models.DTOs.Audit;

public class AuditEntryDto
{
    public int      Id       { get; set; }
    public DateTime Ts       { get; set; }
    public string   Username { get; set; } = "";
    public string   Action   { get; set; } = "";
    public string   Target   { get; set; } = "";
}

public class LifecycleEventDto
{
    public int      Id                   { get; set; }
    public int      AssetId              { get; set; }
    public string   AssetCode            { get; set; } = "";
    public string   OldStatus            { get; set; } = "";
    public string   NewStatus            { get; set; } = "";
    public string   Reason               { get; set; } = "";
    public string   ChangedByDisplayName { get; set; } = "";
    public DateOnly ChangedAt            { get; set; }
}

public class CreateLifecycleEventRequest
{
    public int     AssetId   { get; set; }
    [Required] public string NewStatus { get; set; } = "";
    [Required] public string Reason    { get; set; } = "";
}
