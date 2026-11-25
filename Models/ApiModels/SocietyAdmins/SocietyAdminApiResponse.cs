
using System.Text.Json;

namespace MTSS.UI.Models.ApiModels.SocietyAdmins
{
    public class SocietyAdminApiResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public JsonElement Data { get; set; }
    }
}
