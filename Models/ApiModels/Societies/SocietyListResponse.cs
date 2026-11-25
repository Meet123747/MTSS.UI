
using System.Text.Json;

namespace MTSS.UI.Models.ApiModels.Societies
{
    public class SocietyListResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public List<SocietyListItem> Data { get; set; } = new();
    }
}
