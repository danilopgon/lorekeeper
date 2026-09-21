using Api.Modules.Campaigns.Domain;
using FluentAssertions;

namespace Unit.Modules.Campaigns.Domain;

public class CampaignTests
{
    [Fact]
    public void CreateUsesGeneratedIdentityTrimmedNameAndServerTimestamps()
    {
        var now = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        var name = CampaignName.Create(" Ash Crown ").Value!;

        var campaign = Campaign.Create(name, now);

        campaign.Id.Should().NotBe(Guid.Empty);
        campaign.Name.Value.Should().Be("Ash Crown");
        campaign.CreatedAt.Should().Be(now);
        campaign.UpdatedAt.Should().Be(now);
    }
}
