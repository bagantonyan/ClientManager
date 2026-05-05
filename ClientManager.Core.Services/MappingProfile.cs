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
            CreateMap<Client, ClientDto>()
                .ForMember(d => d.Founders,
                    opt => opt.MapFrom(s => s.ClientFounders!.Select(cf => cf.Founder)));

            CreateMap<Founder, FounderDto>();

            CreateMap<ClientForCreationDto, Client>()
                .ForMember(d => d.ClientFounders,
                    opt => opt.MapFrom(s => s.Founders));

            CreateMap<FounderForCreationDto, Founder>();

            CreateMap<FounderForCreationDto, ClientFounder>()
                .ForMember(cf => cf.Founder, opt => opt.MapFrom(src => src));
        }
    }
}