
using Microsoft.AspNetCore.Http;

namespace MTSS.UI.Services
{
    public class SessionService
    {
        private const string BackendCookieKey =
            "MTSS_Backend_Auth_Cookie";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionService(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetBackendCookie(string cookie)
        {
            _httpContextAccessor.HttpContext?.Session.SetString(
                BackendCookieKey, cookie);
        }

        public string? GetBackendCookie()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString(
                BackendCookieKey);
        }

        public bool HasBackendCookie()
        {
            return !string.IsNullOrWhiteSpace(GetBackendCookie());
        }

        public void ClearBackendCookie()
        {
            _httpContextAccessor.HttpContext?.Session.Remove(
                BackendCookieKey);
        }
    }
}
