using ClientManager.Core.Domain.Entities;
using ClientManager.Infrastructure.Persistence.Extensions.Utility;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace ClientManager.Infrastructure.Persistence.Extensions
{
    public static class RepositoryClientExtensions
    {
        public static IQueryable<Client> Search(this IQueryable<Client> clients, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return clients;

            var pattern = $"%{searchTerm.Trim()}%";

            return clients.Where(e =>
                (e.Name != null && EF.Functions.Like(e.Name, pattern)) ||
                (e.INN != null && EF.Functions.Like(e.INN, pattern)));
        }

        public static IQueryable<Client> Sort(this IQueryable<Client> clients, string orderByQueryString)
        {
            if (string.IsNullOrWhiteSpace(orderByQueryString))
                return clients.OrderBy(e => e.Name);

            var orderQuery = OrderQueryBuilder.CreateOrderQuery<Client>(orderByQueryString);

            if (string.IsNullOrWhiteSpace(orderQuery))
                return clients.OrderBy(e => e.Name);

            return clients.OrderBy(orderQuery);
        }
    }
}