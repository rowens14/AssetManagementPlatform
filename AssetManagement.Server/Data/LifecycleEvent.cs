/*
 * FILE: LifecycleEvent.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: EF Core entity recording a single status transition for an asset.
 *          Every time an asset's LifecycleStatus changes, a LifecycleEvent is
 *          appended recording the old status, new status, reason (required),
 *          who made the change, and when. Records are never edited or deleted.
 *          This provides a complete lifecycle audit trail from acquisition to disposal.
 */

namespace AssetManagement.Server.Data;

public class LifecycleEvent
{
    public int      Id           { get; set; }
    public int      AssetId      { get; set; }
    public string   OldStatus    { get; set; } = "";
    public string   NewStatus    { get; set; } = "";
    public string   Reason       { get; set; } = "";   // required — cannot be empty
    public int      ChangedByUserId { get; set; }
    public DateOnly ChangedAt    { get; set; }

    public Asset   Asset     { get; set; } = null!;
    public AppUser ChangedBy { get; set; } = null!;
}
