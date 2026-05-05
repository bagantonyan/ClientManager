using AutoMapper;
using ClientManager.Core.Domain.Entities;
using Shared.DataTransferObjects.Clients;
using Shared.DataTransferObjects.Founders;

namespace ClientManager.Core.Services
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Client, ClientDto>();
            CreateMap<Founder, FounderDto>();
        }
    }
}