using Api.Modules.Campaigns.Domain;
using FluentAssertions;

namespace Unit.Modules.Campaigns.Domain;

public class CampaignIdParserTests
{
    [Fact]
    public void ParseAcceptsCanonicalUuidAndFormatsLowercase()
    {
        var result = CampaignIdParser.Parse("8F5F5B5B-7B4B-4B86-A7A2-59457E82DC11");

        result.IsSuccess.Should().BeTrue();
        CampaignIdParser.Format(result.Value).Should().Be("8f5f5b5b-7b4b-4b86-a7a2-59457e82dc11");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("8f5f5b5b7b4b4b86a7a259457e82dc11")]
    public void ParseRejectsMalformedUuidValues(string? input)
    {
        var result = CampaignIdParser.Parse(input);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(CampaignIdParser.InvalidCode);
    }
}
