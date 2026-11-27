using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MTSS.UI.Services;

namespace MTSS.UI.Controllers;

public class ResidentController : Controller
{
    private readonly ApiService _api;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ResidentController(ApiService api) => _api = api;

    private bool IsResidentSignedIn() =>
        !string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")) &&
        HttpContext.Session.GetString("Roles")?.Split(',')
            .Any(role => role.Equals("Resident", StringComparison.OrdinalIgnoreCase)) == true;

    private IActionResult LoginRedirect() => RedirectToAction("Login", "Auth");

    [HttpGet]
    public IActionResult Index()
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        return RedirectToAction(nameof(Dashboard));
    }

    private static string ReadMessage(string body, string fallback)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
                return message.GetString() ?? fallback;
        }
        catch (JsonException) { }
        return fallback;
    }

    private async Task<(List<JsonElement> Rows, string? Error, bool Unauthorized)> GetRows(
        string endpoint, CancellationToken cancellationToken)
    {
        using var response = await _api.GetAsync(endpoint, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return (new(), "Your session has expired. Please sign in again.", true);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return (new(), ReadMessage(body, $"Request failed (HTTP {(int)response.StatusCode})."), false);

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("data", out var data))
            return (new(), "The API response did not contain a data field.", false);

        if (data.ValueKind == JsonValueKind.Array)
            return (data.EnumerateArray().Select(item => item.Clone()).ToList(), null, false);
        if (data.ValueKind == JsonValueKind.Object)
            return (new() { data.Clone() }, null, false);
        return (new(), null, false);
    }

    private async Task<(JsonElement? Data, string? Error, bool Unauthorized)> GetOne(
        string endpoint, CancellationToken cancellationToken)
    {
        using var response = await _api.GetAsync(endpoint, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return (null, "Your session has expired. Please sign in again.", true);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return (null, ReadMessage(body, $"Request failed (HTTP {(int)response.StatusCode})."), false);

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("data", out var data))
            return (null, "The API response did not contain a data field.", false);
        return (data.Clone(), null, false);
    }

    private static string ReadStringProp(JsonElement row, string key) =>
        row.ValueKind == JsonValueKind.Object &&
        row.TryGetProperty(key, out var prop) &&
        prop.ValueKind != JsonValueKind.Null
            ? prop.ToString()
            : "";

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();

        ViewBag.FullName = HttpContext.Session.GetString("FullName") ?? "Resident";
        ViewBag.Email = HttpContext.Session.GetString("Email") ?? "";

        try
        {
            var complaintsTask = GetRows("api/resident/complaints", cancellationToken);
            var notificationsTask = GetRows("api/resident/notifications", cancellationToken);
            var pinsTask = GetRows("api/resident/amenity-pins", cancellationToken);
            await Task.WhenAll(complaintsTask, notificationsTask, pinsTask);

            var complaints = complaintsTask.Result;
            var notifications = notificationsTask.Result;
            var pins = pinsTask.Result;

            if (complaints.Unauthorized || notifications.Unauthorized || pins.Unauthorized)
                return LoginRedirect();

            ViewBag.ComplaintCount = complaints.Rows.Count;
            ViewBag.OpenComplaintCount = complaints.Rows.Count(r =>
                ReadStringProp(r, "status").Equals("Open", StringComparison.OrdinalIgnoreCase) ||
                ReadStringProp(r, "status").Equals("Pending", StringComparison.OrdinalIgnoreCase));
            ViewBag.InProgressComplaintCount = complaints.Rows.Count(r =>
                ReadStringProp(r, "status").Contains("Progress", StringComparison.OrdinalIgnoreCase));
            ViewBag.ResolvedComplaintCount = complaints.Rows.Count(r =>
                ReadStringProp(r, "status").Equals("Resolved", StringComparison.OrdinalIgnoreCase) ||
                ReadStringProp(r, "status").Equals("Approved", StringComparison.OrdinalIgnoreCase));
            ViewBag.RecentComplaints = complaints.Rows.Take(5).ToList();

            ViewBag.NotificationCount = notifications.Rows.Count;
            ViewBag.UnreadNotificationCount = notifications.Rows.Count(r =>
                !ReadStringProp(r, "isRead").Equals("true", StringComparison.OrdinalIgnoreCase));
            ViewBag.RecentNotifications = notifications.Rows.Take(5).ToList();

            ViewBag.PinCount = pins.Rows.Count;
            ViewBag.ActivePinCount = pins.Rows.Count(r =>
            {
                var status = ReadStringProp(r, "status");
                return !status.Contains("Revoked", StringComparison.OrdinalIgnoreCase) &&
                       !status.Contains("Used", StringComparison.OrdinalIgnoreCase) &&
                       !status.Contains("Expired", StringComparison.OrdinalIgnoreCase);
            });
            ViewBag.RecentPins = pins.Rows.Take(5).ToList();

            ViewBag.ComplaintError = complaints.Error;
            ViewBag.NotificationError = notifications.Error;
            ViewBag.PinError = pins.Error;
            return View("~/Views/Resident/Dashboard/Index.cshtml");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            ViewBag.ErrorMessage = "We couldn't load your resident overview. Please try again.";
            return View("~/Views/Resident/Dashboard/Index.cshtml");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Guests(CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();

        ViewBag.Rows = new List<JsonElement>();
        try
        {
            var result = await GetRows("api/resident/guest-entries", cancellationToken);
            if (result.Unauthorized) return LoginRedirect();

            ViewBag.Rows = result.Rows;
            ViewBag.LoadFailed = result.Error != null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            ViewBag.LoadFailed = true;
        }

        if (ViewBag.LoadFailed is bool failed && failed)
            ViewBag.ErrorMessage = "Guest entries are temporarily unavailable. Please try again later.";

        return View("~/Views/Resident/Guests/Index.cshtml");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RespondToGuestEntry(int id, string? status, CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        if (id <= 0 || string.IsNullOrWhiteSpace(status) ||
            !(status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
              status.Equals("Rejected", StringComparison.OrdinalIgnoreCase)))
        {
            TempData["Error"] = "Invalid guest-entry response. Please choose Approve or Reject again.";
            return RedirectToAction(nameof(Guests));
        }

        try
        {
            using var response = await _api.PatchAsync(
                $"api/resident/guest-entries/{id}/status",
                new { status },
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                response.IsSuccessStatusCode
                    ? $"Guest entry {status.ToLowerInvariant()}."
                    : "We couldn't update this guest entry. Please refresh and try again.";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            TempData["Error"] = "Unable to connect to the guest-entry service. Please try again.";
        }

        return RedirectToAction(nameof(Guests));
    }

    [HttpGet]
    public IActionResult Parcels()
    {
        if (!IsResidentSignedIn()) return LoginRedirect();

        // The current backend only exposes /api/societyadmin/parcels (SocietyAdmin-only).
        // Do not call SocietyAdmin endpoints from a Resident session.
        ViewBag.EndpointUnavailable = true;
        ViewBag.Rows = new List<JsonElement>();
        return View("~/Views/Resident/Parcels/Index.cshtml");
    }

    [HttpGet]
    public IActionResult ParcelDetails(int id)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        return RedirectToAction(nameof(Parcels));
    }

    [HttpGet]
    public IActionResult Profile()
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        ViewBag.UserId = HttpContext.Session.GetString("UserId") ?? "";
        ViewBag.FullName = HttpContext.Session.GetString("FullName") ?? "Resident";
        ViewBag.Email = HttpContext.Session.GetString("Email") ?? "";
        ViewBag.Roles = HttpContext.Session.GetString("Roles") ?? "Resident";
        return View("~/Views/Resident/Profile/Index.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Complaints(CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        try
        {
            var result = await GetRows("api/resident/complaints", cancellationToken);
            if (result.Unauthorized) return LoginRedirect();
            ViewBag.Rows = result.Rows;
            ViewBag.ErrorMessage = result.Error;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            ViewBag.Rows = new List<JsonElement>();
            ViewBag.ErrorMessage = "Unable to load complaints from the backend.";
        }
        return View("~/Views/Resident/Complaints/Index.cshtml");
    }

    [HttpGet]
    public IActionResult CreateComplaint()
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        return View("~/Views/Resident/Complaints/Create.cshtml");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateComplaint(string subject, string description, CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();

        subject = subject?.Trim() ?? "";
        description = description?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(description))
        {
            ViewBag.ErrorMessage = "Subject and description are required.";
            ViewBag.Subject = subject;
            ViewBag.Description = description;
            return View("~/Views/Resident/Complaints/Create.cshtml");
        }

        try
        {
            using var response = await _api.PostAsync(
                "api/resident/complaints",
                new { subject, description },
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = ReadMessage(body, $"Complaint submission failed (HTTP {(int)response.StatusCode}).");
                ViewBag.Subject = subject;
                ViewBag.Description = description;
                return View("~/Views/Resident/Complaints/Create.cshtml");
            }

            TempData["Success"] = ReadMessage(body, "Your complaint was submitted successfully.");
            return RedirectToAction(nameof(Complaints));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ViewBag.ErrorMessage = "Unable to connect to the backend.";
            ViewBag.Subject = subject;
            ViewBag.Description = description;
            return View("~/Views/Resident/Complaints/Create.cshtml");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ComplaintDetails(int id, CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        if (id <= 0) return BadRequest();

        try
        {
            var result = await GetOne($"api/resident/complaints/{id}", cancellationToken);
            if (result.Unauthorized) return LoginRedirect();
            if (result.Error != null)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Complaints));
            }
            ViewBag.Record = result.Data;
            return View("~/Views/Resident/Complaints/Details.cshtml");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            TempData["Error"] = "Unable to load complaint details.";
            return RedirectToAction(nameof(Complaints));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Notifications(CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        try
        {
            var result = await GetRows("api/resident/notifications", cancellationToken);
            if (result.Unauthorized) return LoginRedirect();
            ViewBag.Rows = result.Rows;
            ViewBag.ErrorMessage = result.Error;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            ViewBag.Rows = new List<JsonElement>();
            ViewBag.ErrorMessage = "Unable to load notifications.";
        }
        return View("~/Views/Resident/Notifications/Index.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> NotificationDetails(int id, CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        if (id <= 0) return BadRequest();
        try
        {
            var result = await GetOne($"api/resident/notifications/{id}", cancellationToken);
            if (result.Unauthorized) return LoginRedirect();
            if (result.Error != null)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Notifications));
            }
            ViewBag.Record = result.Data;
            return View("~/Views/Resident/Notifications/Details.cshtml");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            TempData["Error"] = "Unable to load notification details.";
            return RedirectToAction(nameof(Notifications));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int id, CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        if (id <= 0) return BadRequest();
        return await SendBodylessPatch($"api/resident/notifications/{id}/read", nameof(Notifications), cancellationToken);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllNotificationsRead(CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        return await SendBodylessPatch("api/resident/notifications/read-all", nameof(Notifications), cancellationToken);
    }

    [HttpGet]
    public IActionResult Amenities(CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        ViewBag.Rows = new List<JsonElement>();
        ViewBag.AmenitiesUnavailable = true;
        return View("~/Views/Resident/Amenities/Index.cshtml");
    }

    private async Task<bool> LoadKnownAmenitiesFromPinHistoryAsync(CancellationToken cancellationToken)
    {
        // The current backend does not expose a resident-scoped amenities list endpoint.
        // Use the existing GET /api/resident/amenity-pins endpoint to surface any amenities
        // the resident has previously generated PINs for as quick-fill suggestions.
        var knownAmenities = new List<(string Id, string Name)>();
        try
        {
            var result = await GetRows("api/resident/amenity-pins", cancellationToken);
            if (result.Unauthorized) return false;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in result.Rows)
            {
                var aid = ReadStringProp(row, "amenityId");
                var name = ReadStringProp(row, "amenityName");
                if (!string.IsNullOrWhiteSpace(aid) && seen.Add(aid))
                {
                    knownAmenities.Add((aid, string.IsNullOrWhiteSpace(name) ? $"Amenity #{aid}" : name));
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            // Non-fatal: the manual Amenity ID input remains usable.
        }

        ViewBag.KnownAmenities = knownAmenities;
        return true;
    }

    [HttpGet]
    public IActionResult GeneratePin(CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        return RedirectToAction(nameof(Amenities));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult GeneratePin(int amenityId, CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        return RedirectToAction(nameof(Amenities));
    }

    [HttpGet]
    public IActionResult PinDetails(int id, CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        return RedirectToAction(nameof(Amenities));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RevokePin(int id, CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        return RedirectToAction(nameof(Amenities));
    }

    private async Task<IActionResult> SendBodylessPatch(string endpoint, string returnAction, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _api.PatchNoContentAsync(endpoint, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                response.IsSuccessStatusCode
                    ? ReadMessage(body, "Your request was completed successfully.")
                    : ReadMessage(body, $"Request failed (HTTP {(int)response.StatusCode}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            TempData["Error"] = "Unable to connect to the backend.";
        }
        return RedirectToAction(returnAction);
    }
}
