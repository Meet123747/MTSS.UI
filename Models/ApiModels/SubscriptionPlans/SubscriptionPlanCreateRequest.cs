
namespace MTSS.UI.Models.ApiModels.SubscriptionPlans
{
    public class SubscriptionPlanCreateRequest
    {
        public string Name { get; set; } = string.Empty;

        public int MaxWings { get; set; }

        public int MaxFloorsPerWing { get; set; }

        public int MaxFlats { get; set; }

        public int MaxAmenities { get; set; }

        public int MaxSocietyAdmins { get; set; }

        public bool GuestApprovalEnabled { get; set; }

        public bool ParcelRegisterEnabled { get; set; }

        public bool AmenityPinEnabled { get; set; }

        public bool ComplaintManagementEnabled { get; set; }

        public bool ReportsEnabled { get; set; }
    }
}
