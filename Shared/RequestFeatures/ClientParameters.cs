namespace Shared.RequestFeatures
{
    public class ClientParameters : RequestParameters
    {
        public ClientParameters() => OrderBy = "Name";
        public string? SearchTerm { get; set; }
    }
}