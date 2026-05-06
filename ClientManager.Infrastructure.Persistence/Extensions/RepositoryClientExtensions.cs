using ClientManager.Core.Domain.Entities;

namespace ClientManager.Infrastructure.Persistence.Extensions
{
    public static class RepositoryClientExtensions
    {
        public static IQueryable<Client> Search(this IQueryable<Client> clients, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return clients;

            var lowerCaseTerm = searchTerm.Trim().ToLower();

            return clients.Where(e => e.Name.ToLower().Contains(lowerCaseTerm) || e.INN.Contains(lowerCaseTerm));
        }
    }
}