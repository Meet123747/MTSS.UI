using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using MTSS.UI.Services;

namespace MTSS.UI.Controllers;

public class SocietyAdminModulesController : Controller
{
    private readonly ApiService _api;

    private sealed record Module(
        string Title,
        string Endpoint,
        string[] CreateFields,
        string[] CreateLabels,
        string[]? EditFields = null,
        string[]? EditLabels = null,
        string? StatusEndpoint = null,
        string? ReportEndpoint = null,
        bool AllowEdit = true)
    {
        public string[] EffectiveEditFields => EditFields ?? CreateFields;
        public string[] EffectiveEditLabels => EditLabels ?? CreateLabels;
    }

    private static readonly Dictionary<string, Module> Modules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["wings"] = new(
            "Wings",
            "api/societyadmin/wings",
            new[] { "name" },
            new[] { "Wing name" },
            StatusEndpoint: "api/societyadmin/wings/{id}/status"),
        ["floors"] = new(
            "Floors",
            "api/societyadmin/floors",
            new[] { "wingId", "name", "floorNumber" },
            new[] { "Wing", "Floor name", "Floor number" },
            new[] { "name", "floorNumber" },
            new[] { "Floor name", "Floor number" },
            StatusEndpoint: "api/societyadmin/floors/{id}/status"),
        ["flats"] = new(
            "Flats",
            "api/societyadmin/flats",
            new[] { "floorId", "flatNumber" },
            new[] { "Floor", "Flat number" },
            new[] { "flatNumber", "isOccupied" },
            new[] { "Flat number", "Occupied" },
            StatusEndpoint: "api/societyadmin/flats/{id}/status"),
        ["residents"] = new(
            "Residents",
            "api/societyadmin/residents",
            new[] { "flatId", "fullName", "mobileNumber" },
            new[] { "Flat", "Full name", "Mobile number" },
            new[] { "fullName", "mobileNumber", "isHome" },
            new[] { "Full name", "Mobile number", "Currently at home" },
            StatusEndpoint: "api/societyadmin/residents/{id}/status",
            ReportEndpoint: "api/societyadmin/reports/residents"),
        ["amenities"] = new(
            "Amenities",
            "api/societyadmin/amenities",
            new[] { "name", "description", "openingTime", "closingTime" },
            new[] { "Amenity name", "Description", "Opening time", "Closing time" },
            StatusEndpoint: "api/societyadmin/amenities/{id}/status"),
        ["guests"] = new(
            "Guest entries",
            "api/societyadmin/guest-entries",
            new[] { "flatId", "residentProfileId", "guestName", "guestPhone", "purpose" },
            new[] { "Flat", "Resident", "Guest name", "Guest phone", "Purpose" },
            StatusEndpoint: "api/societyadmin/guest-entries/{id}/status",
            ReportEndpoint: "api/societyadmin/reports/guest-entries",
            AllowEdit: false),
        ["parcels"] = new(
            "Parcels",
            "api/societyadmin/parcels",
            new[] { "flatId", "residentProfileId", "courierName", "trackingNumber" },
            new[] { "Flat", "Resident", "Courier", "Tracking number" },
            StatusEndpoint: "api/societyadmin/parcels/{id}/status",
            ReportEndpoint: "api/societyadmin/reports/parcels",
            AllowEdit: false),
        ["complaints"] = new(
            "Complaints",
            "api/societyadmin/complaints",
            Array.Empty<string>(),
            Array.Empty<string>(),
            StatusEndpoint: "api/societyadmin/complaints/{id}/status",
            ReportEndpoint: "api/societyadmin/reports/complaints",
            AllowEdit: false),
        ["notifications"] = new(
            "Notifications",
            "api/societyadmin/notifications",
            new[] { "title", "message", "type", "userId" },
            new[] { "Title", "Message", "Type", "Recipient resident (optional)" },
            AllowEdit: false),
        ["amenity-logs"] = new(
            "Amenity access logs",
            "api/societyadmin/amenity-access-logs",
            Array.Empty<string>(),
            Array.Empty<string>(),
            ReportEndpoint: "api/societyadmin/reports/amenity-access",
            AllowEdit: false),
        ["amenity-pins"] = new(
            "Amenity PIN records",
            "api/societyadmin/amenity-pins",
            Array.Empty<string>(),
            Array.Empty<string>(),
            AllowEdit: false)
    };

    public SocietyAdminModulesController(ApiService api) => _api = api;

    private bool SignedIn() =>
        !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")) &&
        HttpContext.Session.GetString("Roles")?.Split(',')
            .Any(x => x.Equals("SocietyAdmin", StringComparison.OrdinalIgnoreCase)) == true;

    private IActionResult LoginRedirect() => RedirectToAction("Login", "Auth");

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
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return (new(), "Your session has expired. Please log in again.");

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return (new(), ApiMessage(body, $"Request failed (HTTP {(int)response.StatusCode})."));

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("data", out var data))
            return (new(), "The backend response did not include data.");

        if (data.ValueKind == JsonValueKind.Array)
            return (data.EnumerateArray().Select(x => x.Clone()).ToList(), null);
        if (data.ValueKind == JsonValueKind.Object)
            return (new() { data.Clone() }, null);
        return (new(), null);
    }

    private async Task<List<JsonElement>> SafeReadRows(string endpoint, CancellationToken ct)
    {
        try
        {
            var (rows, _) = await ReadRows(endpoint, ct);
            return rows;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return new();
        }
    }

    private async Task LoadLookupsAsync(string module, CancellationToken ct)
    {
        var key = module.ToLowerInvariant();
        var needWings = key is "floors" or "flats" or "residents" or "guests" or "parcels";
        var needFloors = key is "flats" or "residents" or "guests" or "parcels";
        var needFlats = key is "residents" or "guests" or "parcels";
        var needResidents = key is "guests" or "parcels" or "notifications";
        var needAmenities = key is "amenity-pins" or "amenity-logs" or "validate-pin";

        var wingsTask = needWings ? SafeReadRows("api/societyadmin/wings", ct) : Task.FromResult(new List<JsonElement>());
        var floorsTask = needFloors ? SafeReadRows("api/societyadmin/floors", ct) : Task.FromResult(new List<JsonElement>());
        var flatsTask = needFlats ? SafeReadRows("api/societyadmin/flats", ct) : Task.FromResult(new List<JsonElement>());
        var residentsTask = needResidents ? SafeReadRows("api/societyadmin/residents", ct) : Task.FromResult(new List<JsonElement>());
        var amenitiesTask = needAmenities ? SafeReadRows("api/societyadmin/amenities", ct) : Task.FromResult(new List<JsonElement>());

        await Task.WhenAll(wingsTask, floorsTask, flatsTask, residentsTask, amenitiesTask);

        ViewBag.Wings = wingsTask.Result;
        ViewBag.Floors = floorsTask.Result;
        ViewBag.Flats = flatsTask.Result;
        ViewBag.Residents = residentsTask.Result;
        ViewBag.Amenities = amenitiesTask.Result;
    }

    private static string ApiMessage(string body, string fallback)
    {
        try
        {
            using var d = JsonDocument.Parse(body);
            if (d.RootElement.TryGetProperty("message", out var m))
                return m.GetString() ?? fallback;
        }
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
            if (!response.IsSuccessStatusCode)
                ViewBag.ErrorMessage = ApiMessage(body, $"Dashboard request failed (HTTP {(int)response.StatusCode}).");
            else
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("data", out var data))
                    ViewBag.DashboardData = data.Clone();
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            ViewBag.ErrorMessage = "Unable to load dashboard data from the backend.";
        }
        ViewBag.FullName = HttpContext.Session.GetString("FullName") ?? "Society Admin";
        return View("~/Views/SocietyAdmin/Dashboard/Index.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Index(string module, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (string.IsNullOrWhiteSpace(module) || !Modules.TryGetValue(module, out var config))
            return NotFound();

        try
        {
            var rowsTask = ReadRows(config.Endpoint, ct);
            var lookupsTask = LoadLookupsAsync(module, ct);
            await Task.WhenAll(rowsTask, lookupsTask);

            var (rows, error) = rowsTask.Result;
            ViewBag.Module = module;
            ViewBag.Title = config.Title;
            ViewBag.Rows = rows;
            ViewBag.ErrorMessage = error;
            ViewBag.Fields = config.CreateFields;
            ViewBag.Labels = config.CreateLabels;
            ViewBag.AllowEdit = config.AllowEdit;
            return View(ModuleViewPath(module, "Index"));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            ViewBag.Module = module;
            ViewBag.Title = config.Title;
            ViewBag.Rows = new List<JsonElement>();
            ViewBag.Fields = config.CreateFields;
            ViewBag.Labels = config.CreateLabels;
            ViewBag.AllowEdit = config.AllowEdit;
            ViewBag.ErrorMessage = "Unable to connect to the backend.";
            return View(ModuleViewPath(module, "Index"));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(string module, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (string.IsNullOrWhiteSpace(module) || !Modules.TryGetValue(module, out var config) || config.CreateFields.Length == 0)
            return NotFound();

        await LoadLookupsAsync(module, ct);
        ViewBag.Module = module;
        ViewBag.Title = config.Title;
        ViewBag.Fields = config.CreateFields;
        ViewBag.Labels = config.CreateLabels;
        ViewBag.Values = new Dictionary<string, string>();
        return View(ModuleViewPath(module, "Form"));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string module, int id, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (string.IsNullOrWhiteSpace(module) || !Modules.TryGetValue(module, out var config) || !config.AllowEdit || config.EffectiveEditFields.Length == 0 || id <= 0)
            return NotFound();

        try
        {
            var responseTask = _api.GetAsync($"{config.Endpoint}/{id}", ct);
            var lookupsTask = LoadLookupsAsync(module, ct);
            await Task.WhenAll(responseTask, lookupsTask);

            using var response = responseTask.Result;
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = ApiMessage(body, "Unable to load record for editing.");
                return RedirectToAction(nameof(Index), new { module });
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("data", out var data))
                return RedirectToAction(nameof(Index), new { module });

            if (data.ValueKind == JsonValueKind.Array)
                data = data.EnumerateArray().FirstOrDefault();

            var values = new Dictionary<string, string>();
            foreach (var f in config.EffectiveEditFields)
            {
                if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty(f, out var v))
                    values[f] = v.ValueKind == JsonValueKind.Null ? "" : v.ToString();
            }

            ViewBag.Module = module;
            ViewBag.Title = config.Title;
            ViewBag.Fields = config.EffectiveEditFields;
            ViewBag.Labels = config.EffectiveEditLabels;
            ViewBag.Values = values;
            ViewBag.Id = id;
            return View(ModuleViewPath(module, "Form"));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            TempData["Error"] = "Unable to load record.";
            return RedirectToAction(nameof(Index), new { module });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string module, int id, Dictionary<string, string> fields, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (string.IsNullOrWhiteSpace(module) || !Modules.TryGetValue(module, out var config) || !config.AllowEdit || id <= 0)
            return NotFound();

        Dictionary<string, object?> payload;
        try
        {
            payload = BuildPayload(config.EffectiveEditFields, fields);
        }
        catch (ArgumentException ex)
        {
            await LoadLookupsAsync(module, ct);
            ViewBag.ErrorMessage = ex.Message;
            ViewBag.Module = module;
            ViewBag.Title = config.Title;
            ViewBag.Fields = config.EffectiveEditFields;
            ViewBag.Labels = config.EffectiveEditLabels;
            ViewBag.Values = fields;
            ViewBag.Id = id;
            return View(ModuleViewPath(module, "Form"));
        }

        try
        {
            using var response = await _api.PutAsync($"{config.Endpoint}/{id}", payload, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                await LoadLookupsAsync(module, ct);
                ViewBag.ErrorMessage = ApiMessage(body, $"Update failed (HTTP {(int)response.StatusCode}).");
                ViewBag.Module = module;
                ViewBag.Title = config.Title;
                ViewBag.Fields = config.EffectiveEditFields;
                ViewBag.Labels = config.EffectiveEditLabels;
                ViewBag.Values = fields;
                ViewBag.Id = id;
                return View(ModuleViewPath(module, "Form"));
            }
            TempData["Success"] = "Record updated successfully.";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            TempData["Error"] = "Unable to connect to the backend.";
        }
        return RedirectToAction(nameof(Index), new { module });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string module, Dictionary<string, string> fields, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (string.IsNullOrWhiteSpace(module) || !Modules.TryGetValue(module, out var config) || config.CreateFields.Length == 0)
            return NotFound();

        Dictionary<string, object?> payload;
        try
        {
            payload = BuildPayload(config.CreateFields, fields);
        }
        catch (ArgumentException ex)
        {
            await LoadLookupsAsync(module, ct);
            ViewBag.ErrorMessage = ex.Message;
            ViewBag.Module = module;
            ViewBag.Title = config.Title;
            ViewBag.Fields = config.CreateFields;
            ViewBag.Labels = config.CreateLabels;
            ViewBag.Values = fields;
            return View(ModuleViewPath(module, "Form"));
        }

        try
        {
            using var response = await _api.PostAsync(config.Endpoint, payload, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                await LoadLookupsAsync(module, ct);
                ViewBag.ErrorMessage = ApiMessage(body, $"Create failed (HTTP {(int)response.StatusCode}).");
                ViewBag.Module = module;
                ViewBag.Title = config.Title;
                ViewBag.Fields = config.CreateFields;
                ViewBag.Labels = config.CreateLabels;
                ViewBag.Values = fields;
                return View(ModuleViewPath(module, "Form"));
            }
            TempData["Success"] = $"{config.Title} record created successfully.";
            return RedirectToAction(nameof(Index), new { module });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            await LoadLookupsAsync(module, ct);
            ViewBag.ErrorMessage = "Unable to connect to the backend.";
            ViewBag.Module = module;
            ViewBag.Title = config.Title;
            ViewBag.Fields = config.CreateFields;
            ViewBag.Labels = config.CreateLabels;
            ViewBag.Values = fields;
            return View(ModuleViewPath(module, "Form"));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string module, int id, bool isActive, string? status, string? adminRemarks, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (string.IsNullOrWhiteSpace(module) || !Modules.TryGetValue(module, out var config) || string.IsNullOrWhiteSpace(config.StatusEndpoint) || id <= 0)
            return BadRequest();

        var endpoint = config.StatusEndpoint.Replace("{id}", id.ToString());
        object payload = module.Equals("complaints", StringComparison.OrdinalIgnoreCase)
            ? new { status = status ?? "InProgress", adminRemarks = adminRemarks ?? "" }
            : module.Equals("guests", StringComparison.OrdinalIgnoreCase)
                ? new { status = status ?? "Approved" }
                : module.Equals("parcels", StringComparison.OrdinalIgnoreCase)
                    ? new { status = status ?? "Collected" }
                    : new { isActive };
        try
        {
            using var response = await _api.PatchAsync(endpoint, payload, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                response.IsSuccessStatusCode
                    ? "Status updated successfully."
                    : ApiMessage(body, $"Status update failed (HTTP {(int)response.StatusCode}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            TempData["Error"] = "Unable to connect to the backend.";
        }
        return RedirectToAction(nameof(Index), new { module });
    }

    [HttpGet]
    public async Task<IActionResult> ValidateAmenityPin(CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        await LoadLookupsAsync("validate-pin", ct);
        return View("~/Views/SocietyAdmin/ValidateAmenityPin/Index.cshtml");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateAmenityPin(int amenityId, string pin, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        await LoadLookupsAsync("validate-pin", ct);
        ViewBag.SelectedAmenityId = amenityId;
        if (amenityId <= 0 || string.IsNullOrWhiteSpace(pin))
        {
            ViewBag.ErrorMessage = "Select an amenity and enter a valid PIN.";
            return View("~/Views/SocietyAdmin/ValidateAmenityPin/Index.cshtml");
        }

        try
        {
            using var response = await _api.PostAsync("api/societyadmin/amenity-pins/validate", new { amenityId, pin = pin.Trim() }, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
                ViewBag.SuccessMessage = ApiMessage(body, "Amenity PIN validation completed successfully.");
            else
                ViewBag.ErrorMessage = ApiMessage(body, $"PIN validation failed (HTTP {(int)response.StatusCode}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ViewBag.ErrorMessage = "Unable to connect to the backend.";
        }
        return View("~/Views/SocietyAdmin/ValidateAmenityPin/Index.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Report(string module, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (string.IsNullOrWhiteSpace(module) || !Modules.TryGetValue(module, out var config) || string.IsNullOrWhiteSpace(config.ReportEndpoint))
            return NotFound();

        try
        {
            var (rows, error) = await ReadRows(config.ReportEndpoint, ct);
            ViewBag.Rows = rows;
            ViewBag.ErrorMessage = error;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            ViewBag.Rows = new List<JsonElement>();
            ViewBag.ErrorMessage = "Unable to load report.";
        }
        ViewBag.Module = module;
        ViewBag.Title = config.Title + " report";
        return View(ModuleViewPath(module, "Report"));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateResidentAccount(int id, string email, string password, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (id <= 0 || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Resident ID, email and password are required.";
            return RedirectToAction(nameof(Index), new { module = "residents" });
        }

        try
        {
            using var response = await _api.PostAsync($"api/societyadmin/residents/{id}/create-account", new { email = email.Trim(), password }, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                response.IsSuccessStatusCode
                    ? "Resident login account created."
                    : ApiMessage(body, $"Account creation failed (HTTP {(int)response.StatusCode}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            TempData["Error"] = "Unable to connect to the backend.";
        }
        return RedirectToAction(nameof(Index), new { module = "residents" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkResidentAccount(int id, string applicationUserId, CancellationToken ct)
    {
        if (!SignedIn()) return LoginRedirect();
        if (id <= 0 || string.IsNullOrWhiteSpace(applicationUserId))
        {
            TempData["Error"] = "Resident ID and Application User ID are required.";
            return RedirectToAction(nameof(Index), new { module = "residents" });
        }

        try
        {
            using var response = await _api.PatchAsync(
                $"api/societyadmin/residents/{id}/link-account",
                new { applicationUserId = applicationUserId.Trim() },
                ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(ct);
            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                response.IsSuccessStatusCode
                    ? "Resident account linked successfully."
                    : ApiMessage(body, $"Account linking failed (HTTP {(int)response.StatusCode}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            TempData["Error"] = "Unable to connect to the backend.";
        }
        return RedirectToAction(nameof(Index), new { module = "residents" });
    }

    private static Dictionary<string, object?> BuildPayload(string[] fieldList, Dictionary<string, string> fields)
    {
        var result = new Dictionary<string, object?>();
        foreach (var key in fieldList)
        {
            fields.TryGetValue(key, out var value);
            value ??= "";

            if (key == "userId")
            {
                if (!string.IsNullOrWhiteSpace(value))
                    result[key] = value.Trim();
                continue;
            }

            if (key == "floorNumber")
            {
                if (!int.TryParse(value, out var floorNo) || floorNo < 0)
                    throw new ArgumentException("Floor number must be 0 or greater.");
                result[key] = floorNo;
            }
            else if (key.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(value, out var number) || number <= 0)
                    throw new ArgumentException($"Please select a valid {key.Replace("Id", "")}.");
                result[key] = number;
            }
            else if (key is "isOccupied" or "isHome")
            {
                result[key] = value.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("on", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                var trimmed = value.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    throw new ArgumentException($"{key} is required.");

                if (key.EndsWith("Time", StringComparison.OrdinalIgnoreCase) && trimmed.Length == 5)
                    trimmed += ":00";

                result[key] = trimmed;
            }
        }
        return result;
    }
}
