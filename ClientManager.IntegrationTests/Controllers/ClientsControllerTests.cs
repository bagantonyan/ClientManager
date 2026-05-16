using ClientManager.IntegrationTests.Infrastructure;
using FluentAssertions;
using Shared.DataTransferObjects.Clients;
using Shared.DataTransferObjects.Founders;
using Shared.Enums;
using System.Net;
using System.Net.Http.Json;

namespace ClientManager.IntegrationTests.Controllers
{
    public class ClientsControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ClientsControllerTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private const string LegalEntityInn = "7707083893";
        private const string IndividualInn  = "770708389324";

        private static ClientForCreationDto ValidLegalEntityDto(string? inn = null) => new()
        {
            INN = inn ?? LegalEntityInn,
            Name = "ООО Тест",
            ClientType = ClientType.Legal_Entity,
            Founders = new[]
            {
                new FounderForCreationDto { INN = IndividualInn, FullName = "Иванов И.И." }
            }
        };

        [Fact]
        public async Task Post_creates_a_client_and_returns_201_with_etag()
        {
            var dto = ValidLegalEntityDto();

            var response = await _client.PostAsJsonAsync("/api/clients", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.ETag.Should().NotBeNull("server must return ETag for concurrency control");

            var created = await response.Content.ReadFromJsonAsync<ClientDto>();
            created.Should().NotBeNull();
            created!.INN.Should().Be(dto.INN);
            created.Name.Should().Be(dto.Name);
            created.Founders.Should().HaveCount(1);
        }

        [Fact]
        public async Task Post_returns_422_when_inn_length_doesnt_match_client_type()
        {
            var dto = ValidLegalEntityDto() with { INN = IndividualInn };

            var response = await _client.PostAsJsonAsync("/api/clients", dto);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task Post_returns_422_when_legal_entity_has_no_founders()
        {
            var dto = ValidLegalEntityDto() with { Founders = Array.Empty<FounderForCreationDto>() };

            var response = await _client.PostAsJsonAsync("/api/clients", dto);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task Get_returns_404_when_client_does_not_exist()
        {
            var response = await _client.GetAsync($"/api/clients/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Get_returns_200_with_etag_for_existing_client()
        {
            var createResponse = await _client.PostAsJsonAsync("/api/clients",
                ValidLegalEntityDto(inn: "7710030411"));
            var created = await createResponse.Content.ReadFromJsonAsync<ClientDto>();

            var getResponse = await _client.GetAsync($"/api/clients/{created!.Id}");

            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            getResponse.Headers.ETag.Should().NotBeNull();
        }

        [Fact]
        public async Task Delete_returns_204_then_get_returns_404()
        {
            var createResponse = await _client.PostAsJsonAsync("/api/clients",
                ValidLegalEntityDto(inn: "7728168971"));
            var created = await createResponse.Content.ReadFromJsonAsync<ClientDto>();

            var deleteResponse = await _client.DeleteAsync($"/api/clients/{created!.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await _client.GetAsync($"/api/clients/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "soft-deleted clients are filtered out by query filter");
        }

        [Fact]
        public async Task Get_list_returns_200_with_pagination_header()
        {
            var response = await _client.GetAsync("/api/clients?PageNumber=1&PageSize=10");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.Should().Contain(h => h.Key == "X-Pagination",
                "controller writes pagination metadata into a header");
        }

        [Fact]
        public async Task Get_collection_returns_400_when_route_id_list_is_garbage()
        {
            var response = await _client.GetAsync("/api/clients/collection/(not-a-guid,also-garbage)");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}