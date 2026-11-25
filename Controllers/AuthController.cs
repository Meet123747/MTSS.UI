
using Microsoft.AspNetCore.Mvc;
using MTSS.UI.Models.ApiModels.Auth;
using MTSS.UI.Models.ViewModels.Auth;
using MTSS.UI.Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace MTSS.UI.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApiService _apiService;

        public AuthController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var request = new LoginRequest
            {
                Email = model.Email,
                Password = model.Password
            };

            try
            {
                // Step 1: Clear previous session data.
                HttpContext.Session.Clear();

                // Step 2: Send login request to backend.
                using var response = await _apiService.PostAsync(
                    "/api/auth/login",
                    request,
                    cancellationToken);

                Console.WriteLine(
                    $"LOGIN HTTP STATUS: {(int)response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Invalid email or password.");

                    return View(model);
                }

                // Step 3: Read backend login response.
                var result = await response.Content
                    .ReadFromJsonAsync<LoginResponse>(
                        cancellationToken: cancellationToken);

                if (result == null ||
                    !result.Success ||
                    result.Data == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        result?.Message ?? "Login failed.");

                    return View(model);
                }

                // Step 4: Identify the user's role.
                var roles = result.Data.Roles ?? new List<string>();

                // Diagnostic information.
                // Never log passwords or authentication cookie values.
                Console.WriteLine(
                    $"LOGIN SUCCESS: {result.Success}");

                Console.WriteLine(
                    $"LOGIN ROLES: {string.Join(", ", roles)}");

                var isSuperAdmin = roles.Contains(
                    "SuperAdmin",
                    StringComparer.OrdinalIgnoreCase);

                var isSocietyAdmin = roles.Contains(
                    "SocietyAdmin",
                    StringComparer.OrdinalIgnoreCase);

                var isResident = roles.Contains(
                    "Resident",
                    StringComparer.OrdinalIgnoreCase);

                if (!isSuperAdmin &&
                    !isSocietyAdmin &&
                    !isResident)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Your account does not have an authorized role.");

                    return View(model);
                }

                // Step 5: Capture backend authentication cookie.
                bool cookieCaptured =
                    _apiService.CaptureAuthenticationCookie(response);

                Console.WriteLine(
                    $"Backend authentication cookie captured: {cookieCaptured}");

                Console.WriteLine(
                    $"MVC Session ID: {HttpContext.Session.Id}");

                if (!cookieCaptured)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Authentication cookie was not received.");

                    return View(model);
                }

                // Step 6: Store user information in session.
                HttpContext.Session.SetString(
                    "UserId", result.Data.UserId);

                HttpContext.Session.SetString(
                    "FullName", result.Data.FullName);

                HttpContext.Session.SetString(
                    "Email", result.Data.Email);

                HttpContext.Session.SetString(
                    "Roles", string.Join(",", roles));

                // Step 7: Redirect according to user role.
                if (isSuperAdmin)
                {
                    return RedirectToAction(
                        "Index", "SuperAdmin");
                }

                if (isSocietyAdmin)
                {
                    return RedirectToAction(
                        "Index", "SocietyAdmin");
                }

                return RedirectToAction(
                    "Index", "Resident");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to connect to the MTSS backend.");

                return View(model);
            }
            catch (JsonException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The backend returned an invalid response.");

                return View(model);
            }
        }

        // Logout from the UI session.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(nameof(Login));
        }
    }
}
