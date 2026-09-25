using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Application.CreateCampaign;
using Api.Modules.Campaigns.Application.GetCampaign;
using Api.Modules.Campaigns.Application.ListCampaigns;
using FluentAssertions;
using MediatR;

namespace Unit.Modules.Campaigns.Application;

public sealed class CampaignRequestHandlerTests
{
    [Fact]
    public void CampaignUseCasesAreMediatRRequestsHandledByTheirFeatureHandlers()
    {
        typeof(CreateCampaignCommand).Should().Implement<IRequest<CreateCampaignResult>>();
        typeof(CreateCampaignHandler).Should().Implement<IRequestHandler<CreateCampaignCommand, CreateCampaignResult>>();
        typeof(ListCampaignsQuery).Should().Implement<IRequest<IReadOnlyList<CampaignDto>>>();
        typeof(ListCampaignsHandler).Should().Implement<IRequestHandler<ListCampaignsQuery, IReadOnlyList<CampaignDto>>>();
        typeof(GetCampaignQuery).Should().Implement<IRequest<GetCampaignResult>>();
        typeof(GetCampaignHandler).Should().Implement<IRequestHandler<GetCampaignQuery, GetCampaignResult>>();
    }
}
