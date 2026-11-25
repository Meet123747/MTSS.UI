
using Microsoft.AspNetCore.Mvc;
using MTSS.UI.Models.ApiModels.Societies;
using MTSS.UI.Models.ApiModels.SubscriptionPlans;
using MTSS.UI.Models.ApiModels.SocietySubscriptions;
using MTSS.UI.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MTSS.UI.Models.ApiModels.SocietyAdmins;

namespace MTSS.UI.Controllers
{
    public class SuperAdminController : Controller
    {
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;

        public SuperAdminController(
            ApiService apiService,
            SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
        }



        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            ViewBag.TotalSocieties = "--";
            ViewBag.ActiveSocieties = "--";
            ViewBag.TotalPlans = "--";
            ViewBag.ActivePlans = "--";

            // Store recent society records for the dashboard
            ViewBag.RecentSocieties = new List<SocietyListItem>();

            try
            {
                // Fetch societies
                using var societyResponse = await _apiService.GetAsync(
                    "/api/superadmin/societies",
                    cancellationToken);

                if (societyResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (societyResponse.IsSuccessStatusCode)
                {
                    var societyResult = await societyResponse.Content
                        .ReadFromJsonAsync<SocietyListResponse>(
                            cancellationToken: cancellationToken);

                    if (societyResult != null && societyResult.Success)
                    {
                        var societies = societyResult.Data
                            ?? new List<SocietyListItem>();

                        ViewBag.TotalSocieties = societies.Count;
                        ViewBag.ActiveSocieties =
                            societies.Count(s => s.IsActive);

                        ViewBag.RecentSocieties = societies
                            .OrderByDescending(s => s.CreatedAt)
                            .Take(5)
                            .ToList();
                    }
                }

                // Fetch subscription plans
                using var planResponse = await _apiService.GetAsync(
                    "/api/superadmin/subscription-plans",
                    cancellationToken);

                if (planResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (planResponse.IsSuccessStatusCode)
                {
                    var planResult = await planResponse.Content
                        .ReadFromJsonAsync<SubscriptionPlanListResponse>(
                            cancellationToken: cancellationToken);

                    if (planResult != null && planResult.Success)
                    {
                        var plans = planResult.Data
                            ?? new List<SubscriptionPlanListItem>();

                        ViewBag.TotalPlans = plans.Count;
                        ViewBag.ActivePlans =
                            plans.Count(p => p.IsActive);
                    }
                }

                return View();
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to the MTSS backend.";

                return View();
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage =
                    "The backend returned an invalid dashboard response.";

                return View();
            }
        }
        // Fetch societies from the live backend API
        [HttpGet]
        public async Task<IActionResult> Societies(
            CancellationToken cancellationToken)
        {
            var societies = new List<SocietyListItem>();

            try
            {
                var backendCookie = _sessionService.GetBackendCookie();

                Console.WriteLine(
                    $"Backend cookie available in session: {!string.IsNullOrWhiteSpace(backendCookie)}");

                using var response = await _apiService.GetAsync(
                    "/api/superadmin/societies",
                    cancellationToken);

                Console.WriteLine(
                    $"Societies API HTTP Status: {(int)response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        response.StatusCode == HttpStatusCode.Unauthorized
                        ? "Your session has expired. Please log in again."
                        : "Unable to retrieve societies from the backend.";

                    return View("Societies/Index", societies);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<SocietyListResponse>(
                        cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                {
                    ViewBag.ErrorMessage =
                        result?.Message ?? "Unable to load societies.";

                    return View("Societies/Index", societies);
                }

                societies = result.Data ?? new List<SocietyListItem>();

                return View("Societies/Index", societies);
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to the MTSS backend.";

                return View("Societies/Index", societies);
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage =
                    "The backend returned an invalid society response.";

                return View("Societies/Index", societies);
            }
        }

        // Display society details
        [HttpGet]
        public async Task<IActionResult> SocietyDetails(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                using var response = await _apiService.GetAsync(
                    $"/api/superadmin/societies/{id}",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("Society not found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode,
                        "Unable to retrieve society details.");
                }

                var result = await response.Content
                    .ReadFromJsonAsync<SocietyResponse>(
                        cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                {
                    ViewBag.ErrorMessage =
                        result?.Message ?? "Unable to load society details.";

                    return View("Societies/Details");
                }

                var society = result.Data.Deserialize<SocietyListItem>(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (society == null)
                {
                    return NotFound("Society details were not available.");
                }

                return View("Societies/Details", society);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503,
                    "Unable to connect to the MTSS backend.");
            }
            catch (JsonException)
            {
                return StatusCode(502,
                    "The backend returned an invalid response.");
            }
        }

        // Load existing society information for editing
        [HttpGet]
        public async Task<IActionResult> EditSociety(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                using var response = await _apiService.GetAsync(
                    $"/api/superadmin/societies/{id}",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("Society not found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode,
                        "Unable to load society for editing.");
                }

                var result = await response.Content
                    .ReadFromJsonAsync<SocietyResponse>(
                        cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                {
                    ViewBag.ErrorMessage =
                        result?.Message ?? "Unable to load society information.";

                    return View("Societies/Edit");
                }

                var society = result.Data.Deserialize<SocietyListItem>(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (society == null)
                {
                    return NotFound("Society details were not available.");
                }

                var model = new SocietyCreateRequest
                {
                    Name = society.Name,
                    Address = society.Address,
                    City = society.City,
                    State = society.State,
                    Pincode = society.Pincode
                };

                ViewBag.SocietyId = society.SocietyId;

                return View("Societies/Edit", model);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503,
                    "Unable to connect to the MTSS backend.");
            }
            catch (JsonException)
            {
                return StatusCode(502,
                    "The backend returned an invalid response.");
            }
        }

        // Update existing society
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSociety(
            int id,
            SocietyCreateRequest model,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            ViewBag.SocietyId = id;

            if (!ModelState.IsValid)
            {
                return View("Societies/Edit", model);
            }

            try
            {
                using var response = await _apiService.PutAsync(
                    $"/api/superadmin/societies/{id}",
                    model,
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        "Unable to update society. Please check the details.";

                    return View("Societies/Edit", model);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<SocietyResponse>(
                        cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                {
                    ViewBag.ErrorMessage =
                        result?.Message ?? "Society update failed.";

                    return View("Societies/Edit", model);
                }

                TempData["SuccessMessage"] =
                    result.Message ?? "Society updated successfully.";

                return RedirectToAction(
                    nameof(SocietyDetails),
                    new { id });
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to the MTSS backend.";

                return View("Societies/Edit", model);
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage =
                    "The backend returned an invalid response.";

                return View("Societies/Edit", model);
            }
        }

        // Display Create Society page
        [HttpGet]
        public IActionResult CreateSociety()
        {
            return View("Societies/Create");
        }

        // Display Create Subscription Plan page
        [HttpGet]
        public IActionResult CreateSubscriptionPlan()
        {
            return View("SubscriptionPlans/Create");
        }

        // Display subscription plan details
        [HttpGet]
        public async Task<IActionResult> SubscriptionPlanDetails(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                using var response = await _apiService.GetAsync(
                    $"/api/superadmin/subscription-plans/{id}",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("Subscription plan not found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)response.StatusCode,
                        "Unable to retrieve subscription plan details.");
                }

                var result = await response.Content
                    .ReadFromJsonAsync<SubscriptionPlanResponse>(
                        cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                {
                    ViewBag.ErrorMessage =
                        result?.Message ?? "Unable to load subscription plan.";

                    return RedirectToAction(nameof(SubscriptionPlans));
                }

                var plan = result.Data.Deserialize<SubscriptionPlanListItem>(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (plan == null)
                {
                    return NotFound("Subscription plan details were not available.");
                }

                return View("SubscriptionPlans/Details", plan);
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    503,
                    "Unable to connect to the MTSS backend.");
            }
            catch (JsonException)
            {
                return StatusCode(
                    502,
                    "The backend returned an invalid subscription plan response.");
            }
        }

        // Load existing subscription plan information for editing
        [HttpGet]
        public async Task<IActionResult> EditSubscriptionPlan(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                using var response = await _apiService.GetAsync(
                    $"/api/superadmin/subscription-plans/{id}",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("Subscription plan not found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)response.StatusCode,
                        "Unable to load subscription plan for editing.");
                }

                var result = await response.Content
                    .ReadFromJsonAsync<SubscriptionPlanResponse>(
                        cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                {
                    TempData["ErrorMessage"] =
                        result?.Message ?? "Unable to load subscription plan.";

                    return RedirectToAction(nameof(SubscriptionPlans));
                }

                var plan = result.Data.Deserialize<SubscriptionPlanListItem>(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (plan == null)
                {
                    return NotFound(
                        "Subscription plan details were not available.");
                }

                return View("SubscriptionPlans/Edit", plan);
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    503,
                    "Unable to connect to the MTSS backend.");
            }
            catch (JsonException)
            {
                return StatusCode(
                    502,
                    "The backend returned an invalid subscription plan response.");
            }
        }

        // Update an existing subscription plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubscriptionPlan(
            int id,
            SubscriptionPlanCreateRequest model,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            // Rebuild the edit view model so entered values are preserved.
            SubscriptionPlanListItem BuildEditModel()
            {
                return new SubscriptionPlanListItem
                {
                    SubscriptionPlanId = id,
                    Name = model.Name,
                    MaxWings = model.MaxWings,
                    MaxFloorsPerWing = model.MaxFloorsPerWing,
                    MaxFlats = model.MaxFlats,
                    MaxAmenities = model.MaxAmenities,
                    MaxSocietyAdmins = model.MaxSocietyAdmins,
                    GuestApprovalEnabled = model.GuestApprovalEnabled,
                    ParcelRegisterEnabled = model.ParcelRegisterEnabled,
                    AmenityPinEnabled = model.AmenityPinEnabled,
                    ComplaintManagementEnabled = model.ComplaintManagementEnabled,
                    ReportsEnabled = model.ReportsEnabled
                };
            }

            if (!ModelState.IsValid)
            {
                return View("SubscriptionPlans/Edit", BuildEditModel());
            }

            try
            {
                using var response = await _apiService.PutAsync(
                    $"/api/superadmin/subscription-plans/{id}",
                    model,
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("Subscription plan not found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        "Unable to update subscription plan. Please check the details.";

                    return View("SubscriptionPlans/Edit", BuildEditModel());
                }

                var result = await response.Content
                    .ReadFromJsonAsync<SubscriptionPlanResponse>(
                        cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                {
                    ViewBag.ErrorMessage =
                        result?.Message ?? "Subscription plan update failed.";

                    return View("SubscriptionPlans/Edit", BuildEditModel());
                }

                TempData["SuccessMessage"] =
                    result.Message ?? "Subscription plan updated successfully.";

                return RedirectToAction(
                    nameof(SubscriptionPlanDetails),
                    new { id });
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to the MTSS backend.";

                return View("SubscriptionPlans/Edit", BuildEditModel());
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage =
                    "The backend returned an invalid response.";

                return View("SubscriptionPlans/Edit", BuildEditModel());
            }
        }

        // Display all subscription plans
        [HttpGet]
        public async Task<IActionResult> SubscriptionPlans(
            CancellationToken cancellationToken)
        {
            var plans = new List<SubscriptionPlanListItem>();

            try
            {
                using var response = await _apiService.GetAsync(
                    "/api/superadmin/subscription-plans",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        "Unable to retrieve subscription plans.";

                    return View("SubscriptionPlans/Index", plans);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<SubscriptionPlanListResponse>(
                        cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                {
                    ViewBag.ErrorMessage =
                        result?.Message ?? "Unable to load subscription plans.";

                    return View("SubscriptionPlans/Index", plans);
                }

                plans = result.Data ?? new List<SubscriptionPlanListItem>();

                return View("SubscriptionPlans/Index", plans);
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to the MTSS backend.";

                return View("SubscriptionPlans/Index", plans);
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage =
                    "The backend returned an invalid subscription plan response.";

                return View("SubscriptionPlans/Index", plans);
            }
        }

        // Create a new subscription plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubscriptionPlan(
            SubscriptionPlanCreateRequest model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View("SubscriptionPlans/Create", model);
            }

            try
            {
                using var response = await _apiService.PostAsync(
                    "/api/superadmin/subscription-plans",
                    model,
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        "Unable to create subscription plan. Please check the details.";

                    return View("SubscriptionPlans/Create", model);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<SubscriptionPlanResponse>(
                        cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                {
                    ViewBag.ErrorMessage =
                        result?.Message ?? "Subscription plan creation failed.";

                    return View("SubscriptionPlans/Create", model);
                }

                TempData["SuccessMessage"] =
                    result.Message ?? "Subscription plan created successfully.";

                return RedirectToAction(nameof(SubscriptionPlans));
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to the MTSS backend.";

                return View("SubscriptionPlans/Create", model);
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage =
                    "The backend returned an invalid response.";

                return View("SubscriptionPlans/Create", model);
            }
        }

        // Create a new society
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSociety(
            SocietyCreateRequest model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View("Societies/Create", model);
            }

            try
            {
                using var response = await _apiService.PostAsync(
                    "/api/superadmin/societies",
                    model,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        "Unable to create society. Please check the details.";

                    return View("Societies/Create", model);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<SocietyResponse>(
                        cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                {
                    ViewBag.ErrorMessage =
                        result?.Message ?? "Society creation failed.";

                    return View("Societies/Create", model);
                }

                TempData["SuccessMessage"] =
                    result.Message ?? "Society created successfully.";

                return RedirectToAction(nameof(Societies));
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to the MTSS backend.";

                return View("Societies/Create", model);
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage =
                    "The backend returned an invalid response.";

                return View("Societies/Create", model);
            }
        }

        // Activate or deactivate an existing society
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSocietyStatus(
            int id,
            bool isActive,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                using var response = await _apiService.PatchAsync(
                    $"/api/superadmin/societies/{id}/status",
                    new { isActive },
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Unable to update society status.";

                    return RedirectToAction(
                        nameof(SocietyDetails), new { id });
                }

                TempData["SuccessMessage"] = isActive
                    ? "Society activated successfully."
                    : "Society deactivated successfully.";

                return RedirectToAction(
                    nameof(SocietyDetails), new { id });
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "Unable to connect to the MTSS backend.";

                return RedirectToAction(
                    nameof(SocietyDetails), new { id });
            }
        }

        // Activate or deactivate an existing subscription plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSubscriptionPlanStatus(
            int id,
            bool isActive,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                using var response = await _apiService.PatchAsync(
                    $"/api/superadmin/subscription-plans/{id}/status",
                    new { isActive },
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Auth");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] =
                        "Subscription plan not found.";

                    return RedirectToAction(nameof(SubscriptionPlans));
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Unable to update subscription plan status.";

                    return RedirectToAction(
                        nameof(SubscriptionPlanDetails),
                        new { id });
                }

                TempData["SuccessMessage"] = isActive
                    ? "Subscription plan activated successfully."
                    : "Subscription plan deactivated successfully.";

                return RedirectToAction(
                    nameof(SubscriptionPlanDetails),
                    new { id });
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "Unable to connect to the MTSS backend.";

                return RedirectToAction(
                    nameof(SubscriptionPlanDetails),
                    new { id });
            }
        }



        // SuperAdmin Reports - reuses the existing societies and plans APIs.

        [HttpGet]
        public async Task<IActionResult> Reports(
            CancellationToken cancellationToken)
        {
            var societies = new List<SocietyListItem>();
            var plans = new List<SubscriptionPlanListItem>();

            ViewBag.TotalSocieties = 0;
            ViewBag.ActiveSocieties = 0;
            ViewBag.InactiveSocieties = 0;
            ViewBag.TotalPlans = 0;
            ViewBag.ActivePlans = 0;
            ViewBag.InactivePlans = 0;

            try
            {
                // Fetch societies
                // Fetch societies
                var backendCookie = _sessionService.GetBackendCookie();

                Console.WriteLine(
                    $"Reports - Backend cookie available: {!string.IsNullOrWhiteSpace(backendCookie)}");

                using var societyResponse = await _apiService.GetAsync(
                    "/api/superadmin/societies",
                    cancellationToken);

                Console.WriteLine(
                    $"Reports - Society API Status: {(int)societyResponse.StatusCode}");

                Console.WriteLine(
                    $"Reports - Society API Status: {(int)societyResponse.StatusCode}");

                if (societyResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (!societyResponse.IsSuccessStatusCode)
                {
                    var errorBody = await societyResponse.Content.ReadAsStringAsync(
                        cancellationToken);

                    Console.WriteLine(
                        $"Reports - Society API Error: {errorBody}");

                    ViewBag.ErrorMessage =
                        $"Society API failed with status {(int)societyResponse.StatusCode}: {errorBody}";

                    return View("Reports/Index");
                }

                var societyResult =
                    await societyResponse.Content.ReadFromJsonAsync<SocietyListResponse>(
                        cancellationToken: cancellationToken);

                if (societyResult == null || !societyResult.Success)
                {
                    ViewBag.ErrorMessage =
                        societyResult?.Message ?? "Unable to load society registration data.";

                    return View("Reports/Index");
                }

                societies = societyResult.Data ?? new List<SocietyListItem>();

                // Fetch subscription plans
                using var planResponse = await _apiService.GetAsync(
                    "/api/superadmin/subscription-plans",
                    cancellationToken);

                Console.WriteLine(
                    $"Reports - Subscription Plan API Status: {(int)planResponse.StatusCode}");

                if (planResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (!planResponse.IsSuccessStatusCode)
                {
                    var errorBody = await planResponse.Content.ReadAsStringAsync(
                        cancellationToken);

                    Console.WriteLine(
                        $"Reports - Subscription Plan API Error: {errorBody}");

                    ViewBag.Societies = societies;

                    ViewBag.ErrorMessage =
                        $"Subscription Plan API failed with status {(int)planResponse.StatusCode}: {errorBody}";

                    return View("Reports/Index");
                }

                var planResult =
                    await planResponse.Content.ReadFromJsonAsync<SubscriptionPlanListResponse>(
                        cancellationToken: cancellationToken);

                if (planResult == null || !planResult.Success)
                {
                    ViewBag.ErrorMessage =
                        planResult?.Message ?? "Unable to load subscription plan data.";

                    ViewBag.Societies = societies;
                    return View("Reports/Index");
                }

                plans = planResult.Data ?? new List<SubscriptionPlanListItem>();

                // Calculate report statistics
                ViewBag.Societies = societies;
                ViewBag.Plans = plans;

                ViewBag.TotalSocieties = societies.Count;
                ViewBag.ActiveSocieties = societies.Count(s => s.IsActive);
                ViewBag.InactiveSocieties = societies.Count(s => !s.IsActive);

                ViewBag.TotalPlans = plans.Count;
                ViewBag.ActivePlans = plans.Count(p => p.IsActive);
                ViewBag.InactivePlans = plans.Count(p => !p.IsActive);

                ViewBag.RecentSocieties = societies
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(10)
                    .ToList();

                return View("Reports/Index");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Reports HTTP Error: {ex.Message}");

                ViewBag.ErrorMessage =
                    "Unable to connect to the MTSS backend.";

                return View("Reports/Index");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Reports JSON Error: {ex.Message}");

                ViewBag.ErrorMessage =
                    "The backend returned an invalid report response.";

                return View("Reports/Index");
            }
        }


        // Static SuperAdmin Payments page. No Payments API is documented yet.
        [HttpGet]
        public IActionResult Payments()
        {
            return View("Payments/Index");
        }
        // SuperAdmin Settings page
        [HttpGet]
        public IActionResult Settings()
        {
            return View("Settings/Index");
        }
        // Display societies for Society Subscription Management
        [HttpGet]
        public async Task<IActionResult> SocietySubscriptions(
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _apiService.GetAsync(
                    "/api/superadmin/societies",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = "Unable to retrieve societies.";
                    return View("SocietySubscriptions/Index",
                        new List<SocietyListItem>());
                }

                var result = await response.Content.ReadFromJsonAsync<SocietyListResponse>(
                    cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                {
                    ViewBag.ErrorMessage = result?.Message ?? "Unable to load societies.";
                    return View("SocietySubscriptions/Index",
                        new List<SocietyListItem>());
                }

                return View("SocietySubscriptions/Index",
                    result.Data ?? new List<SocietyListItem>());
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "Unable to connect to the MTSS backend.";
                return View("SocietySubscriptions/Index",
                    new List<SocietyListItem>());
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage = "The backend returned an invalid society response.";
                return View("SocietySubscriptions/Index",
                    new List<SocietyListItem>());
            }
        }

        // View the subscription assigned to one society
        [HttpGet]
        public async Task<IActionResult> SocietySubscriptionDetails(
            int societyId,
            CancellationToken cancellationToken)
        {
            if (societyId <= 0) return BadRequest();

            try
            {
                using var societyResponse = await _apiService.GetAsync(
                    $"/api/superadmin/societies/{societyId}", cancellationToken);

                if (societyResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");
                if (!societyResponse.IsSuccessStatusCode)
                    return StatusCode((int)societyResponse.StatusCode,
                        "Unable to retrieve society details.");

                var societyResult = await societyResponse.Content.ReadFromJsonAsync<SocietyResponse>(
                    cancellationToken: cancellationToken);
                if (societyResult == null || !societyResult.Success)
                    return NotFound(societyResult?.Message ?? "Society not found.");

                var society = societyResult.Data.Deserialize<SocietyListItem>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (society == null) return NotFound("Society details were not available.");

                using var subscriptionResponse = await _apiService.GetAsync(
                    $"/api/superadmin/societies/{societyId}/subscription",
                    cancellationToken);

                if (subscriptionResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (subscriptionResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    ViewBag.SubscriptionData = (JsonElement?)null;
                    return View("SocietySubscriptions/Details", society);
                }

                if (!subscriptionResponse.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Unable to retrieve the society subscription.";
                    ViewBag.SubscriptionData = (JsonElement?)null;
                    return View("SocietySubscriptions/Details", society);
                }

                var subscriptionResult =
                    await subscriptionResponse.Content.ReadFromJsonAsync<SocietySubscriptionResponse>(
                        cancellationToken: cancellationToken);

                if (subscriptionResult == null || !subscriptionResult.Success ||
                    subscriptionResult.Data.ValueKind == JsonValueKind.Null ||
                    subscriptionResult.Data.ValueKind == JsonValueKind.Undefined)
                {
                    ViewBag.SubscriptionData = (JsonElement?)null;
                }
                else
                {
                    var data = subscriptionResult.Data;
                    if (data.ValueKind == JsonValueKind.Array)
                        data = data.GetArrayLength() > 0
                            ? data[0]
                            : default;

                    ViewBag.SubscriptionData =
                        data.ValueKind == JsonValueKind.Undefined
                            ? (JsonElement?)null
                            : data;
                }

                return View("SocietySubscriptions/Details", society);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "Unable to connect to the MTSS backend.");
            }
            catch (JsonException)
            {
                return StatusCode(502, "The backend returned an invalid subscription response.");
            }
        }

        // Open the assign subscription form
        [HttpGet]
        public async Task<IActionResult> AssignSocietySubscription(
            int societyId,
            CancellationToken cancellationToken)
        {
            if (societyId <= 0) return BadRequest();

            try
            {
                using var societyResponse = await _apiService.GetAsync(
                    $"/api/superadmin/societies/{societyId}", cancellationToken);
                if (societyResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");
                if (!societyResponse.IsSuccessStatusCode)
                    return StatusCode((int)societyResponse.StatusCode, "Unable to load society.");

                var societyResult = await societyResponse.Content.ReadFromJsonAsync<SocietyResponse>(
                    cancellationToken: cancellationToken);
                var society = societyResult?.Data.Deserialize<SocietyListItem>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (society == null) return NotFound("Society not found.");

                using var plansResponse = await _apiService.GetAsync(
                    "/api/superadmin/subscription-plans", cancellationToken);
                if (plansResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");
                if (!plansResponse.IsSuccessStatusCode)
                    return StatusCode((int)plansResponse.StatusCode, "Unable to load subscription plans.");

                var plansResult = await plansResponse.Content.ReadFromJsonAsync<SubscriptionPlanListResponse>(
                    cancellationToken: cancellationToken);

                ViewBag.Society = society;
                ViewBag.SubscriptionPlans = plansResult?.Data
                    ?? new List<SubscriptionPlanListItem>();

                return View("SocietySubscriptions/Assign",
                    new SocietySubscriptionRequest
                    {
                        StartDate = DateTime.Today,
                        EndDate = DateTime.Today.AddYears(1)
                    });
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "Unable to connect to the MTSS backend.");
            }
            catch (JsonException)
            {
                return StatusCode(502, "The backend returned an invalid response.");
            }
        }

        // Assign a subscription plan to a society
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSocietySubscription(
            int societyId,
            SocietySubscriptionRequest model,
            CancellationToken cancellationToken)
        {
            if (societyId <= 0) return BadRequest();

            if (model.EndDate <= model.StartDate)
                ModelState.AddModelError(nameof(model.EndDate),
                    "End date must be later than the start date.");

            if (!ModelState.IsValid)
            {
                return await ReloadAssignView(societyId, model, cancellationToken);
            }

            try
            {
                using var response = await _apiService.PostAsync(
                    $"/api/superadmin/societies/{societyId}/subscription",
                    model, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                var result = await response.Content.ReadFromJsonAsync<SocietySubscriptionResponse>(
                    cancellationToken: cancellationToken);

                if (!response.IsSuccessStatusCode || result == null || !result.Success)
                {
                    ModelState.AddModelError(string.Empty,
                        result?.Message ?? "Unable to assign subscription.");
                    return await ReloadAssignView(societyId, model, cancellationToken);
                }

                TempData["SuccessMessage"] =
                    result.Message ?? "Society subscription assigned successfully.";
                return RedirectToAction(nameof(SocietySubscriptionDetails), new { societyId });
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Unable to connect to the MTSS backend.");
                return await ReloadAssignView(societyId, model, cancellationToken);
            }
            catch (JsonException)
            {
                ModelState.AddModelError(string.Empty, "The backend returned an invalid response.");
                return await ReloadAssignView(societyId, model, cancellationToken);
            }
        }

        // Load the existing subscription into the update form
        [HttpGet]
        public async Task<IActionResult> EditSocietySubscription(
            int societyId,
            CancellationToken cancellationToken)
        {
            if (societyId <= 0) return BadRequest();

            try
            {
                using var response = await _apiService.GetAsync(
                    $"/api/superadmin/societies/{societyId}/subscription",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");
                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode,
                        "Unable to retrieve the existing subscription.");

                var result = await response.Content.ReadFromJsonAsync<SocietySubscriptionResponse>(
                    cancellationToken: cancellationToken);
                if (result == null || !result.Success)
                    return NotFound(result?.Message ?? "Subscription not found.");

                var data = result.Data;
                if (data.ValueKind == JsonValueKind.Array)
                    data = data.GetArrayLength() > 0 ? data[0] : default;
                if (data.ValueKind != JsonValueKind.Object)
                    return NotFound("No subscription is assigned to this society.");

                var model = data.Deserialize<SocietySubscriptionRequest>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (model == null) return NotFound("Subscription data was not available.");

                return await ReloadAssignView(societyId, model, cancellationToken,
                    "SocietySubscriptions/Edit");
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "Unable to connect to the MTSS backend.");
            }
            catch (JsonException)
            {
                return StatusCode(502, "The backend returned an invalid subscription response.");
            }
        }

        // Update the assigned subscription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSocietySubscription(
            int societyId,
            SocietySubscriptionRequest model,
            CancellationToken cancellationToken)
        {
            if (societyId <= 0) return BadRequest();

            if (model.EndDate <= model.StartDate)
                ModelState.AddModelError(nameof(model.EndDate),
                    "End date must be later than the start date.");

            if (!ModelState.IsValid)
                return await ReloadAssignView(societyId, model, cancellationToken,
                    "SocietySubscriptions/Edit");

            try
            {
                using var response = await _apiService.PutAsync(
                    $"/api/superadmin/societies/{societyId}/subscription",
                    model, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                var result = await response.Content.ReadFromJsonAsync<SocietySubscriptionResponse>(
                    cancellationToken: cancellationToken);

                if (!response.IsSuccessStatusCode || result == null || !result.Success)
                {
                    ModelState.AddModelError(string.Empty,
                        result?.Message ?? "Unable to update subscription.");
                    return await ReloadAssignView(societyId, model, cancellationToken,
                        "SocietySubscriptions/Edit");
                }

                TempData["SuccessMessage"] =
                    result.Message ?? "Society subscription updated successfully.";
                return RedirectToAction(nameof(SocietySubscriptionDetails), new { societyId });
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Unable to connect to the MTSS backend.");
                return await ReloadAssignView(societyId, model, cancellationToken,
                    "SocietySubscriptions/Edit");
            }
            catch (JsonException)
            {
                ModelState.AddModelError(string.Empty, "The backend returned an invalid response.");
                return await ReloadAssignView(societyId, model, cancellationToken,
                    "SocietySubscriptions/Edit");
            }
        }

        // Change society subscription status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSocietySubscriptionStatus(
            int societyId,
            SocietySubscriptionStatusRequest model,
            CancellationToken cancellationToken)
        {
            if (societyId <= 0) return BadRequest();

            try
            {
                using var response = await _apiService.PatchAsync(
                    $"/api/superadmin/societies/{societyId}/subscription/status",
                    model, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (!response.IsSuccessStatusCode)
                    TempData["ErrorMessage"] = "Unable to update society subscription status.";
                else
                    TempData["SuccessMessage"] = "Society subscription status updated successfully.";

                return RedirectToAction(nameof(SocietySubscriptionDetails), new { societyId });
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "Unable to connect to the MTSS backend.";
                return RedirectToAction(nameof(SocietySubscriptionDetails), new { societyId });
            }
        }

        // ==========================================
        // SOCIETY ADMIN MANAGEMENT
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> SocietyAdmins(
            int? societyId,
            CancellationToken cancellationToken)
        {
            var societies = new List<SocietyListItem>();
            ViewBag.Societies = societies;
            ViewBag.Admins = new List<JsonElement>();
            ViewBag.SelectedSocietyId = societyId;

            try
            {
                using var societyResponse = await _apiService.GetAsync(
                    "/api/superadmin/societies",
                    cancellationToken);

                if (societyResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (!societyResponse.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = "Unable to retrieve societies.";
                    return View("SocietyAdmins/Index");
                }

                var societyResult =
                    await societyResponse.Content.ReadFromJsonAsync<SocietyListResponse>(
                        cancellationToken: cancellationToken);

                if (societyResult == null || !societyResult.Success)
                {
                    ViewBag.ErrorMessage =
                        societyResult?.Message ?? "Unable to load societies.";
                    return View("SocietyAdmins/Index");
                }

                societies = societyResult.Data ?? new List<SocietyListItem>();
                ViewBag.Societies = societies;

                if (societyId.HasValue && societyId.Value > 0)
                {
                    using var adminResponse = await _apiService.GetAsync(
                        $"/api/superadmin/societies/{societyId.Value}/admins",
                        cancellationToken);

                    if (adminResponse.StatusCode == HttpStatusCode.Unauthorized)
                        return RedirectToAction("Login", "Auth");

                    if (!adminResponse.IsSuccessStatusCode)
                    {
                        ViewBag.ErrorMessage = "Unable to retrieve Society Admins.";
                        return View("SocietyAdmins/Index");
                    }

                    var adminResult =
                        await adminResponse.Content.ReadFromJsonAsync<SocietyAdminApiResponse>(
                            cancellationToken: cancellationToken);

                    if (adminResult == null || !adminResult.Success)
                    {
                        ViewBag.ErrorMessage =
                            adminResult?.Message ?? "Unable to load Society Admins.";
                    }
                    else if (adminResult.Data.ValueKind == JsonValueKind.Array)
                    {
                        ViewBag.Admins = adminResult.Data.EnumerateArray().ToList();
                    }
                    else if (adminResult.Data.ValueKind == JsonValueKind.Object)
                    {
                        ViewBag.Admins = new List<JsonElement> { adminResult.Data };
                    }
                }

                return View("SocietyAdmins/Index");
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "Unable to connect to the MTSS backend.";
                return View("SocietyAdmins/Index");
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage = "The backend returned an invalid response.";
                return View("SocietyAdmins/Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateSocietyAdmin(
            int societyId,
            CancellationToken cancellationToken)
        {
            if (societyId <= 0)
                return BadRequest();

            try
            {
                using var response = await _apiService.GetAsync(
                    $"/api/superadmin/societies/{societyId}",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Unable to load society.");

                var result = await response.Content.ReadFromJsonAsync<SocietyResponse>(
                    cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                    return NotFound(result?.Message ?? "Society not found.");

                var society = result.Data.Deserialize<SocietyListItem>(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (society == null)
                    return NotFound("Society details were not available.");

                ViewBag.Society = society;
                return View("SocietyAdmins/Create", new SocietyAdminCreateRequest());
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "Unable to connect to the MTSS backend.");
            }
            catch (JsonException)
            {
                return StatusCode(502, "The backend returned an invalid society response.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSocietyAdmin(
            int societyId,
            SocietyAdminCreateRequest model,
            CancellationToken cancellationToken)
        {
            if (societyId <= 0)
                return BadRequest();

            if (!ModelState.IsValid)
                return await ReloadSocietyAdminCreateView(
                    societyId, model, cancellationToken);

            try
            {
                using var response = await _apiService.PostAsync(
                    $"/api/superadmin/societies/{societyId}/admins",
                    model,
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                var result =
                    await response.Content.ReadFromJsonAsync<SocietyAdminApiResponse>(
                        cancellationToken: cancellationToken);

                if (!response.IsSuccessStatusCode || result == null || !result.Success)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        result?.Message ?? "Unable to create Society Admin.");

                    return await ReloadSocietyAdminCreateView(
                        societyId, model, cancellationToken);
                }

                TempData["SuccessMessage"] =
                    result.Message ?? "Society Admin created successfully.";

                return RedirectToAction(nameof(SocietyAdmins), new { societyId });
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Unable to connect to the MTSS backend.");
                return await ReloadSocietyAdminCreateView(
                    societyId, model, cancellationToken);
            }
            catch (JsonException)
            {
                ModelState.AddModelError(string.Empty, "The backend returned an invalid response.");
                return await ReloadSocietyAdminCreateView(
                    societyId, model, cancellationToken);
            }
        }

        [HttpGet]
        public async Task<IActionResult> SocietyAdminDetails(
            int societyId,
            string userId,
            CancellationToken cancellationToken)
        {
            if (societyId <= 0 || string.IsNullOrWhiteSpace(userId))
                return BadRequest();

            try
            {
                using var response = await _apiService.GetAsync(
                    $"/api/superadmin/societies/{societyId}/admins/{Uri.EscapeDataString(userId)}",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound("Society Admin not found.");

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode,
                        "Unable to retrieve Society Admin details.");

                var result =
                    await response.Content.ReadFromJsonAsync<SocietyAdminApiResponse>(
                        cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                    return NotFound(result?.Message ?? "Society Admin not found.");

                var adminData = NormalizeSocietyAdminData(result.Data);
                if (adminData.ValueKind != JsonValueKind.Object)
                    return NotFound("Society Admin details were not available.");

                ViewBag.SocietyId = societyId;
                ViewBag.UserId = userId;
                return View("SocietyAdmins/Details", adminData);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "Unable to connect to the MTSS backend.");
            }
            catch (JsonException)
            {
                return StatusCode(502, "The backend returned an invalid admin response.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditSocietyAdmin(
            int societyId,
            string userId,
            CancellationToken cancellationToken)
        {
            if (societyId <= 0 || string.IsNullOrWhiteSpace(userId))
                return BadRequest();

            try
            {
                using var response = await _apiService.GetAsync(
                    $"/api/superadmin/societies/{societyId}/admins/{Uri.EscapeDataString(userId)}",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode,
                        "Unable to retrieve Society Admin.");

                var result =
                    await response.Content.ReadFromJsonAsync<SocietyAdminApiResponse>(
                        cancellationToken: cancellationToken);

                if (result == null || !result.Success)
                    return NotFound(result?.Message ?? "Society Admin not found.");

                var adminData = NormalizeSocietyAdminData(result.Data);
                if (adminData.ValueKind != JsonValueKind.Object)
                    return NotFound("Society Admin details were not available.");

                var model = new SocietyAdminUpdateRequest
                {
                    FullName = GetJsonString(adminData, "fullName", "FullName"),
                    Email = GetJsonString(adminData, "email", "Email")
                };

                ViewBag.SocietyId = societyId;
                ViewBag.UserId = userId;
                return View("SocietyAdmins/Edit", model);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "Unable to connect to the MTSS backend.");
            }
            catch (JsonException)
            {
                return StatusCode(502, "The backend returned an invalid admin response.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSocietyAdmin(
            int societyId,
            string userId,
            SocietyAdminUpdateRequest model,
            CancellationToken cancellationToken)
        {
            if (societyId <= 0 || string.IsNullOrWhiteSpace(userId))
                return BadRequest();

            ViewBag.SocietyId = societyId;
            ViewBag.UserId = userId;

            if (!ModelState.IsValid)
                return View("SocietyAdmins/Edit", model);

            try
            {
                using var response = await _apiService.PutAsync(
                    $"/api/superadmin/societies/{societyId}/admins/{Uri.EscapeDataString(userId)}",
                    model,
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                    ViewBag.ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                        ? "Unable to update Society Admin."
                        : errorMessage;

                    return View("SocietyAdmins/Edit", model);
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    using var document = JsonDocument.Parse(responseBody);
                    if (document.RootElement.ValueKind == JsonValueKind.Object &&
                        document.RootElement.TryGetProperty("success", out var success) &&
                        success.ValueKind == JsonValueKind.False)
                    {
                        ViewBag.ErrorMessage = GetJsonString(
                            document.RootElement, "message");
                        return View("SocietyAdmins/Edit", model);
                    }
                }

                TempData["SuccessMessage"] = "Society Admin updated successfully.";
                return RedirectToAction(
                    nameof(SocietyAdminDetails), new { societyId, userId });
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "Unable to connect to the MTSS backend.";
                return View("SocietyAdmins/Edit", model);
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage = "The backend returned an invalid response.";
                return View("SocietyAdmins/Edit", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSocietyAdminStatus(
            int societyId,
            string userId,
            bool isActive,
            CancellationToken cancellationToken)
        {
            if (societyId <= 0 || string.IsNullOrWhiteSpace(userId))
                return BadRequest();

            try
            {
                using var response = await _apiService.PatchAsync(
                    $"/api/superadmin/societies/{societyId}/admins/{Uri.EscapeDataString(userId)}/status",
                    new SocietyAdminStatusRequest { IsActive = isActive },
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(errorBody)
                        ? "Unable to update Society Admin status."
                        : errorBody;
                }
                else
                {
                    TempData["SuccessMessage"] = isActive
                        ? "Society Admin activated successfully."
                        : "Society Admin deactivated successfully.";
                }

                return RedirectToAction(
                    nameof(SocietyAdminDetails), new { societyId, userId });
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "Unable to connect to the MTSS backend.";
                return RedirectToAction(
                    nameof(SocietyAdminDetails), new { societyId, userId });
            }
        }

        private async Task<IActionResult> ReloadSocietyAdminCreateView(
            int societyId,
            SocietyAdminCreateRequest model,
            CancellationToken cancellationToken)
        {
            using var response = await _apiService.GetAsync(
                $"/api/superadmin/societies/{societyId}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return RedirectToAction("Login", "Auth");

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Unable to reload society.");

            var result = await response.Content.ReadFromJsonAsync<SocietyResponse>(
                cancellationToken: cancellationToken);

            if (result == null || !result.Success)
                return NotFound(result?.Message ?? "Society not found.");

            ViewBag.Society = result.Data.Deserialize<SocietyListItem>(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return View("SocietyAdmins/Create", model);
        }

        private static JsonElement NormalizeSocietyAdminData(JsonElement data)
        {
            if (data.ValueKind == JsonValueKind.Array)
                return data.GetArrayLength() > 0 ? data[0] : default;

            return data;
        }

        private static string GetJsonString(
            JsonElement element,
            params string[] propertyNames)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return string.Empty;

            foreach (var property in element.EnumerateObject())
            {
                if (propertyNames.Any(name =>
                    string.Equals(name, property.Name,
                        StringComparison.OrdinalIgnoreCase)))
                {

                    return property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString() ?? string.Empty
                        : property.Value.ToString();
                }
            }

            return string.Empty;
        }

        private async Task<IActionResult> ReloadAssignView(
            int societyId,
            SocietySubscriptionRequest model,
            CancellationToken cancellationToken,
            string viewName = "SocietySubscriptions/Assign")
        {
            using var societyResponse = await _apiService.GetAsync(
                $"/api/superadmin/societies/{societyId}", cancellationToken);
            if (!societyResponse.IsSuccessStatusCode)
                return StatusCode((int)societyResponse.StatusCode, "Unable to reload society.");

            var societyResult = await societyResponse.Content.ReadFromJsonAsync<SocietyResponse>(
                cancellationToken: cancellationToken);
            ViewBag.Society = societyResult?.Data.Deserialize<SocietyListItem>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            using var plansResponse = await _apiService.GetAsync(
                "/api/superadmin/subscription-plans", cancellationToken);
            if (!plansResponse.IsSuccessStatusCode)
                return StatusCode((int)plansResponse.StatusCode, "Unable to reload subscription plans.");

            var plansResult = await plansResponse.Content.ReadFromJsonAsync<SubscriptionPlanListResponse>(
                cancellationToken: cancellationToken);
            ViewBag.SubscriptionPlans = plansResult?.Data
                ?? new List<SubscriptionPlanListItem>();

            return View(viewName, model);
        }


    }
}
