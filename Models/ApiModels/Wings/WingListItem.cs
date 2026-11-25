
using System;
using System.Text.Json.Serialization;

namespace MTSS.UI.Models.ApiModels.Wings
{
    public class WingListItem
    {
        [JsonPropertyName("wingId")]
        public int WingId { get; set; }

        [JsonPropertyName("societyId")]
        public int SocietyId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }
    }
}
