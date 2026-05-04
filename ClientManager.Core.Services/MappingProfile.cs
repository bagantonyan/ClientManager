using AutoMapper;
using ClientManager.Core.Domain.Entities;
using Shared.DataTransferObjects.Clients;

namespace ClientManager.Core.Services
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Client, ClientDto>();
        }
    }
}
