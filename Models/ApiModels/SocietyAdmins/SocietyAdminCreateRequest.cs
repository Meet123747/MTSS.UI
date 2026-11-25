
namespace MTSS.UI.Models.ApiModels.SocietyAdmins
{
    public class SocietyAdminCreateRequest
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
