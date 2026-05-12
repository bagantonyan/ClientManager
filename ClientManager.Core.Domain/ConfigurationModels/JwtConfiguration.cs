using System.Text.Json.Serialization;

namespace ClientManager.Core.Domain.ConfigurationModels
{
    public class JwtConfiguration
    {
        public string Section { get; set; } = "JwtSettings";
        public string? ValidIssuer { get; set; }
        public string? ValidAudience { get; set; }
        public string? Expires { get; set; }

        [JsonIgnore]
        public string? Secret { get; set; }
    }
}