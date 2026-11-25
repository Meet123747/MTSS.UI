
namespace MTSS.UI.Models.ApiModels.SubscriptionPlans
{
    public class SubscriptionPlanListResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public List<SubscriptionPlanListItem> Data { get; set; } = new();
    }
}
