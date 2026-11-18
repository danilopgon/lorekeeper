using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Application.CreateCampaign;
using Api.Modules.Campaigns.Application.GetCampaign;
using Api.Modules.Campaigns.Application.ListCampaigns;
using Api.Modules.Campaigns.Domain;
using Api.SharedKernel;
using FluentAssertions;
using NSubstitute;

namespace Unit.Modules.Campaigns.Application;

public sealed class CampaignHandlerTests
{
    private readonly ICampaignRepository _repository = Substitute.For<ICampaignRepository>();
    private readonly ICampaignQueries _queries = Substitute.For<ICampaignQueries>();

    [Fact]
    public async Task CreateAddsTheCampaignAndReturnsItsDtoWhenSaved()
    {
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(CampaignSaveOutcome.Saved);

        var result = await new CreateCampaignHandler(_repository).Handle(new CreateCampaignCommand("  Ash Crown  "), CancellationToken.None);

        result.Value.Name.Should().Be("Ash Crown");
        _repository.Received(1).Add(Arg.Is<Campaign>(campaign => campaign.Name.Value == "Ash Crown"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateRejectsInvalidNamesWithoutTouchingTheRepository(string? name)
    {
        var result = await new CreateCampaignHandler(_repository).Handle(new CreateCampaignCommand(name), CancellationToken.None);

        result.Error.Should().Be(Error.Validation("campaign_name_invalid", "Campaign name is invalid.").ForField("name"));
        _repository.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task CreateReportsANameConflictFromTheRepository()
    {
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(CampaignSaveOutcome.NameConflict);

        var result = await new CreateCampaignHandler(_repository).Handle(new CreateCampaignCommand("Ash Crown"), CancellationToken.None);

        result.Error.Should().Be(CreateCampaignHandler.NameConflict);
    }

    [Fact]
    public async Task GetReturnsTheCampaignFoundByTheReadInterface()
    {
        var id = Guid.NewGuid();
        var dto = new CampaignDto(id.ToString("D"), "Ash Crown", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
        _queries.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await new GetCampaignHandler(_queries).Handle(new GetCampaignQuery(id.ToString("D")), CancellationToken.None);

        result.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetReturnsNotFoundForAnUnknownId()
    {
        _queries.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((CampaignDto?)null);

        var result = await new GetCampaignHandler(_queries).Handle(new GetCampaignQuery(Guid.NewGuid().ToString("D")), CancellationToken.None);

        result.Error!.Code.Should().Be("campaign_not_found");
    }

    [Fact]
    public async Task GetRejectsAMalformedIdWithoutQuerying()
    {
        var result = await new GetCampaignHandler(_queries).Handle(new GetCampaignQuery("not-a-uuid"), CancellationToken.None);

        result.Error!.Code.Should().Be("campaign_id_invalid");
        _queries.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ListReturnsTheReadInterfaceProjection()
    {
        IReadOnlyList<CampaignDto> campaigns = [new CampaignDto(Guid.NewGuid().ToString("D"), "Ash Crown", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch)];
        _queries.ListAsync(Arg.Any<CancellationToken>()).Returns(campaigns);

        var result = await new ListCampaignsHandler(_queries).Handle(new ListCampaignsQuery(), CancellationToken.None);

        result.Value.Should().BeSameAs(campaigns);
    }
}
