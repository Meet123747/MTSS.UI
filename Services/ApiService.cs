
using System.Net.Http.Json;
using System.Linq;

namespace MTSS.UI.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly SessionService _sessionService;

        private const string AuthCookieName =
            ".AspNetCore.Identity.Application";

        public ApiService(
            HttpClient httpClient,
            SessionService sessionService)
        {
            _httpClient = httpClient;
            _sessionService = sessionService;
        }

        // Attach backend authentication cookie to outgoing requests.
        private void AddAuthenticationCookie(
            HttpRequestMessage request)
        {
            var cookie = _sessionService.GetBackendCookie();

            if (string.IsNullOrWhiteSpace(cookie))
            {
                Console.WriteLine("Backend cookie: MISSING");
                return;
            }

            var cookieNames = cookie
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Select(part =>
                {
                    var separator = part.IndexOf('=');

                    return separator > 0
                        ? part[..separator]
                        : "Unknown";
                });

            Console.WriteLine(
                $"Backend cookie names: {string.Join(", ", cookieNames)}");

            var added = request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            Console.WriteLine(
                $"Cookie header attached: {added}");
        }

        // Capture backend authentication cookie after login.
        public bool CaptureAuthenticationCookie(
            HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues(
                "Set-Cookie", out var cookieHeaders))
            {
                Console.WriteLine("No Set-Cookie header received.");
                return false;
            }

            var cookies = new Dictionary<string, string>(
                StringComparer.Ordinal);

            foreach (var header in cookieHeaders)
            {
                var cookiePair = header.Split(';')[0].Trim();

                var separatorIndex = cookiePair.IndexOf('=');

                if (separatorIndex <= 0)
                    continue;

                var cookieName = cookiePair[..separatorIndex];

                bool isMainCookie = string.Equals(
                    cookieName,
                    AuthCookieName,
                    StringComparison.Ordinal);

                bool isChunkCookie =
                    cookieName.StartsWith(
                        AuthCookieName + "C",
                        StringComparison.Ordinal) &&
                    cookieName[(AuthCookieName.Length + 1)..]
                        .All(char.IsDigit);

                if (isMainCookie || isChunkCookie)
                {
                    cookies[cookieName] = cookiePair;
                }
            }

            if (!cookies.ContainsKey(AuthCookieName))
            {
                Console.WriteLine(
                    "Main Identity cookie was not found.");

                return false;
            }

            var cookieHeader = string.Join(
                "; ",
                cookies
                    .OrderBy(c =>
                        c.Key == AuthCookieName ? 0 : 1)
                    .ThenBy(c => c.Key)
                    .Select(c => c.Value));

            _sessionService.SetBackendCookie(cookieHeader);

            Console.WriteLine(
                $"Identity cookies captured: {cookies.Count}");

            return true;
        }

        // GET
        public async Task<HttpResponseMessage> GetAsync(
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get, endpoint);

            AddAuthenticationCookie(request);

            return await _httpClient.SendAsync(
                request, cancellationToken);
        }

        // POST
        public async Task<HttpResponseMessage> PostAsync<TRequest>(
            string endpoint,
            TRequest data,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(data)
            };

            AddAuthenticationCookie(request);

            return await _httpClient.SendAsync(
                request, cancellationToken);
        }

        // PUT
        public async Task<HttpResponseMessage> PutAsync<TRequest>(
            string endpoint,
            TRequest data,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Put, endpoint)
            {
                Content = JsonContent.Create(data)
            };

            AddAuthenticationCookie(request);

            return await _httpClient.SendAsync(
                request, cancellationToken);
        }

        // PATCH with request body
        public async Task<HttpResponseMessage> PatchAsync<TRequest>(
            string endpoint,
            TRequest data,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Patch, endpoint)
            {
                Content = JsonContent.Create(data)
            };

            AddAuthenticationCookie(request);

            return await _httpClient.SendAsync(
                request, cancellationToken);
        }

        // DELETE
        public async Task<HttpResponseMessage> DeleteAsync(
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Delete, endpoint);

            AddAuthenticationCookie(request);

            return await _httpClient.SendAsync(
                request, cancellationToken);
        }

        // PATCH without a request body
        public async Task<HttpResponseMessage> PatchNoContentAsync(
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Patch, endpoint);

            AddAuthenticationCookie(request);

            return await _httpClient.SendAsync(
                request, cancellationToken);
        }
    }
}
