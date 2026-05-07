using AutoMapper;
using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Exceptions;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services;
using ClientManager.UnitTests.TestData;
using FluentAssertions;
using LoggingService;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.DataTransferObjects.Clients;
using Shared.DataTransferObjects.Founders;
using Shared.Enums;

namespace ClientManager.UnitTests.Services
{
    public class ClientServiceTests
    {
        private readonly IRepositoryManager _repo = Substitute.For<IRepositoryManager>();
        private readonly IClientRepository _clientRepo = Substitute.For<IClientRepository>();
        private readonly IFounderRepository _founderRepo = Substitute.For<IFounderRepository>();
        private readonly ClientService _sut;

        public ClientServiceTests()
        {
            _repo.Client.Returns(_clientRepo);
            _repo.Founder.Returns(_founderRepo);

            var mapper = new MapperConfiguration(
                cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
            _sut = new ClientService(_repo, Substitute.For<ILoggerManager>(), mapper);
        }

        private static ClientForCreationDto ValidLegalEntityDto() => new()
        {
            INN = ValidInns.LegalEntity1,
            Name = "ООО Ромашка",
            ClientType = ClientType.Legal_Entity,
            Founders = new[] { new FounderForCreationDto { INN = ValidInns.Individual1, FullName = "Иванов И.И." } }
        };

        [Fact]
        public async Task Create_throws_when_active_client_with_same_inn_exists()
        {
            var dto = ValidLegalEntityDto();
            _clientRepo.GetByInnIncludingDeletedAsync(dto.INN!, true, Arg.Any<CancellationToken>())
                .Returns(new Client { INN = dto.INN!, Name = "X", DeletedDate = null });

            await FluentActions.Invoking(() => _sut.CreateClientAsync(dto))
                .Should().ThrowAsync<ClientWithSameInnExistsException>();
        }

        [Fact]
        public async Task Create_restores_soft_deleted_client_with_same_inn()
        {
            var dto = ValidLegalEntityDto();
            var existing = new Client
            {
                Id = Guid.NewGuid(),
                INN = dto.INN!,
                Name = "Old name",
                ClientType = ClientType.Legal_Entity,
                DeletedDate = DateTime.UtcNow.AddDays(-3)
            };
            _clientRepo.GetByInnIncludingDeletedAsync(dto.INN!, true, Arg.Any<CancellationToken>())
                .Returns(existing);
            _founderRepo.GetByInnIncludingDeletedAsync(Arg.Any<string>(), true, Arg.Any<CancellationToken>())
                .Returns((Founder?)null);

            await _sut.CreateClientAsync(dto);

            existing.DeletedDate.Should().BeNull();
            existing.Name.Should().Be(dto.Name);
            _clientRepo.DidNotReceive().CreateClient(Arg.Any<Client>());
        }

        [Fact]
        public async Task Create_throws_when_legal_entity_has_no_founders()
        {
            var dto = ValidLegalEntityDto() with { Founders = Array.Empty<FounderForCreationDto>() };

            await FluentActions.Invoking(() => _sut.CreateClientAsync(dto))
                .Should().ThrowAsync<LegalEntityWithoutFoundersException>();
        }

        [Fact]
        public async Task Delete_soft_deletes_orphaned_founders_only()
        {
            var clientId = Guid.NewGuid();
            var otherClientId = Guid.NewGuid();

            var orphanedFounder = new Founder { Id = Guid.NewGuid() };
            var orphanedLink = new ClientFounder { ClientId = clientId, Founder = orphanedFounder };
            orphanedFounder.ClientFounders = new List<ClientFounder> { orphanedLink };

            var sharedFounder = new Founder { Id = Guid.NewGuid() };
            var sharedLink = new ClientFounder { ClientId = clientId, Founder = sharedFounder };
            sharedFounder.ClientFounders = new List<ClientFounder>
            {
                sharedLink,
                new() { ClientId = otherClientId, FounderId = sharedFounder.Id }
            };

            var client = new Client
            {
                Id = clientId,
                ClientType = ClientType.Legal_Entity,
                ClientFounders = new List<ClientFounder> { orphanedLink, sharedLink }
            };
            _clientRepo.GetClientForDeletionAsync(clientId, Arg.Any<CancellationToken>()).Returns(client);

            await _sut.DeleteClientAsync(clientId);

            _founderRepo.Received(1).DeleteFounder(orphanedFounder);
            _founderRepo.DidNotReceive().DeleteFounder(sharedFounder);
            _clientRepo.Received(1).DeleteClient(client);
        }

        [Fact]
        public async Task Delete_throws_when_client_not_found()
        {
            var id = Guid.NewGuid();
            _clientRepo.GetClientForDeletionAsync(id, Arg.Any<CancellationToken>()).Returns((Client?)null);

            await FluentActions.Invoking(() => _sut.DeleteClientAsync(id))
                .Should().ThrowAsync<ClientNotFoundException>();
        }
    }
}
