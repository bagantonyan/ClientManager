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
using Shared.DataTransferObjects.Founders;
using Shared.Enums;

namespace ClientManager.UnitTests.Services
{
    public class FounderServiceTests
    {
        private readonly IRepositoryManager _repo = Substitute.For<IRepositoryManager>();
        private readonly IClientRepository _clientRepo = Substitute.For<IClientRepository>();
        private readonly IFounderRepository _founderRepo = Substitute.For<IFounderRepository>();
        private readonly FounderService _sut;

        public FounderServiceTests()
        {
            _repo.Client.Returns(_clientRepo);
            _repo.Founder.Returns(_founderRepo);

            var mapper = new MapperConfiguration(
                cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
            _sut = new FounderService(_repo, Substitute.For<ILoggerManager>(), mapper);
        }

        private static FounderForCreationDto ValidDto() =>
            new() { INN = ValidInns.Individual1, FullName = "Иванов И.И." };

        private void SetupActiveLegalEntity(Guid clientId) =>
            _clientRepo.GetClientAsync(clientId, true, false, Arg.Any<CancellationToken>())
                .Returns(new Client { Id = clientId, ClientType = ClientType.Legal_Entity });

        [Fact]
        public async Task Create_restores_soft_deleted_founder_and_links_to_client()
        {
            var clientId = Guid.NewGuid();
            SetupActiveLegalEntity(clientId);

            var existing = new Founder
            {
                Id = Guid.NewGuid(),
                INN = ValidInns.Individual1,
                FullName = "Old",
                DeletedDate = DateTime.UtcNow.AddDays(-1),
                ClientFounders = new List<ClientFounder>()
            };
            _founderRepo.GetByInnIncludingDeletedAsync(ValidInns.Individual1, true, Arg.Any<CancellationToken>())
                .Returns(existing);

            await _sut.CreateFounderForClientAsync(clientId, ValidDto(), true);

            existing.DeletedDate.Should().BeNull();
            existing.FullName.Should().Be("Иванов И.И.");
            existing.ClientFounders!.Should().ContainSingle(cf => cf.Client!.Id == clientId);
        }

        [Fact]
        public async Task Create_reuses_active_founder_for_a_different_client()
        {
            var clientId = Guid.NewGuid();
            var otherClientId = Guid.NewGuid();
            SetupActiveLegalEntity(clientId);

            var existing = new Founder
            {
                Id = Guid.NewGuid(),
                INN = ValidInns.Individual1,
                ClientFounders = new List<ClientFounder> { new() { ClientId = otherClientId } }
            };
            _founderRepo.GetByInnIncludingDeletedAsync(ValidInns.Individual1, true, Arg.Any<CancellationToken>())
                .Returns(existing);

            await _sut.CreateFounderForClientAsync(clientId, ValidDto(), true);

            existing.ClientFounders.Should().HaveCount(2);
        }

        [Fact]
        public async Task Create_throws_when_founder_already_linked_to_same_client()
        {
            var clientId = Guid.NewGuid();
            SetupActiveLegalEntity(clientId);

            _founderRepo.GetByInnIncludingDeletedAsync(ValidInns.Individual1, true, Arg.Any<CancellationToken>())
                .Returns(new Founder
                {
                    Id = Guid.NewGuid(),
                    INN = ValidInns.Individual1,
                    ClientFounders = new List<ClientFounder> { new() { ClientId = clientId } }
                });

            await FluentActions.Invoking(() => _sut.CreateFounderForClientAsync(clientId, ValidDto(), true))
                .Should().ThrowAsync<FounderAlreadyLinkedToClientException>();
        }

        [Fact]
        public async Task Delete_throws_when_removing_last_founder_of_legal_entity()
        {
            var clientId = Guid.NewGuid();
            var founderId = Guid.NewGuid();

            _clientRepo.GetClientAsync(clientId, false, true, Arg.Any<CancellationToken>())
                .Returns(new Client
                {
                    Id = clientId,
                    ClientType = ClientType.Legal_Entity,
                    ClientFounders = new List<ClientFounder> { new() { ClientId = clientId, FounderId = founderId } }
                });
            _founderRepo.GetFounderWithLinksAsync(clientId, founderId, true, Arg.Any<CancellationToken>())
                .Returns(new Founder
                {
                    Id = founderId,
                    ClientFounders = new List<ClientFounder> { new() { ClientId = clientId, FounderId = founderId } }
                });

            await FluentActions.Invoking(() =>
                    _sut.DeleteFounderForClientAsync(clientId, founderId, false))
                .Should().ThrowAsync<LegalEntityWithoutFoundersException>();
        }

        [Fact]
        public async Task Delete_unlinks_but_keeps_founder_when_other_links_exist()
        {
            var clientId = Guid.NewGuid();
            var otherClientId = Guid.NewGuid();
            var founderId = Guid.NewGuid();

            _clientRepo.GetClientAsync(clientId, false, true, Arg.Any<CancellationToken>())
                .Returns(new Client
                {
                    Id = clientId,
                    ClientType = ClientType.Legal_Entity,
                    ClientFounders = new List<ClientFounder>
                    {
                        new() { ClientId = clientId, FounderId = founderId },
                        new() { ClientId = clientId, FounderId = Guid.NewGuid() }
                    }
                });

            var founder = new Founder
            {
                Id = founderId,
                ClientFounders = new List<ClientFounder>
                {
                    new() { ClientId = clientId, FounderId = founderId },
                    new() { ClientId = otherClientId, FounderId = founderId }
                }
            };
            _founderRepo.GetFounderWithLinksAsync(clientId, founderId, true, Arg.Any<CancellationToken>())
                .Returns(founder);

            await _sut.DeleteFounderForClientAsync(clientId, founderId, false);

            founder.ClientFounders.Should().ContainSingle(cf => cf.ClientId == otherClientId);
            _founderRepo.DidNotReceive().DeleteFounder(Arg.Any<Founder>());
        }
    }
}
