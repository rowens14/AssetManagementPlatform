/*
 * FILE: ClientApplicationState.cs
 * PROJECT: AssetManagement.Client / Services
 * PURPOSE: Singleton client-side state manager for the Blazor WASM application.
 *          Acts as the single source of truth for all data cached in the browser:
 *          current user, assets, assignments, lifecycle events, licenses, audit log,
 *          sites, departments, and users. Orchestrates calls to ServerApiClient and
 *          notifies components to re-render via the OnChange event. Also manages:
 *          - Login / logout flow
 *          - Page navigation state
 *          - Toast notification queue
 *          - Role-based permission checks (CanEdit, IsAdmin)
 *          Components inject this service and subscribe to OnChange to stay in sync.
 */

using AssetManagement.Shared.Models;
using AssetManagement.Shared.Models.DTOs.Audit;
using AssetManagement.Shared.Models.DTOs.Dashboard;
using AssetManagement.Shared.Models.DTOs.Employees;

namespace AssetManagement.Client.Services
{
    public class ClientApplicationState(ServerApiClient api)
    {
        public AppUserDto? CurrentUser { get; private set; }
        public string CurrentPage { get; private set; } = "hardware";
        public AssetDto? DetailAsset { get; private set; }

        public void SetDetailAsset(int id)
        {
            DetailAsset = Assets.FirstOrDefault(a => a.Id == id);
            Notify();
        }
        public bool IsLoading { get; private set; }

        // Cached data
        public List<SiteDto>           Sites           { get; private set; } = [];
        public List<DepartmentDto>     Departments     { get; private set; } = [];
        public List<AppUserDto>        Users           { get; private set; } = [];
        public List<AssetDto>          Assets          { get; private set; } = [];
        public List<AssignmentDto>     Assignments     { get; private set; } = [];
        public List<LifecycleEventDto> LifecycleEvents { get; private set; } = [];
        public List<LicenseDto>        Licenses        { get; private set; } = [];
        public List<AuditEntryDto>     AuditLog        { get; private set; } = [];
        public List<ToastMessage>      Toasts          { get; private set; } = [];

        public bool CanEdit => CurrentUser?.Role is "Admin" or "Manager";
        public bool IsAdmin => CurrentUser?.Role == "Admin";

        public SiteDto?       GetSite(int id)  => Sites.FirstOrDefault(s => s.Id == id);
        public DepartmentDto? GetDept(int id)  => Departments.FirstOrDefault(d => d.Id == id);
        public AppUserDto?    GetUser(int id)  => Users.FirstOrDefault(u => u.Id == id);
        public AssetDto?      GetAsset(int id) => Assets.FirstOrDefault(a => a.Id == id);

        public event Action? OnChange;
        private void Notify() => OnChange?.Invoke();

        // ── Auth ──────────────────────────────────────────────────────────────────
        public async Task<string?> LoginAsync(string username, string password)
        {
            var resp = await api.LoginAsync(new LoginRequest { Username = username, Password = password });
            if (resp == null || !resp.Success) return resp?.Error ?? "Login failed.";
            CurrentUser = resp.User;
            // Store JWT and attach it to all future HTTP requests
            if (!string.IsNullOrEmpty(resp.Token))
                api.SetToken(resp.Token);
            await LoadAllAsync();
            return null;
        }

        public void Logout()
        {
            CurrentUser = null;
            CurrentPage = "hardware";
            api.ClearToken();
            Assets.Clear(); Users.Clear(); Assignments.Clear();
            LifecycleEvents.Clear(); Licenses.Clear(); AuditLog.Clear();
            Notify();
        }

        public void Navigate(string page)
        {
            CurrentPage = page;
            Notify();
        }

        // ── Load ──────────────────────────────────────────────────────────────────
        public async Task LoadAllAsync()
        {
            IsLoading = true; Notify();
            var tasks = new Task[]
            {
                LoadSitesAsync(), LoadDepartmentsAsync(), LoadUsersAsync(),
                LoadAssetsAsync(), LoadAssignmentsAsync(), LoadLifecycleAsync(),
                LoadLicensesAsync(), LoadAuditAsync()
            };
            await Task.WhenAll(tasks);
            IsLoading = false; Notify();
        }

        public async Task LoadSitesAsync()       { Sites           = await api.GetSitesAsync()       ?? []; Notify(); }
        public async Task LoadDepartmentsAsync() { Departments     = await api.GetDepartmentsAsync() ?? []; Notify(); }
        public async Task LoadUsersAsync()       { Users           = await api.GetUsersAsync()       ?? []; Notify(); }
        public async Task LoadAssetsAsync()      { Assets          = await api.GetAssetsAsync()      ?? []; Notify(); }
        public async Task LoadAssignmentsAsync() { Assignments     = await api.GetAssignmentsAsync() ?? []; Notify(); }
        public async Task LoadLifecycleAsync()   { LifecycleEvents = await api.GetLifecycleEventsAsync() ?? []; Notify(); }
        public async Task LoadLicensesAsync()    { Licenses        = await api.GetLicensesAsync()    ?? []; Notify(); }
        public async Task LoadAuditAsync()       { AuditLog        = await api.GetAuditAsync()       ?? []; Notify(); }

        // ── Assets ────────────────────────────────────────────────────────────────
        public async Task<string?> SaveAssetAsync(SaveAssetRequest req, bool isNew)
        {
            if (isNew)
            {
                var (data, err) = await api.CreateAssetAsync(req);
                if (err != null) return err;
                Assets.Insert(0, data!);
                await api.AddAuditAsync(CurrentUser!.Username, "Created asset", req.AssetCode);
                await LoadAuditAsync();
            }
            else
            {
                var (data, err) = await api.UpdateAssetAsync(req.Id, req);
                if (err != null) return err;
                var idx = Assets.FindIndex(a => a.Id == req.Id);
                if (idx >= 0) Assets[idx] = data!;
                await api.AddAuditAsync(CurrentUser!.Username, "Updated asset", req.AssetCode);
                await LoadAuditAsync();
            }
            ShowToast(isNew ? "Asset created." : "Asset updated.");
            Notify(); return null;
        }

        public async Task<bool> DeleteAssetAsync(int id)
        {
            var asset = GetAsset(id);
            if (!await api.DeleteAssetAsync(id)) return false;
            Assets.RemoveAll(a => a.Id == id);
            Assignments.RemoveAll(a => a.AssetId == id);
            await api.AddAuditAsync(CurrentUser!.Username, "Deleted asset", asset?.AssetCode ?? id.ToString());
            await LoadAuditAsync();
            ShowToast("Asset deleted."); Notify(); return true;
        }

        // ── Assignments ───────────────────────────────────────────────────────────
        public async Task<string?> CreateAssignmentAsync(CreateAssignmentRequest req)
        {
            var (data, err) = await api.CreateAssignmentAsync(req);
            if (err != null) return err;
            Assignments.Insert(0, data!);
            // Refresh assets since status may have changed
            await LoadAssetsAsync();
            await api.AddAuditAsync(CurrentUser!.Username, $"Assigned asset", $"{data!.AssetCode} to {data.UserDisplayName}");
            await LoadAuditAsync();
            ShowToast("Assignment created."); Notify(); return null;
        }

        public async Task ReturnAssignmentAsync(int id, DateOnly returnDate)
        {
            var a = Assignments.FirstOrDefault(x => x.Id == id);
            var assetCode = a?.AssetCode ?? id.ToString();
            await api.ReturnAssignmentAsync(id, new ReturnAssignmentRequest { ReturnDate = returnDate });
            await LoadAssignmentsAsync();
            await api.AddAuditAsync(CurrentUser!.Username, "Returned assignment", assetCode);
            await LoadAuditAsync();
            ShowToast("Assignment returned."); Notify();
        }

        public async Task DeleteAssignmentAsync(int id)
        {
            var a = Assignments.FirstOrDefault(x => x.Id == id);
            await api.DeleteAssignmentAsync(id);
            Assignments.RemoveAll(x => x.Id == id);
            await api.AddAuditAsync(CurrentUser!.Username, "Deleted assignment", a?.AssetCode ?? id.ToString());
            await LoadAuditAsync();
            ShowToast("Assignment deleted."); Notify();
        }

        // ── Lifecycle Events ──────────────────────────────────────────────────────
        public async Task<string?> CreateLifecycleEventAsync(CreateLifecycleEventRequest req)
        {
            var (data, err) = await api.CreateLifecycleEventAsync(req);
            if (err != null) return err;
            LifecycleEvents.Insert(0, data!);
            await LoadAssetsAsync(); // asset status changed
            await api.AddAuditAsync(CurrentUser!.Username, $"Changed lifecycle for {data!.AssetCode}", $"{data.OldStatus} → {data.NewStatus}");
            await LoadAuditAsync();
            ShowToast("Lifecycle event recorded."); Notify(); return null;
        }

        // ── Licenses ─────────────────────────────────────────────────────────────
        public async Task<string?> SaveLicenseAsync(SaveLicenseRequest req, bool isNew)
        {
            if (isNew)
            {
                var (data, err) = await api.CreateLicenseAsync(req);
                if (err != null) return err;
                Licenses.Insert(0, data!);
                await api.AddAuditAsync(CurrentUser!.Username, "Created license", req.Product);
            }
            else
            {
                var (data, err) = await api.UpdateLicenseAsync(req.Id, req);
                if (err != null) return err;
                var idx = Licenses.FindIndex(l => l.Id == req.Id);
                if (idx >= 0) Licenses[idx] = data!;
                await api.AddAuditAsync(CurrentUser!.Username, "Updated license", req.Product);
            }
            await LoadAuditAsync();
            ShowToast(isNew ? "License created." : "License updated."); Notify(); return null;
        }

        public async Task DeleteLicenseAsync(int id)
        {
            var l = Licenses.FirstOrDefault(x => x.Id == id);
            await api.DeleteLicenseAsync(id);
            Licenses.RemoveAll(x => x.Id == id);
            await api.AddAuditAsync(CurrentUser!.Username, "Deleted license", l?.Product ?? id.ToString());
            await LoadAuditAsync();
            ShowToast("License deleted."); Notify();
        }

        // ── Users ─────────────────────────────────────────────────────────────────
        public async Task<string?> CreateUserAsync(CreateUserRequest req)
        {
            var (data, err) = await api.CreateUserAsync(req);
            if (err != null) return err;
            Users.Add(data!);
            await api.AddAuditAsync(CurrentUser!.Username, "Created user", req.Username);
            await LoadAuditAsync();
            ShowToast("User created."); Notify(); return null;
        }

        public async Task ToggleUserAsync(int id)
        {
            if (id == CurrentUser?.Id) { ShowToast("Can't deactivate yourself.", "error"); return; }
            await api.ToggleUserAsync(id);
            await LoadUsersAsync();
            var u = GetUser(id);
            await api.AddAuditAsync(CurrentUser!.Username, u?.IsActive == true ? "Activated user" : "Deactivated user", u?.Username ?? "");
            await LoadAuditAsync();
            ShowToast($"User {(u?.IsActive == true ? "activated" : "deactivated")}."); Notify();
        }

        // ── Toast ─────────────────────────────────────────────────────────────────
        public void ShowToast(string text, string type = "")
        {
            var t = new ToastMessage { Text = text, Type = type };
            Toasts.Add(t);
            Notify();
            Task.Delay(3000).ContinueWith(_ => { Toasts.Remove(t); Notify(); });
        }
    }

    public class ToastMessage
    {
        public string Text { get; set; } = "";
        public string Type { get; set; } = "";
    }
}
