
using System.Text.Json;

namespace MTSS.UI.Models.ApiModels.SocietySubscriptions
{
    public class SocietySubscriptionResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public JsonElement Data { get; set; }
    }
}
