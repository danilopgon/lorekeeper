using Api.Modules.Campaigns.Domain;
using FluentAssertions;

namespace Unit.Modules.Campaigns.Domain;

public class CampaignNameTests
{
    [Theory]
    [InlineData(" Ash Crown ", "Ash Crown")]
    [InlineData("A", "A")]
    public void CreateTrimsValidNames(string input, string expected)
    {
        var result = CampaignName.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRejectsMissingOrBlankNames(string? input)
    {
        var result = CampaignName.Create(input);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(CampaignName.InvalidCode);
    }

    [Fact]
    public void CreateAcceptsNamesAtMaximumLength()
    {
        var result = CampaignName.Create(new string('A', CampaignName.MaxLength));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().HaveLength(CampaignName.MaxLength);
    }

    [Fact]
    public void CreateRejectsNamesOverMaximumLengthAfterTrimming()
    {
        var result = CampaignName.Create($" {new string('A', CampaignName.MaxLength + 1)} ");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(CampaignName.InvalidCode);
    }
}
