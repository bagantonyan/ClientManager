using Shared.DataTransferObjects.Founders;

namespace Shared.DataTransferObjects.Clients
{
    public record ClientForUpdateDto
    {
        public string? Name { get; init; }
        public IEnumerable<FounderForCreationDto>? Founders { get; init; }
    }
}