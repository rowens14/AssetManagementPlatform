/*
 * FILE: ApiControllerBase.cs
 * PROJECT: AssetManagement.Server / Controllers
 * PURPOSE: Base class for all API controllers. Provides a helper to extract
 *          the authenticated user's ID from the JWT claims, replacing the old
 *          X-User-Id header pattern. All controllers that need the current
 *          user ID inherit from this class and call CurrentUserId.
 */

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Server.Controllers;

[ApiController]
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId =>
        int.TryParse(User.FindFirstValue("userId"), out var id) ? id : 0;

    protected string CurrentUserRole =>
        User.FindFirstValue(ClaimTypes.Role) ?? "Viewer";
}
