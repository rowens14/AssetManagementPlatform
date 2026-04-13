/*
 * FILE: AuditLog.cs
 * PROJECT: AssetManagement.Server / Data
 * PURPOSE: EF Core entity for a single append-only audit log entry.
 *          Records every significant action taken by any user in the system.
 *          The AssetDbContext enforces append-only at the SaveChanges level —
 *          any attempt to update or delete an AuditLog entry throws an
 *          InvalidOperationException, even for Admin users.
 *          UserId is nullable to support system-generated entries.
 */

namespace AssetManagement.Server.Data;

public class AuditLog
{
    public int      Id        { get; set; }
    public DateTime Ts        { get; set; }
    public int?     UserId    { get; set; }    // nullable — system entries have no user
    public string   Username  { get; set; } = "";
    public string   Action    { get; set; } = "";
    public string   Target    { get; set; } = "";

    public AppUser? User { get; set; }
}
