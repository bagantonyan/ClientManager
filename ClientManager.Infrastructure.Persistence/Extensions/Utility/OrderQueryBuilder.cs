using System.Text;

namespace ClientManager.Infrastructure.Persistence.Extensions.Utility
{
    public static class OrderQueryBuilder
    {
        public static string CreateOrderQuery(
            string orderByQueryString,
            IReadOnlyCollection<string> allowedFields)
        {
            var orderQueryBuilder = new StringBuilder();

            foreach (var raw in orderByQueryString.Trim().Split(','))
            {
                var param = raw.Trim();
                if (string.IsNullOrEmpty(param))
                    continue;

                var requestedName = param.Split(' ')[0];
                var canonical = allowedFields
                    .FirstOrDefault(f => string.Equals(f, requestedName, StringComparison.OrdinalIgnoreCase));
                if (canonical is null)
                    continue;

                var direction = param.EndsWith(" desc", StringComparison.OrdinalIgnoreCase)
                    ? "descending"
                    : "ascending";

                orderQueryBuilder.Append($"{canonical} {direction}, ");
            }

            return orderQueryBuilder.ToString().TrimEnd(',', ' ');
        }
    }
}