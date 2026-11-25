using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using MTSS.UI.Services;

namespace MTSS.UI.Controllers;

public class SocietyAdminModulesController : Controller
{
    private readonly ApiService _api;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record Module(string Title, string Endpoint, string[] Fields, string[] Labels, string? StatusEndpoint = null, string? ReportEndpoint = null);
    private static readonly Dictionary<string, Module> Modules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["wings"] = new("Wings", "api/societyadmin/wings", new[] { "name" }, new[] { "Wing name" }, "api/societyadmin/wings/{id}/status"),
        ["floors"] = new("Floors", "api/societyadmin/floors", new[] { "wingId", "name", "floorNumber" }, new[] { "Wing ID", "Floor name", "Floor number" }, "api/societyadmin/floors/{id}/status"),
        ["flats"] = new("Flats", "api/societyadmin/flats", new[] { "floorId", "flatNumber", "isOccupied" }, new[] { "Floor ID", "Flat number", "Occupied" }, "api/societyadmin/flats/{id}/status"),
        ["residents"] = new("Residents", "api/societyadmin/residents", new[] { "flatId", "fullName", "mobileNumber" }, new[] { "Flat ID", "Full name", "Mobile number" }, "api/societyadmin/residents/{id}/status", "api/societyadmin/reports/residents"),
        ["amenities"] = new("Amenities", "api/societyadmin/amenities", new[] { "name", "description", "openingTime", "closingTime" }, new[] { "Amenity name", "Description", "Opening time (HH:mm:ss)", "Closing time (HH:mm:ss)" }, "api/societyadmin/amenities/{id}/status"),
        ["guests"] = new("Guest entries", "api/societyadmin/guest-entries", new[] { "flatId", "residentProfileId", "guestName", "guestPhone", "purpose" }, new[] { "Flat ID", "Resident profile ID", "Guest name", "Guest phone", "Purpose" }, "api/societyadmin/guest-entries/{id}/status", "api/societyadmin/reports/guest-entries"),
        ["parcels"] = new("Parcels", "api/societyadmin/parcels", new[] { "flatId", "residentProfileId", "courierName", "trackingNumber" }, new[] { "Flat ID", "Resident profile ID", "Courier", "Tracking number" }, "api/societyadmin/parcels/{id}/status", "api/societyadmin/reports/parcels"),
        ["complaints"] = new("Complaints", "api/societyadmin/complaints", Array.Empty<string>(), Array.Empty<string>(), "api/societyadmin/complaints/{id}/status", "api/societyadmin/reports/complaints"),
        ["notifications"] = new("Notifications", "api/societyadmin/notifications", new[] { "title", "message", "type" }, new[] { "Title", "Message", "Type" }),
        ["amenity-logs"] = new("Amenity access logs", "api/societyadmin/amenity-access-logs", Array.Empty<string>(), Array.Empty<string>(), null, "api/societyadmin/reports/amenity-access"),
        ["amenity-pins"] = new("Amenity PIN records", "api/societyadmin/amenity-pins", Array.Empty<string>(), Array.Empty<string>())
    };

    public SocietyAdminModulesController(ApiService api) => _api = api;

    private bool SignedIn() => !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")) &&
        HttpContext.Session.GetString("Roles")?.Split(',').Any(x => x.Equals("SocietyAdmin", StringComparison.OrdinalIgnoreCase)) == true;

    private IActionResult LoginRedirect() => RedirectToAction("Login", "Auth");

    // Resolve each module to the folder structure used in Views/SocietyAdmin.
    private static string ModuleViewPath(string module, string viewName)
    {
        var folder = module.ToLowerInvariant() switch
        {
            "wings" => "Wings",
            "floors" => "Floors",
            "flats" => "Flats",
            "residents" => "Residents",
            "amenities" => "Amenities",
            "guests" => "GuestEntries",
            "parcels" => "Parcels",
            "complaints" => "Complaints",
            "notifications" => "Notifications",
            "amenity-logs" => "AmenityAccessLogs",
            "amenity-pins" => "AmenityPins",
            _ => throw new ArgumentException("Unknown SocietyAdmin module.", nameof(module))
        };

        return $"~/Views/SocietyAdmin/{folder}/{viewName}.cshtml";
    }

    private async Task<(List<JsonElement> Rows, string? Error)> ReadRows(string endpoint, CancellationToken ct)
    {
        using var response = await _api.GetAsync(endpoint, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return (new(), "Your session has expired. Please log in again.");
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) return (new(), ApiMessage(body, $"Request failed (HTTP {(int)response.StatusCode})."));
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("data", out var data)) return (new(), "The backend response did not include data.");
        if (data.ValueKind == JsonValueKind.Array) return (data.EnumerateArray().Select(x => x.Clone()).ToList(), null);
        if (data.ValueKind == JsonValueKind.Object) return (new() { data.Clone() }, null);
        return (new(), null);
    }

    private static string ApiMessage(string body, string fallback)
    {
        try { using var d = JsonDocument.Parse(body); if (d.RootElement.TryGetProperty("message", out var m)) return m.GetString() ?? fallback; }
        catch { }
        return fallback;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        try
        {
            using var response = await _api.GetAsync("api/societyadmin/dashboard", ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode) ViewBag.ErrorMessage = ApiMessage(body, $"Dashboard request failed (HTTP {(int)response.StatusCode}).");
            else { using var doc = JsonDocument.Parse(body); if (doc.RootElement.TryGetProperty("data", out var data)) ViewBag.DashboardData = data.Clone(); }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException) { ViewBag.ErrorMessage = "Unable to load dashboard data from the backend."; }
        ViewBag.FullName = HttpContext.Session.GetString("FullName") ?? "Society Admin";
        return View("~/Views/SocietyAdmin/Dashboard/Index.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Index(string module, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (!Modules.TryGetValue(module ?? "", out var config)) return NotFound();
        try
        {
            var (rows, error) = await ReadRows(config.Endpoint, ct);
            ViewBag.Module = module; ViewBag.Title = config.Title; ViewBag.Rows = rows; ViewBag.ErrorMessage = error;
            ViewBag.Fields = config.Fields; ViewBag.Labels = config.Labels;
            return View(ModuleViewPath(module, "Index"));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        { ViewBag.Module = module; ViewBag.Title = config.Title; ViewBag.Rows = new List<JsonElement>(); ViewBag.Fields = config.Fields; ViewBag.Labels = config.Labels; ViewBag.ErrorMessage = "Unable to connect to the backend."; return View(ModuleViewPath(module, "Index")); }
    }

    [HttpGet]
    public IActionResult Create(string module)
    {
        if (!SignedIn()) return LoginRedirect();
        if (!Modules.TryGetValue(module ?? "", out var config) || config.Fields.Length == 0) return NotFound();
        ViewBag.Module = module; ViewBag.Title = config.Title; ViewBag.Fields = config.Fields; ViewBag.Labels = config.Labels; ViewBag.Values = new Dictionary<string,string>();
        return View(ModuleViewPath(module, "Form"));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string module, int id, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (!Modules.TryGetValue(module ?? "", out var config) || config.Fields.Length == 0 || id <= 0) return NotFound();
        try
        {
            using var response = await _api.GetAsync($"{config.Endpoint}/{id}", ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode) { TempData["Error"] = ApiMessage(body, "Unable to load record for editing."); return RedirectToAction(nameof(Index), new { module }); }
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("data", out var data)) return RedirectToAction(nameof(Index), new { module });
            if (data.ValueKind == JsonValueKind.Array) data = data.EnumerateArray().FirstOrDefault();
            var values = new Dictionary<string,string>();
            foreach (var f in config.Fields) if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty(f, out var v)) values[f] = v.ToString();
            ViewBag.Module = module; ViewBag.Title = config.Title; ViewBag.Fields = config.Fields; ViewBag.Labels = config.Labels; ViewBag.Values = values; ViewBag.Id = id;
            return View(ModuleViewPath(module, "Form"));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException) { TempData["Error"] = "Unable to load record."; return RedirectToAction(nameof(Index), new { module }); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string module, int id, Dictionary<string,string> fields, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (!Modules.TryGetValue(module ?? "", out var config) || id <= 0) return NotFound();
        Dictionary<string, object?> payload;
        try { payload = BuildPayload(config, fields); }
        catch (ArgumentException ex) { ViewBag.ErrorMessage = ex.Message; ViewBag.Module = module; ViewBag.Title = config.Title; ViewBag.Fields = config.Fields; ViewBag.Labels = config.Labels; ViewBag.Values = fields; ViewBag.Id = id; return View(ModuleViewPath(module, "Form")); }
        try
        {
            using var response = await _api.PutAsync($"{config.Endpoint}/{id}", payload, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode) { ViewBag.ErrorMessage = ApiMessage(body, $"Update failed (HTTP {(int)response.StatusCode})."); ViewBag.Module = module; ViewBag.Title = config.Title; ViewBag.Fields = config.Fields; ViewBag.Labels = config.Labels; ViewBag.Values = fields; ViewBag.Id = id; return View(ModuleViewPath(module, "Form")); }
            TempData["Success"] = "Record updated successfully.";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { TempData["Error"] = "Unable to connect to the backend."; }
        return RedirectToAction(nameof(Index), new { module });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string module, Dictionary<string,string> fields, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (!Modules.TryGetValue(module ?? "", out var config) || config.Fields.Length == 0) return NotFound();
        Dictionary<string, object?> payload;
        try { payload = BuildPayload(config, fields); }
        catch (ArgumentException ex) { ViewBag.ErrorMessage = ex.Message; ViewBag.Module = module; ViewBag.Title = config.Title; ViewBag.Fields = config.Fields; ViewBag.Labels = config.Labels; ViewBag.Values = fields; return View(ModuleViewPath(module, "Form")); }
        try
        {
            using var response = await _api.PostAsync(config.Endpoint, payload, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode) { ViewBag.ErrorMessage = ApiMessage(body, $"Create failed (HTTP {(int)response.StatusCode})."); ViewBag.Module = module; ViewBag.Title = config.Title; ViewBag.Fields = config.Fields; ViewBag.Labels = config.Labels; ViewBag.Values = fields; return View(ModuleViewPath(module, "Form")); }
            TempData["Success"] = $"{config.Title} record created successfully.";
            return RedirectToAction(nameof(Index), new { module });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { ViewBag.ErrorMessage = "Unable to connect to the backend."; ViewBag.Module = module; ViewBag.Title = config.Title; ViewBag.Fields = config.Fields; ViewBag.Labels = config.Labels; ViewBag.Values = fields; return View(ModuleViewPath(module, "Form")); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string module, int id, bool isActive, string? status, string? adminRemarks, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (!Modules.TryGetValue(module ?? "", out var config) || string.IsNullOrWhiteSpace(config.StatusEndpoint) || id <= 0) return BadRequest();
        var endpoint = config.StatusEndpoint.Replace("{id}", id.ToString());
        object payload = module.Equals("complaints", StringComparison.OrdinalIgnoreCase)
            ? new { status = status ?? "InProgress", adminRemarks = adminRemarks ?? "" }
            : module.Equals("guests", StringComparison.OrdinalIgnoreCase) || module.Equals("parcels", StringComparison.OrdinalIgnoreCase)
                ? new { status = status ?? "Approved" } : new { isActive };
        try
        {
            using var response = await _api.PatchAsync(endpoint, payload, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Status updated successfully." : ApiMessage(body, $"Status update failed (HTTP {(int)response.StatusCode}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { TempData["Error"] = "Unable to connect to the backend."; }
        return RedirectToAction(nameof(Index), new { module });
    }

    [HttpGet]
    public IActionResult ValidateAmenityPin()
    {
        if (!SignedIn()) return LoginRedirect();
        return View("~/Views/SocietyAdmin/ValidateAmenityPin/Index.cshtml");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateAmenityPin(int amenityId, string pin, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (amenityId <= 0 || string.IsNullOrWhiteSpace(pin)) { ViewBag.ErrorMessage = "Enter a valid amenity ID and PIN."; return View("~/Views/SocietyAdmin/ValidateAmenityPin/Index.cshtml"); }
        try
        {
            using var response = await _api.PostAsync("api/societyadmin/amenity-pins/validate", new { amenityId, pin }, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode) ViewBag.SuccessMessage = ApiMessage(body, "Amenity PIN validation completed successfully.");
            else ViewBag.ErrorMessage = ApiMessage(body, $"PIN validation failed (HTTP {(int)response.StatusCode}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { ViewBag.ErrorMessage = "Unable to connect to the backend."; }
        return View("~/Views/SocietyAdmin/ValidateAmenityPin/Index.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Report(string module, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (!Modules.TryGetValue(module ?? "", out var config) || string.IsNullOrWhiteSpace(config.ReportEndpoint)) return NotFound();
        try { var (rows, error) = await ReadRows(config.ReportEndpoint, ct); ViewBag.Rows = rows; ViewBag.ErrorMessage = error; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException) { ViewBag.Rows = new List<JsonElement>(); ViewBag.ErrorMessage = "Unable to load report."; }
        ViewBag.Module = module; ViewBag.Title = config.Title + " report";
        return View(ModuleViewPath(module, "Report"));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateResidentAccount(int id, string email, string password, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (id <= 0 || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) { TempData["Error"] = "Resident ID, email and password are required."; return RedirectToAction(nameof(Index), new { module = "residents" }); }
        try
        {
            using var response = await _api.PostAsync($"api/societyadmin/residents/{id}/create-account", new { email = email.Trim(), password }, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Resident login account created." : ApiMessage(body, $"Account creation failed (HTTP {(int)response.StatusCode}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { TempData["Error"] = "Unable to connect to the backend."; }
        return RedirectToAction(nameof(Index), new { module = "residents" });
    }

    private static Dictionary<string, object?> BuildPayload(Module config, Dictionary<string,string> fields)
    {
        var result = new Dictionary<string, object?>();
        for (var i = 0; i < config.Fields.Length; i++)
        {
            var key = config.Fields[i]; fields.TryGetValue(key, out var value); value ??= "";
            if (key.EndsWith("Id", StringComparison.OrdinalIgnoreCase) || key == "floorNumber")
            { if (!int.TryParse(value, out var number) || number <= 0) throw new ArgumentException($"{key} must be a positive number."); result[key] = number; }
            else if (key == "isOccupied") result[key] = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            else result[key] = value.Trim();
        }
        return result;
    }
}
