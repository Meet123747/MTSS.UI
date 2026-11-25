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

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();

        try
        {
            var complaints = await GetRows("api/resident/complaints", cancellationToken);
            if (complaints.Unauthorized) return LoginRedirect();

            var notifications = await GetRows("api/resident/notifications", cancellationToken);
            if (notifications.Unauthorized) return LoginRedirect();

            var pins = await GetRows("api/resident/amenity-pins", cancellationToken);
            if (pins.Unauthorized) return LoginRedirect();

            ViewBag.ComplaintCount = complaints.Rows.Count;
            ViewBag.NotificationCount = notifications.Rows.Count;
            ViewBag.PinCount = pins.Rows.Count;
            ViewBag.RecentNotifications = notifications.Rows.Take(5).ToList();
            ViewBag.ComplaintError = complaints.Error;
            ViewBag.NotificationError = notifications.Error;
            ViewBag.PinError = pins.Error;
            ViewBag.FullName = HttpContext.Session.GetString("FullName") ?? "Resident";
            return View("~/Views/Resident/Dashboard/Index.cshtml");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            ViewBag.ErrorMessage = "We couldn't load your resident overview. Please try again.";
            ViewBag.FullName = HttpContext.Session.GetString("FullName") ?? "Resident";
            return View("~/Views/Resident/Dashboard/Index.cshtml");
        }
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
    public async Task<IActionResult> Amenities(CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        try
        {
            var result = await GetRows("api/resident/amenity-pins", cancellationToken);
            if (result.Unauthorized) return LoginRedirect();
            ViewBag.Rows = result.Rows;
            ViewBag.ErrorMessage = result.Error;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            ViewBag.Rows = new List<JsonElement>();
            ViewBag.ErrorMessage = "Unable to load your amenity PINs.";
        }
        return View("~/Views/Resident/Amenities/Index.cshtml");
    }

    [HttpGet]
    public IActionResult GeneratePin()
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        return View("~/Views/Resident/Amenities/GeneratePin.cshtml");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GeneratePin(int amenityId, CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        if (amenityId <= 0)
        {
            ViewBag.ErrorMessage = "Enter a valid amenity ID.";
            ViewBag.AmenityId = amenityId;
            return View("~/Views/Resident/Amenities/GeneratePin.cshtml");
        }

        try
        {
            using var response = await _api.PostAsync(
                "api/resident/amenity-pins",
                new { amenityId },
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized) return LoginRedirect();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.ErrorMessage = ReadMessage(body, $"PIN generation failed (HTTP {(int)response.StatusCode}).");
                ViewBag.AmenityId = amenityId;
                return View("~/Views/Resident/Amenities/GeneratePin.cshtml");
            }

            using var document = JsonDocument.Parse(body);
            ViewBag.GeneratedData = document.RootElement.TryGetProperty("data", out var data) ? data.Clone() : default;
            ViewBag.SuccessMessage = ReadMessage(body, "Your amenity PIN was generated.");
            return View("~/Views/Resident/Amenities/Generated.cshtml");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            ViewBag.ErrorMessage = "Unable to generate a PIN right now.";
            ViewBag.AmenityId = amenityId;
            return View("~/Views/Resident/Amenities/GeneratePin.cshtml");
        }
    }

    [HttpGet]
    public async Task<IActionResult> PinDetails(int id, CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        if (id <= 0) return BadRequest();
        try
        {
            var result = await GetOne($"api/resident/amenity-pins/{id}", cancellationToken);
            if (result.Unauthorized) return LoginRedirect();
            if (result.Error != null)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Amenities));
            }
            ViewBag.Record = result.Data;
            return View("~/Views/Resident/Amenities/Details.cshtml");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            TempData["Error"] = "Unable to load PIN details.";
            return RedirectToAction(nameof(Amenities));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokePin(int id, CancellationToken cancellationToken)
    {
        if (!IsResidentSignedIn()) return LoginRedirect();
        if (id <= 0) return BadRequest();
        return await SendBodylessPatch($"api/resident/amenity-pins/{id}/revoke", nameof(Amenities), cancellationToken);
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
