using System.Text.Json;

namespace MTSS.UI.Models.ApiModels.Societies
{
    public class SocietyResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public JsonElement Data { get; set; }
    }
}