
using System.Text.Json;

namespace MTSS.UI.Models.ApiModels.SubscriptionPlans
{
    public class SubscriptionPlanResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public JsonElement Data { get; set; }
    }
}
