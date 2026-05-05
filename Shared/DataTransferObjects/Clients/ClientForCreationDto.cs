using Shared.Enums;

namespace Shared.DataTransferObjects.Clients
{
    public record ClientForCreationDto
    {
        public string? INN { get; set; }
        public string? Name { get; set; }
        public ClientType ClientType { get; set; }
    }
}