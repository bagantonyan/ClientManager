using Shared.DataTransferObjects.Founders;
using Shared.Enums;

namespace Shared.DataTransferObjects.Clients
{
    public record ClientForCreationDto
    {
        public string? INN { get; init; }
        public string? Name { get; init; }
        public ClientType ClientType { get; init; }
        public IEnumerable<FounderForCreationDto>? Founders { get; init; }
    }
}