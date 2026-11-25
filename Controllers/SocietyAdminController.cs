
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MTSS.UI.Services;
using MTSS.UI.Models.ApiModels.Wings;
using System.Text.Json;

namespace MTSS.UI.Controllers
{
    public class SocietyAdminController : Controller
    {
        private readonly ApiService _apiService;

        public SocietyAdminController(ApiService apiService)
        {
            _apiService = apiService;
        }

        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(
                HttpContext.Session.GetString("UserId"));
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Auth");
            return RedirectToAction("Dashboard", "SocietyAdminModules");
        }

        // GET: SocietyAdmin/Wings
        [HttpGet]
        public async Task<IActionResult> Wings()
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                using var response = await _apiService.GetAsync(
                    "api/societyadmin/wings");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        $"Unable to retrieve wings. HTTP Status: {(int)response.StatusCode}.";

                    return View("Wings/Index",
                        new List<WingListItem>());
                }

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(responseBody);

                if (!document.RootElement.TryGetProperty(
                    "data", out var data) ||
                    data.ValueKind != JsonValueKind.Array)
                {
                    ViewBag.ErrorMessage =
                        "The backend response does not contain valid wing data.";

                    return View("Wings/Index",
                        new List<WingListItem>());
                }

                var wings = JsonSerializer.Deserialize<List<WingListItem>>(
                    data.GetRawText(),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                ) ?? new List<WingListItem>();

                return View("Wings/Index", wings);
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to the backend.";

                return View("Wings/Index",
                    new List<WingListItem>());
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage =
                    "Unable to read the wing data returned by the backend.";

                return View("Wings/Index",
                    new List<WingListItem>());
            }
        }

        // GET: SocietyAdmin/CreateWing
        [HttpGet]
        public IActionResult CreateWing()
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            return View("Wings/Create");
        }

        // POST: SocietyAdmin/CreateWing
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWing(string name)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            name = name?.Trim() ?? "";

            if (name.Length < 2 || name.Length > 100)
            {
                ViewBag.ErrorMessage =
                    "Wing name must contain between 2 and 100 characters.";

                return View("Wings/Create");
            }

            try
            {
                var requestBody = new
                {
                    name = name
                };

                using var response = await _apiService.PostAsync(
                    "api/societyadmin/wings",
                    requestBody);

                if (response.IsSuccessStatusCode)
                {
                    TempData["WingSuccess"] =
                        $"'{name}' was created successfully.";

                    return RedirectToAction("Wings");
                }

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                string errorMessage =
                    $"Unable to create wing. HTTP Status: {(int)response.StatusCode}.";

                try
                {
                    using var document =
                        JsonDocument.Parse(responseBody);

                    if (document.RootElement.TryGetProperty(
                        "message", out var message))
                    {
                        errorMessage = message.GetString()
                            ?? errorMessage;
                    }
                }
                catch (JsonException)
                {
                    // Keep fallback error message.
                }

                ViewBag.ErrorMessage = errorMessage;

                return View("Wings/Create");
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to the backend. Please try again.";

                return View("Wings/Create");
            }
            catch (TaskCanceledException)
            {
                ViewBag.ErrorMessage =
                    "The request timed out. Please try again.";

                return View("Wings/Create");
            }
        }

        // GET: SocietyAdmin/DetailsWing/1
        [HttpGet]
        public async Task<IActionResult> DetailsWing(int id)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (id <= 0)
            {
                TempData["WingError"] = "Invalid wing ID.";
                return RedirectToAction("Wings");
            }

            try
            {
                using var response = await _apiService.GetAsync(
                    $"api/societyadmin/wings/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["WingError"] =
                        $"Unable to retrieve wing details. HTTP Status: {(int)response.StatusCode}.";

                    return RedirectToAction("Wings");
                }

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(responseBody);

                if (!document.RootElement.TryGetProperty(
                    "data", out var data) ||
                    data.ValueKind != JsonValueKind.Object)
                {
                    TempData["WingError"] =
                        "Wing details were not found in the backend response.";

                    return RedirectToAction("Wings");
                }

                var wing = JsonSerializer.Deserialize<WingListItem>(
                    data.GetRawText(),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (wing == null)
                {
                    TempData["WingError"] =
                        "Unable to read wing details.";

                    return RedirectToAction("Wings");
                }

                return View("Wings/Details", wing);
            }
            catch (HttpRequestException)
            {
                TempData["WingError"] =
                    "Unable to connect to the backend.";

                return RedirectToAction("Wings");
            }
            catch (JsonException)
            {
                TempData["WingError"] =
                    "Unable to read the wing details returned by the backend.";

                return RedirectToAction("Wings");
            }
            catch (TaskCanceledException)
            {
                TempData["WingError"] =
                    "The request timed out. Please try again.";

                return RedirectToAction("Wings");
            }
        }

        // POST: SocietyAdmin/UpdateWingStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWingStatus(
            int id, bool isActive)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (id <= 0)
            {
                TempData["WingError"] = "Invalid wing ID.";
                return RedirectToAction("Wings");
            }

            try
            {
                var requestBody = new
                {
                    isActive = isActive
                };

                using var response = await _apiService.PatchAsync(
                    $"api/societyadmin/wings/{id}/status",
                    requestBody);

                if (response.IsSuccessStatusCode)
                {
                    TempData["WingSuccess"] = isActive
                        ? "Wing activated successfully."
                        : "Wing deactivated successfully.";

                    return RedirectToAction("Wings");
                }

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                string errorMessage =
                    $"Unable to update wing status. HTTP Status: {(int)response.StatusCode}.";

                try
                {
                    using var document =
                        JsonDocument.Parse(responseBody);

                    if (document.RootElement.TryGetProperty(
                        "message", out var message))
                    {
                        errorMessage = message.GetString()
                            ?? errorMessage;
                    }
                }
                catch (JsonException)
                {
                    // Keep fallback error message.
                }

                TempData["WingError"] = errorMessage;

                return RedirectToAction("Wings");
            }
            catch (HttpRequestException)
            {
                TempData["WingError"] =
                    "Unable to connect to the backend.";

                return RedirectToAction("Wings");
            }
            catch (TaskCanceledException)
            {
                TempData["WingError"] =
                    "The request timed out. Please try again.";

                return RedirectToAction("Wings");
            }
        }
    }
}
