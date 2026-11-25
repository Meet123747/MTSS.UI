
namespace MTSS.UI.Models.ApiModels.Auth
{
    public class LoginResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public LoginUserData? Data { get; set; }
    }
}
