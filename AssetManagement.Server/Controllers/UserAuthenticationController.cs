/*
 * FILE: UserAuthenticationController.cs
 * PROJECT: AssetManagement.Server / Controllers
 * PURPOSE: Handles user authentication. POST /api/auth/login verifies bcrypt
 *          credentials, issues a signed JWT token, and returns it with the user
 *          profile. The token must be included as a Bearer token on all subsequent
 *          requests. POST /api/auth/refresh is not yet implemented — clients
 *          should re-login when the token expires.
 */

using AssetManagement.Server.Data;
using AssetManagement.Server.Services;
using AssetManagement.Shared.Models.DTOs.Employees;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AssetDbContext db, JwtService jwt) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await db.AppUsers
            .Include(u => u.Employee)
                .ThenInclude(e => e!.Site)
            .Include(u => u.Employee)
                .ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(u => u.Username == req.Username && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Ok(new LoginResponse { Success = false, Error = "Invalid credentials." });

        var token = jwt.GenerateToken(user);

        return Ok(new LoginResponse
        {
            Success = true,
            Token   = token,
            User    = MapUser(user)
        });
    }

    internal static AppUserDto MapUser(AppUser u) => new()
    {
        Id          = u.Id,
        Username    = u.Username,
        DisplayName = u.Employee?.FullName ?? u.Username,
        Role        = u.Role,
        EmployeeId  = u.EmployeeId,
        SiteId      = u.Employee?.SiteId ?? 0,
        SiteName    = u.Employee?.Site?.Name ?? "",
        DeptId      = u.Employee?.DepartmentId ?? 0,
        DeptName    = u.Employee?.Department?.Name ?? "",
        IsActive    = u.IsActive,
    };
}
