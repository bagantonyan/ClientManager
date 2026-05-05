namespace Shared.DataTransferObjects.Founders
{
    public record FounderDto
    {
        public Guid Id { get; init; }
        public string? INN { get; init; }
        public string? FullName { get; init; }
    }
}