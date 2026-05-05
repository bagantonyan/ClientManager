using Shared.DataTransferObjects.Founders;
using Shared.Enums;

namespace Shared.DataTransferObjects.Clients
{
    public record ClientDto
    {
        public Guid Id { get; init; }
        public string? INN { get; init; }
        public string? Name { get; init; }
        public ClientType ClientType { get; init; }
        public IEnumerable<FounderDto>? Founders { get; init; }
    }
}
