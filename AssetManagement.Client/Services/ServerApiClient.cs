/*
 * FILE: ServerApiClient.cs
 * PROJECT: AssetManagement.Client / Services
 * PURPOSE: Typed HTTP client wrapping all server API endpoints. Replaced the old
 *          X-User-Id header pattern with JWT Bearer token authentication.
 *          After login, SetToken() stores the token and every subsequent request
 *          automatically includes it in the Authorization header.
 */

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AssetManagement.Shared.Models;
using AssetManagement.Shared.Models.DTOs.Audit;
using AssetManagement.Shared.Models.DTOs.Dashboard;
using AssetManagement.Shared.Models.DTOs.Employees;

namespace AssetManagement.Client.Services;

public class ServerApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public void SetToken(string token)
    {
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearToken()
    {
        http.DefaultRequestHeaders.Authorization = null;
    }

    // ── Auth ──────────────────────────────────────────────────────────────────
    public async Task<LoginResponse?> LoginAsync(LoginRequest req)
    {
        var res = await http.PostAsJsonAsync("api/auth/login", req, JsonOpts);
        return await res.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
    }

    // ── Reference data ────────────────────────────────────────────────────────
    public Task<List<SiteDto>?>       GetSitesAsync()       => http.GetFromJsonAsync<List<SiteDto>>("api/sites", JsonOpts);
    public Task<List<DepartmentDto>?> GetDepartmentsAsync() => http.GetFromJsonAsync<List<DepartmentDto>>("api/departments", JsonOpts);

    // ── Assets ────────────────────────────────────────────────────────────────
    public Task<List<AssetDto>?> GetAssetsAsync() => http.GetFromJsonAsync<List<AssetDto>>("api/assets", JsonOpts);

    public async Task<(AssetDto? data, string? error)> CreateAssetAsync(SaveAssetRequest req)
    {
        var res = await http.PostAsJsonAsync("api/assets", req, JsonOpts);
        if (res.IsSuccessStatusCode) return (await res.Content.ReadFromJsonAsync<AssetDto>(JsonOpts), null);
        return (null, await res.Content.ReadAsStringAsync());
    }

    public async Task<(AssetDto? data, string? error)> UpdateAssetAsync(int id, SaveAssetRequest req)
    {
        var res = await http.PutAsJsonAsync($"api/assets/{id}", req, JsonOpts);
        if (res.IsSuccessStatusCode) return (await res.Content.ReadFromJsonAsync<AssetDto>(JsonOpts), null);
        return (null, await res.Content.ReadAsStringAsync());
    }

    public async Task<bool> DeleteAssetAsync(int id)
    {
        var res = await http.DeleteAsync($"api/assets/{id}");
        return res.IsSuccessStatusCode;
    }

    // ── Assignments ───────────────────────────────────────────────────────────
    public Task<List<AssignmentDto>?> GetAssignmentsAsync() =>
        http.GetFromJsonAsync<List<AssignmentDto>>("api/assignments", JsonOpts);

    public async Task<(AssignmentDto? data, string? error)> CreateAssignmentAsync(CreateAssignmentRequest req)
    {
        var res = await http.PostAsJsonAsync("api/assignments", req, JsonOpts);
        if (res.IsSuccessStatusCode) return (await res.Content.ReadFromJsonAsync<AssignmentDto>(JsonOpts), null);
        return (null, await res.Content.ReadAsStringAsync());
    }

    public async Task<bool> ReturnAssignmentAsync(int id, ReturnAssignmentRequest req)
    {
        var res = await http.PutAsJsonAsync($"api/assignments/{id}/return", req, JsonOpts);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAssignmentAsync(int id)
    {
        var res = await http.DeleteAsync($"api/assignments/{id}");
        return res.IsSuccessStatusCode;
    }

    // ── Lifecycle Events ──────────────────────────────────────────────────────
    public Task<List<LifecycleEventDto>?> GetLifecycleEventsAsync() =>
        http.GetFromJsonAsync<List<LifecycleEventDto>>("api/lifecycleevents", JsonOpts);

    public async Task<(LifecycleEventDto? data, string? error)> CreateLifecycleEventAsync(CreateLifecycleEventRequest req)
    {
        var res = await http.PostAsJsonAsync("api/lifecycleevents", req, JsonOpts);
        if (res.IsSuccessStatusCode) return (await res.Content.ReadFromJsonAsync<LifecycleEventDto>(JsonOpts), null);
        return (null, await res.Content.ReadAsStringAsync());
    }

    // ── Licenses ─────────────────────────────────────────────────────────────
    public Task<List<LicenseDto>?> GetLicensesAsync() =>
        http.GetFromJsonAsync<List<LicenseDto>>("api/licenses", JsonOpts);

    public async Task<(LicenseDto? data, string? error)> CreateLicenseAsync(SaveLicenseRequest req)
    {
        var res = await http.PostAsJsonAsync("api/licenses", req, JsonOpts);
        if (res.IsSuccessStatusCode) return (await res.Content.ReadFromJsonAsync<LicenseDto>(JsonOpts), null);
        return (null, await res.Content.ReadAsStringAsync());
    }

    public async Task<(LicenseDto? data, string? error)> UpdateLicenseAsync(int id, SaveLicenseRequest req)
    {
        var res = await http.PutAsJsonAsync($"api/licenses/{id}", req, JsonOpts);
        if (res.IsSuccessStatusCode) return (await res.Content.ReadFromJsonAsync<LicenseDto>(JsonOpts), null);
        return (null, await res.Content.ReadAsStringAsync());
    }

    public async Task<bool> DeleteLicenseAsync(int id)
    {
        var res = await http.DeleteAsync($"api/licenses/{id}");
        return res.IsSuccessStatusCode;
    }

    // ── Users ─────────────────────────────────────────────────────────────────
    public Task<List<AppUserDto>?> GetUsersAsync() =>
        http.GetFromJsonAsync<List<AppUserDto>>("api/users", JsonOpts);

    public async Task<(AppUserDto? data, string? error)> CreateUserAsync(CreateUserRequest req)
    {
        var res = await http.PostAsJsonAsync("api/users", req, JsonOpts);
        if (res.IsSuccessStatusCode) return (await res.Content.ReadFromJsonAsync<AppUserDto>(JsonOpts), null);
        return (null, await res.Content.ReadAsStringAsync());
    }

    public async Task<bool> ToggleUserAsync(int id)
    {
        var res = await http.PutAsync($"api/users/{id}/toggle", null);
        return res.IsSuccessStatusCode;
    }

    // ── Audit ─────────────────────────────────────────────────────────────────
    public Task<List<AuditEntryDto>?> GetAuditAsync() =>
        http.GetFromJsonAsync<List<AuditEntryDto>>("api/audit", JsonOpts);

    public Task AddAuditAsync(string username, string action, string target) =>
        http.PostAsJsonAsync("api/audit",
            new AuditEntryDto { Username = username, Action = action, Target = target }, JsonOpts);
}
