using System.Text.Json.Serialization;

namespace Shared.DataTransferObjects.Founders
{
    public record FounderDto
    {
        public Guid Id { get; init; }
        public string? INN { get; init; }
        public string? FullName { get; init; }

        [JsonIgnore]
        public byte[]? RowVersion { get; init; }
    }
}