namespace ClientManager.Infrastructure.Presentation.Http
{
    public static class ETagHelper
    {
        public static string ToETag(byte[] rowVersion) =>
            $"\"{Convert.ToBase64String(rowVersion)}\"";

        public static byte[]? TryParseIfMatch(string? ifMatchHeader)
        {
            if (string.IsNullOrWhiteSpace(ifMatchHeader))
                return null;

            var value = ifMatchHeader.Trim();

            if (value.StartsWith("W/", StringComparison.Ordinal))
                return null;

            if (value.Length < 2 || value[0] != '"' || value[^1] != '"')
                return null;

            value = value[1..^1];

            try
            {
                return Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}