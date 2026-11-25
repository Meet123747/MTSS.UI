
namespace MTSS.UI.Models.ApiModels.SocietySubscriptions
{
    public class SocietySubscriptionRequest
    {
        public int SubscriptionPlanId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
