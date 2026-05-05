namespace Shared.DataTransferObjects.Founders
{
    public record FounderForCreationDto
    {
        public string? INN { get; init; }
        public string? FullName { get; init; }
    }
}