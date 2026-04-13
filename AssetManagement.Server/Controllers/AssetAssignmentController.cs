/*
 * FILE: AssetAssignmentController.cs
 * PROJECT: AssetManagement.Server / Controllers
 * PURPOSE: REST API for hardware asset assignment. Requires JWT authentication.
 *          User identity is read from JWT claims via ApiControllerBase.CurrentUserId.
 */

using AssetManagement.Server.Data;
using AssetManagement.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Server.Controllers;

[Route("api/[controller]")]
public class AssignmentsController(AssetDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<List<AssignmentDto>> GetAll() =>
        await db.HardwareAssignments
            .Include(a => a.Asset)
            .Include(a => a.Employee)
            .Include(a => a.CreatedBy).ThenInclude(u => u.Employee)
            .OrderByDescending(a => a.EffectiveDate)
            .Select(a => MapAssignment(a))
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<AssignmentDto>> Create([FromBody] CreateAssignmentRequest req)
    {
        var asset   = await db.Assets.FindAsync(req.AssetId);
        var appUser = await db.AppUsers
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == req.UserId);

        if (asset == null)              return BadRequest("Asset not found.");
        if (appUser?.Employee == null)  return BadRequest("User has no linked employee record.");

        var assignment = new HardwareAssignment
        {
            AssetId         = req.AssetId,
            EmployeeId      = appUser.Employee.Id,
            CreatedByUserId = CurrentUserId,
            EffectiveDate   = req.EffectiveDate,
            EndDate         = null,
            Notes           = req.Notes,
            CreatedAt       = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.HardwareAssignments.Add(assignment);

        if (asset.LifecycleStatus != "Deployed")
        {
            var old = asset.LifecycleStatus;
            asset.LifecycleStatus = "Deployed";
            db.LifecycleEvents.Add(new LifecycleEvent
            {
                AssetId         = asset.Id,
                OldStatus       = old,
                NewStatus       = "Deployed",
                Reason          = $"Assigned to {appUser.Employee.FullName}",
                ChangedByUserId = CurrentUserId,
                ChangedAt       = DateOnly.FromDateTime(DateTime.UtcNow),
            });
        }

        db.AuditLogs.Add(new AuditLog
        {
            Ts       = DateTime.UtcNow,
            UserId   = CurrentUserId,
            Username = User.Identity?.Name ?? "",
            Action   = "Assigned asset",
            Target   = $"{asset.AssetCode} to {appUser.Employee.FullName}",
        });

        await db.SaveChangesAsync();
        await db.Entry(assignment).Reference(a => a.Asset).LoadAsync();
        await db.Entry(assignment).Reference(a => a.Employee).LoadAsync();
        await db.Entry(assignment).Reference(a => a.CreatedBy).LoadAsync();

        return CreatedAtAction(nameof(GetAll), MapAssignment(assignment));
    }

    [HttpPut("{id}/return")]
    public async Task<IActionResult> Return(int id, [FromBody] ReturnAssignmentRequest req)
    {
        var assignment = await db.HardwareAssignments
            .Include(a => a.Asset)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();

        assignment.EndDate = req.ReturnDate;

        db.AuditLogs.Add(new AuditLog
        {
            Ts       = DateTime.UtcNow,
            UserId   = CurrentUserId,
            Username = User.Identity?.Name ?? "",
            Action   = "Returned assignment",
            Target   = assignment.Asset?.AssetCode ?? id.ToString(),
        });

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var assignment = await db.HardwareAssignments.FindAsync(id);
        if (assignment == null) return NotFound();
        db.HardwareAssignments.Remove(assignment);
        await db.SaveChangesAsync();
        return NoContent();
    }

    internal static AssignmentDto MapAssignment(HardwareAssignment a) => new()
    {
        Id                   = a.Id,
        AssetId              = a.AssetId,
        AssetCode            = a.Asset?.AssetCode ?? "",
        AssetModel           = a.Asset?.Model ?? "",
        EmployeeId           = a.EmployeeId,
        UserId               = a.CreatedByUserId,
        UserDisplayName      = a.Employee?.FullName ?? "",
        UserUsername         = a.CreatedBy?.Username ?? "",
        EffectiveDate        = a.EffectiveDate,
        EndDate              = a.EndDate,
        CreatedByDisplayName = a.CreatedBy?.Employee?.FullName ?? a.CreatedBy?.Username ?? "",
        CreatedAt            = a.CreatedAt,
    };
}
